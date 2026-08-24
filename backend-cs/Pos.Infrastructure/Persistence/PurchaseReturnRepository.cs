using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using Microsoft.Data.Sqlite;
using PosCs.Application.Ports;
using PosCs.Domain.Entities;
using PosCs.Domain.Exceptions;
using PosCs.Infrastructure.Persistence;

namespace PosCs.Infrastructure.Persistence
{
    /// <summary>Transactional purchase-return pipeline (plan Phase 8, spec §25): stock leaves
    /// through the shared ledger, the original purchase's cost layers are reduced at their
    /// historical unit cost, cash purchases get an automatic negative refund payment, and
    /// the original purchase's returnStatus rolls forward (partial → full).</summary>
    public class PurchaseReturnRepository : IPurchaseReturnRepository
    {
        public PurchaseReturn Create(PurchaseReturn purchaseReturn, List<PurchaseReturnDetail> details)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        var invoice = conn.QueryFirstOrDefault<PurchaseInvoice>(
                            "SELECT * FROM PurchaseInvoice WHERE id = @id", new { id = purchaseReturn.PurchaseInvoiceId }, transaction: tx);
                        if (invoice == null)
                            throw new NotFoundException("Purchase invoice not found");
                        if (invoice.Status != "posted")
                            throw new DomainValidationException("Only posted purchase invoices can be returned");

                        var lines = conn.Query<PurchaseInvoiceItem>(
                            "SELECT * FROM PurchaseInvoiceItem WHERE purchaseInvoiceId = @id", new { id = invoice.Id }, transaction: tx)
                            .ToDictionary(i => i.Id);

                        var returnedBefore = SumReturnedByPurchase(conn, tx, invoice.Id);

                        purchaseReturn.Id = Guid.NewGuid().ToString("N");
                        purchaseReturn.CreatedAt = DateTime.Now;
                        purchaseReturn.Date = DateTime.Now;
                        purchaseReturn.Number = conn.ExecuteScalar<int>(
                            "SELECT COALESCE(MAX(number), 0) + 1 FROM PurchaseReturn", transaction: tx);

                        var totalRefund = 0.0;

                        foreach (var item in details)
                        {
                            if (!lines.TryGetValue(item.PurchaseItemId, out var line))
                                throw new DomainValidationException("Return line does not belong to the original purchase");

                            var factor = line.QuantityFactor > 0 ? line.QuantityFactor : 1;
                            var alreadyReturned = returnedBefore.TryGetValue(line.Id, out var r) ? r : 0;
                            var available = line.Quantity - alreadyReturned;
                            if (item.Quantity <= 0 || item.Quantity > available + 1e-9)
                                throw new DomainValidationException(
                                    $"Invalid return quantity for product '{line.ProductId}'; maximum is {Math.Max(available, 0)}");

                            // Refund at the historical unit cost (spec §25 example: bought @8 → refunded @8).
                            item.UnitCost = line.UnitCost;
                            item.LineTotal = Math.Round(line.UnitCost * item.Quantity, 2);
                            item.QuantityFactor = factor;
                            item.ProductId = line.ProductId;
                            item.UnitName = line.UnitName;

                            ReduceFifo(conn, tx, line.ProductId, invoice.Id, item.Quantity * factor);

                            StockLedger.Apply(conn, tx, line.ProductId, -(item.Quantity * factor),
                                StockLedger.PurchaseReturn, purchaseReturn.Id, purchaseReturn.Number.ToString(), requireStock: false);

                            totalRefund += item.LineTotal;
                        }

                        if (totalRefund <= 0)
                            throw new DomainValidationException("Return must include at least one line");

                        purchaseReturn.TotalAmount = Math.Round(totalRefund, 2);
                        purchaseReturn.PaymentMethod = string.IsNullOrEmpty(invoice.PaymentMethod) ? "cash" : invoice.PaymentMethod;

                        conn.Execute(@"
                            INSERT INTO PurchaseReturn (id, number, purchaseInvoiceId, date, totalAmount, paymentMethod, notes, status, createdBy, createdAt)
                            VALUES (@id, @number, @purchaseInvoiceId, @date, @totalAmount, @paymentMethod, @notes, @status, @createdBy, @createdAt)",
                            new
                            {
                                id = purchaseReturn.Id,
                                number = purchaseReturn.Number,
                                purchaseInvoiceId = purchaseReturn.PurchaseInvoiceId,
                                date = purchaseReturn.Date.ToString("yyyy-MM-dd HH:mm:ss"),
                                totalAmount = purchaseReturn.TotalAmount,
                                paymentMethod = purchaseReturn.PaymentMethod,
                                notes = purchaseReturn.Notes,
                                status = purchaseReturn.Status,
                                createdBy = purchaseReturn.CreatedBy,
                                createdAt = purchaseReturn.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
                            }, transaction: tx);

                        foreach (var item in details)
                        {
                            item.Id = Guid.NewGuid().ToString("N");
                            item.ReturnId = purchaseReturn.Id;
                            conn.Execute(@"
                                INSERT INTO PurchaseReturnDetail (id, returnId, purchaseItemId, productId, unitName, quantity, quantityFactor, unitCost, lineTotal)
                                VALUES (@id, @returnId, @purchaseItemId, @productId, @unitName, @quantity, @quantityFactor, @unitCost, @lineTotal)",
                                new
                                {
                                    id = item.Id,
                                    returnId = item.ReturnId,
                                    purchaseItemId = item.PurchaseItemId,
                                    productId = item.ProductId,
                                    unitName = item.UnitName,
                                    quantity = item.Quantity,
                                    quantityFactor = item.QuantityFactor,
                                    unitCost = item.UnitCost,
                                    lineTotal = item.LineTotal
                                }, transaction: tx);
                        }

                        // Cash refunds move real money back out: a negative payment keeps every
                        // derived paid/remaining figure correct by construction (spec §19).
                        if (purchaseReturn.PaymentMethod == "cash")
                        {
                            // Attribute the refund to the shift that is open now, if any.
                            var refundShiftId = conn.ExecuteScalar<string>(
                                "SELECT id FROM Shift WHERE status = 'open' ORDER BY number DESC LIMIT 1", transaction: tx);
                            conn.Execute(@"
                                INSERT INTO Payment (id, amount, paymentMethod, date, invoiceId, clientId, supplierId, reference, notes, createdBy, shiftId, createdAt)
                                VALUES (@id, @amount, 'cash', @date, @invoiceId, NULL, @supplierId, @reference, NULL, @createdBy, @shiftId, @createdAt)",
                                new
                                {
                                    id = Guid.NewGuid().ToString("N"),
                                    amount = -purchaseReturn.TotalAmount,
                                    date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                                    invoiceId = invoice.Id,
                                    supplierId = string.IsNullOrEmpty(invoice.SupplierId) ? null : invoice.SupplierId,
                                    reference = $"auto: refund for purchase #{invoice.InvoiceNumber}",
                                    createdBy = purchaseReturn.CreatedBy,
                                    shiftId = refundShiftId,
                                    createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                                }, transaction: tx);
                        }

                        UpdateReturnStatus(conn, tx, invoice);

                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }

                return GetByIdCore(conn, purchaseReturn.Id);
            }
        }

        public PurchaseReturnPageResult GetPaged(int page, int pageSize, string purchaseInvoiceId)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                var where = " WHERE r.status = 'posted'";
                if (!string.IsNullOrWhiteSpace(purchaseInvoiceId))
                    where += " AND r.purchaseInvoiceId = @purchaseInvoiceId";

                var parameters = new { purchaseInvoiceId, pageSize, offset = (page - 1) * pageSize };

                var total = conn.ExecuteScalar<int>($"SELECT COUNT(1) FROM PurchaseReturn r{where}", parameters);
                var items = conn.Query<PurchaseReturn>(
                    "SELECT r.*, p.invoiceNumber AS PurchaseInvoiceNumber FROM PurchaseReturn r " +
                    "JOIN PurchaseInvoice p ON p.id = r.purchaseInvoiceId" + where +
                    " ORDER BY r.createdAt DESC, r.rowid DESC LIMIT @pageSize OFFSET @offset",
                    parameters).ToList();

                foreach (var ret in items)
                    ret.Details = AttachDetails(conn, ret);

                return new PurchaseReturnPageResult { Items = items, Total = total };
            }
        }

        public Dictionary<string, double> SumReturnedByPurchase(string purchaseInvoiceId)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return SumReturnedByPurchase(conn, null, purchaseInvoiceId);
        }

        private static Dictionary<string, double> SumReturnedByPurchase(
            SqliteConnection conn, SqliteTransaction tx, string purchaseInvoiceId)
        {
            return conn.Query<ReturnedRow>(
                "SELECT d.purchaseItemId AS PurchaseItemId, SUM(d.quantity) AS Quantity " +
                "FROM PurchaseReturnDetail d " +
                "JOIN PurchaseReturn r ON r.id = d.returnId " +
                "WHERE r.purchaseInvoiceId = @purchaseInvoiceId AND r.status = 'posted' " +
                "GROUP BY d.purchaseItemId",
                new { purchaseInvoiceId }, transaction: tx)
                .ToDictionary(x => x.PurchaseItemId, x => x.Quantity);
        }

        public List<PurchaseReturn> ListPostedBySupplier(string supplierId)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                return conn.Query<PurchaseReturn>(
                    "SELECT r.*, p.invoiceNumber AS PurchaseInvoiceNumber FROM PurchaseReturn r " +
                    "JOIN PurchaseInvoice p ON p.id = r.purchaseInvoiceId " +
                    "WHERE p.supplierId = @supplierId AND r.status = 'posted' " +
                    "ORDER BY r.date ASC, r.number ASC LIMIT 500",
                    new { supplierId }).ToList();
            }
        }

        public bool HasPostedReturns(string purchaseInvoiceId)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return conn.ExecuteScalar<int>(
                    "SELECT COUNT(1) FROM PurchaseReturn WHERE purchaseInvoiceId = @purchaseInvoiceId AND status = 'posted'",
                    new { purchaseInvoiceId }) > 0;
        }

        /// <summary>Returns purchased goods to the supplier by reducing THIS purchase's cost
        /// layers in arrival order at their stored unit cost. Remaining may go negative when
        /// goods were already sold; the FIFO allocator skips non-positive layers.</summary>
        private static void ReduceFifo(SqliteConnection conn, SqliteTransaction tx,
            string productId, string sourcePurchaseId, double baseQuantity)
        {
            var layers = conn.Query<LayerRow>(
                "SELECT id AS Id, quantityRemaining AS QuantityRemaining, unitCost AS UnitCost " +
                "FROM CostLayer WHERE productId = @productId AND sourcePurchaseId = @sourcePurchaseId " +
                "ORDER BY createdAt, rowid",
                new { productId, sourcePurchaseId }, transaction: tx).ToList();

            var remaining = baseQuantity;
            foreach (var layer in layers)
            {
                if (remaining <= 1e-9) break;
                var take = Math.Min(Math.Max(layer.QuantityRemaining, 0), remaining);
                if (take <= 0) continue;

                conn.Execute("UPDATE CostLayer SET quantityRemaining = quantityRemaining - @take WHERE id = @id",
                    new { take, id = layer.Id }, transaction: tx);
                remaining -= take;
            }

            // Goods already sold have no layer capacity left; the excess leaves as negative
            // remaining on the last layer — same semantics as a full purchase cancellation.
            if (remaining > 1e-9 && layers.Count > 0)
            {
                conn.Execute("UPDATE CostLayer SET quantityRemaining = quantityRemaining - @take WHERE id = @id",
                    new { take = remaining, id = layers[layers.Count - 1].Id }, transaction: tx);
            }
        }

        /// <summary>'partial' when some purchased quantity remains unreturned, 'full' when every
        /// line has been completely returned.</summary>
        private static void UpdateReturnStatus(SqliteConnection conn, SqliteTransaction tx, PurchaseInvoice invoice)
        {
            var rows = conn.Query<StatusRow>(
                "SELECT i.quantity AS Purchased, " +
                "(SELECT COALESCE(SUM(rd.quantity), 0) FROM PurchaseReturnDetail rd " +
                " JOIN PurchaseReturn r ON r.id = rd.returnId " +
                " WHERE rd.purchaseItemId = i.id AND r.status = 'posted') AS Returned " +
                "FROM PurchaseInvoiceItem i WHERE i.purchaseInvoiceId = @id",
                new { id = invoice.Id }, transaction: tx).ToList();

            var allFullyReturned = rows.Count > 0 && rows.All(x => x.Returned >= x.Purchased - 1e-9);
            var anyReturned = rows.Any(x => x.Returned > 0);

            var status = allFullyReturned ? "full" : (anyReturned ? "partial" : (string)null);
            if (status != null)
                conn.Execute("UPDATE PurchaseInvoice SET returnStatus = @status WHERE id = @id",
                    new { status, id = invoice.Id }, transaction: tx);
        }

        private static List<PurchaseReturnDetail> AttachDetails(SqliteConnection conn, PurchaseReturn purchaseReturn)
        {
            var details = conn.Query<PurchaseReturnDetail>(
                "SELECT * FROM PurchaseReturnDetail WHERE returnId = @returnId ORDER BY rowid",
                new { returnId = purchaseReturn.Id }).ToList();
            foreach (var detail in details)
            {
                detail.Product = conn.QueryFirstOrDefault<Product>(
                    "SELECT * FROM Product WHERE id = @id", new { id = detail.ProductId });
            }
            return details;
        }

        private static PurchaseReturn GetByIdCore(SqliteConnection conn, string id)
        {
            var ret = conn.QueryFirstOrDefault<PurchaseReturn>(
                "SELECT r.*, p.invoiceNumber AS PurchaseInvoiceNumber FROM PurchaseReturn r " +
                "JOIN PurchaseInvoice p ON p.id = r.purchaseInvoiceId WHERE r.id = @id", new { id });
            if (ret != null)
                ret.Details = AttachDetails(conn, ret);
            return ret;
        }

        private sealed class ReturnedRow
        {
            public string PurchaseItemId { get; set; }
            public double Quantity { get; set; }
        }

        private sealed class LayerRow
        {
            public string Id { get; set; }
            public double QuantityRemaining { get; set; }
            public double UnitCost { get; set; }
        }

        private sealed class StatusRow
        {
            public double Purchased { get; set; }
            public double Returned { get; set; }
        }
    }
}
