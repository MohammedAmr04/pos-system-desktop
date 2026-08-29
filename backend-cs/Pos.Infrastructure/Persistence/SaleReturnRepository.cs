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
    /// <summary>Transactional sales-return pipeline (plan Phase 7, spec §22-24): restock via
    /// the shared ledger, restore consumed FIFO layers from the original allocations without
    /// ever double-restoring (returnedQuantity guard), auto refund payment for cash sales,
    /// and roll the original invoice's returnStatus forward (partial → full).</summary>
    public class SaleReturnRepository : ISaleReturnRepository
    {
        public SaleReturn Create(SaleReturn saleReturn, List<SaleReturnDetail> details)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        var invoice = conn.QueryFirstOrDefault<Invoice>(
                            "SELECT * FROM Invoice WHERE id = @id", new { id = saleReturn.InvoiceId }, transaction: tx);
                        if (invoice == null)
                            throw new NotFoundException("Invoice not found");
                        if (invoice.Status != "posted")
                            throw new DomainValidationException("Only posted invoices can be returned");

                        var sold = conn.Query<InvoiceDetail>(
                            "SELECT * FROM InvoiceDetail WHERE invoiceId = @id", new { id = invoice.Id }, transaction: tx)
                            .ToDictionary(d => d.Id);

                        var returnedBefore = SumReturnedByInvoice(conn, tx, invoice.Id);

                        saleReturn.Id = Guid.NewGuid().ToString("N");
                        saleReturn.CreatedAt = DateTime.Now;
                        saleReturn.Date = DateTime.Now;
                        saleReturn.Number = conn.ExecuteScalar<int>(
                            "SELECT COALESCE(MAX(number), 0) + 1 FROM SaleReturn", transaction: tx);

                        var totalRefund = 0.0;
                        var totalRestoredCost = 0.0;

                        foreach (var item in details)
                        {
                            if (!sold.TryGetValue(item.InvoiceDetailId, out var line))
                                throw new DomainValidationException("Return line does not belong to the original invoice");

                            var factor = line.QuantityFactor > 0 ? line.QuantityFactor : 1;
                            var alreadyReturned = returnedBefore.TryGetValue(line.Id, out var r) ? r : 0;
                            var available = line.Quantity - alreadyReturned;
                            if (item.Quantity <= 0 || item.Quantity > available + 1e-9)
                                throw new DomainValidationException(
                                    $"Invalid return quantity for product '{line.ProductId}'; maximum is {Math.Max(available, 0)}");

                            // Historical price snapshot; prorate discounts across returned qty.
                            var lineBase = line.FinalTotal > 0
                                ? line.FinalTotal
                                : (line.UnitPrice > 0 ? line.UnitPrice : line.SalePrice) * line.Quantity;
                            var unitRefund = line.Quantity > 0 ? lineBase / line.Quantity : 0;
                            item.LineTotal = Math.Round(unitRefund * item.Quantity, 2);
                            item.UnitPrice = line.UnitPrice > 0 ? line.UnitPrice : line.SalePrice;
                            item.QuantityFactor = factor;
                            item.ProductId = line.ProductId;
                            item.UnitName = line.UnitName;

                            item.RestoredCost = RestoreFifo(conn, tx, line.Id, item.Quantity * factor);

                            StockLedger.Apply(conn, tx, line.ProductId, item.Quantity * factor,
                                StockLedger.SaleReturn, saleReturn.Id, saleReturn.Number.ToString(), requireStock: false);

                            totalRefund += item.LineTotal;
                            totalRestoredCost += item.RestoredCost;
                        }

                        if (totalRefund <= 0)
                            throw new DomainValidationException("Return must include at least one line");

                        saleReturn.TotalAmount = Math.Round(totalRefund, 2);
                        saleReturn.RestoredCost = Math.Round(totalRestoredCost, 2);
                        saleReturn.PaymentMethod = string.IsNullOrEmpty(invoice.PaymentMethod) ? "cash" : invoice.PaymentMethod;

                        conn.Execute(@"
                            INSERT INTO SaleReturn (id, number, invoiceId, date, totalAmount, restoredCost, paymentMethod, notes, status, createdBy, createdAt)
                            VALUES (@id, @number, @invoiceId, @date, @totalAmount, @restoredCost, @paymentMethod, @notes, @status, @createdBy, @createdAt)",
                            new
                            {
                                id = saleReturn.Id,
                                number = saleReturn.Number,
                                invoiceId = saleReturn.InvoiceId,
                                date = saleReturn.Date.ToString("yyyy-MM-dd HH:mm:ss"),
                                totalAmount = saleReturn.TotalAmount,
                                restoredCost = saleReturn.RestoredCost,
                                paymentMethod = saleReturn.PaymentMethod,
                                notes = saleReturn.Notes,
                                status = saleReturn.Status,
                                createdBy = saleReturn.CreatedBy,
                                createdAt = saleReturn.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
                            }, transaction: tx);

                        foreach (var item in details)
                        {
                            item.Id = Guid.NewGuid().ToString("N");
                            item.ReturnId = saleReturn.Id;
                            conn.Execute(@"
                                INSERT INTO SaleReturnDetail (id, returnId, invoiceDetailId, productId, unitName, quantity, quantityFactor, unitPrice, lineTotal, restoredCost)
                                VALUES (@id, @returnId, @invoiceDetailId, @productId, @unitName, @quantity, @quantityFactor, @unitPrice, @lineTotal, @restoredCost)",
                                new
                                {
                                    id = item.Id,
                                    returnId = item.ReturnId,
                                    invoiceDetailId = item.InvoiceDetailId,
                                    productId = item.ProductId,
                                    unitName = item.UnitName,
                                    quantity = item.Quantity,
                                    quantityFactor = item.QuantityFactor,
                                    unitPrice = item.UnitPrice,
                                    lineTotal = item.LineTotal,
                                    restoredCost = item.RestoredCost
                                }, transaction: tx);
                        }

                        // Cash refunds move real money back out: a negative payment keeps every
                        // derived paid/remaining figure correct by construction (spec §19).
                        if (saleReturn.PaymentMethod == "cash")
                        {
                            // Attribute the refund to the shift that is open now, if any.
                            var refundShiftId = conn.ExecuteScalar<string>(
                                "SELECT id FROM Shift WHERE status = 'open' ORDER BY number DESC LIMIT 1", transaction: tx);
                            conn.Execute(@"
                                INSERT INTO Payment (id, amount, paymentMethod, date, invoiceId, clientId, supplierId, reference, notes, createdBy, shiftId, createdAt)
                                VALUES (@id, @amount, 'cash', @date, @invoiceId, @clientId, NULL, @reference, NULL, @createdBy, @shiftId, @createdAt)",
                                new
                                {
                                    id = Guid.NewGuid().ToString("N"),
                                    amount = -saleReturn.TotalAmount,
                                    date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                                    invoiceId = invoice.Id,
                                    clientId = string.IsNullOrEmpty(invoice.ClientId) ? null : invoice.ClientId,
                                    reference = $"auto: refund for sale #{invoice.InvoiceNumber}",
                                    createdBy = saleReturn.CreatedBy,
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

                return GetByIdCore(conn, saleReturn.Id);
            }
        }

        public SaleReturnPageResult GetPaged(int page, int pageSize, string invoiceId)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                var where = " WHERE r.status = 'posted'";
                if (!string.IsNullOrWhiteSpace(invoiceId))
                    where += " AND r.invoiceId = @invoiceId";

                var parameters = new { invoiceId, pageSize, offset = (page - 1) * pageSize };

                var total = conn.ExecuteScalar<int>(
                    $"SELECT COUNT(1) FROM SaleReturn r{where}", parameters);
                var items = conn.Query<SaleReturn>(
                    "SELECT r.*, i.invoiceNumber AS InvoiceNumber FROM SaleReturn r " +
                    "JOIN Invoice i ON i.id = r.invoiceId" + where +
                    " ORDER BY r.createdAt DESC, r.rowid DESC LIMIT @pageSize OFFSET @offset",
                    parameters).ToList();

                foreach (var ret in items)
                    ret.Details = AttachDetails(conn, ret);

                return new SaleReturnPageResult { Items = items, Total = total };
            }
        }

        public List<SaleReturnDetail> GetDetails(string returnId)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                return AttachDetails(conn, new SaleReturn { Id = returnId });
            }
        }

        public Dictionary<string, double> SumReturnedByInvoice(string invoiceId)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                return SumReturnedByInvoice(conn, null, invoiceId);
            }
        }

        private static Dictionary<string, double> SumReturnedByInvoice(
            SqliteConnection conn, SqliteTransaction tx, string invoiceId)
        {
            return conn.Query<ReturnedRow>(
                "SELECT d.invoiceDetailId AS InvoiceDetailId, SUM(d.quantity) AS Quantity " +
                "FROM SaleReturnDetail d " +
                "JOIN SaleReturn r ON r.id = d.returnId " +
                "WHERE r.invoiceId = @invoiceId AND r.status = 'posted' " +
                "GROUP BY d.invoiceDetailId",
                new { invoiceId }, transaction: tx)
                .ToDictionary(x => x.InvoiceDetailId, x => x.Quantity);
        }

        /// <summary>Returns the returned quantity to its original FIFO layers (spec §24): walks the
        /// line's allocations in order and restores each layer up to what that allocation still
        /// holds, marking restored amounts so later returns cannot restore them twice.</summary>
        private static double RestoreFifo(SqliteConnection conn, SqliteTransaction tx,
            string invoiceDetailId, double baseQuantity)
        {
            var allocations = conn.Query<AllocationRow>(
                "SELECT id AS Id, costLayerId AS CostLayerId, quantity AS Quantity, unitCost AS UnitCost, returnedQuantity AS ReturnedQuantity " +
                "FROM SaleCostAllocation WHERE invoiceDetailId = @invoiceDetailId ORDER BY createdAt, rowid",
                new { invoiceDetailId }, transaction: tx).ToList();

            var remaining = baseQuantity;
            var restoredCost = 0.0;

            foreach (var alloc in allocations)
            {
                if (remaining <= 1e-9) break;
                var restorable = alloc.Quantity - alloc.ReturnedQuantity;
                if (restorable <= 0) continue;

                var take = Math.Min(restorable, remaining);
                conn.Execute("UPDATE CostLayer SET quantityRemaining = quantityRemaining + @take WHERE id = @id",
                    new { take, id = alloc.CostLayerId }, transaction: tx);
                conn.Execute("UPDATE SaleCostAllocation SET returnedQuantity = returnedQuantity + @take WHERE id = @id",
                    new { take, id = alloc.Id }, transaction: tx);
                restoredCost += take * alloc.UnitCost;
                remaining -= take;
            }

            // Legacy lines sold before Phase 4 have no allocations; nothing to restore —
            // stock still comes back through the ledger above.
            return Math.Round(restoredCost, 2);
        }

        /// <summary>'partial' when some sellable quantity remains out, 'full' when every line
        /// has been completely returned.</summary>
        private static void UpdateReturnStatus(SqliteConnection conn, SqliteTransaction tx, Invoice invoice)
        {
            var rows = conn.Query<StatusRow>(
                "SELECT d.quantity AS Sold, " +
                "(SELECT COALESCE(SUM(rd.quantity), 0) FROM SaleReturnDetail rd " +
                " JOIN SaleReturn r ON r.id = rd.returnId " +
                " WHERE rd.invoiceDetailId = d.id AND r.status = 'posted') AS Returned " +
                "FROM InvoiceDetail d WHERE d.invoiceId = @id",
                new { id = invoice.Id }, transaction: tx).ToList();

            var allFullyReturned = rows.Count > 0 && rows.All(x => x.Returned >= x.Sold - 1e-9);
            var anyReturned = rows.Any(x => x.Returned > 0);

            var status = allFullyReturned ? "full" : (anyReturned ? "partial" : null);
            if (status != null)
                conn.Execute("UPDATE Invoice SET returnStatus = @status WHERE id = @id",
                    new { status, id = invoice.Id }, transaction: tx);
        }

        private static List<SaleReturnDetail> AttachDetails(SqliteConnection conn, SaleReturn saleReturn)
        {
            var details = conn.Query<SaleReturnDetail>(
                "SELECT * FROM SaleReturnDetail WHERE returnId = @returnId ORDER BY rowid",
                new { returnId = saleReturn.Id }).ToList();
            foreach (var detail in details)
            {
                detail.Product = conn.QueryFirstOrDefault<Product>(
                    "SELECT * FROM Product WHERE id = @id", new { id = detail.ProductId });
            }
            return details;
        }

        private static SaleReturn GetByIdCore(SqliteConnection conn, string id)
        {
            var ret = conn.QueryFirstOrDefault<SaleReturn>(
                "SELECT r.*, i.invoiceNumber AS InvoiceNumber FROM SaleReturn r " +
                "JOIN Invoice i ON i.id = r.invoiceId WHERE r.id = @id", new { id });
            if (ret != null)
                ret.Details = AttachDetails(conn, ret);
            return ret;
        }

        private sealed class ReturnedRow
        {
            public string InvoiceDetailId { get; set; }
            public double Quantity { get; set; }
        }

        private sealed class AllocationRow
        {
            public string Id { get; set; }
            public string CostLayerId { get; set; }
            public double Quantity { get; set; }
            public double UnitCost { get; set; }
            public double ReturnedQuantity { get; set; }
        }

        private sealed class StatusRow
        {
            public double Sold { get; set; }
            public double Returned { get; set; }
        }
    }
}
