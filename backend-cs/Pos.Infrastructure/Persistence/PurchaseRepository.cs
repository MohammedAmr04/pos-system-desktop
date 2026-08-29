using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using Microsoft.Data.Sqlite;
using PosCs.Application.Ports;
using PosCs.Domain.Entities;
using PosCs.Domain.Exceptions;

namespace PosCs.Infrastructure.Persistence
{
    public class PurchaseRepository : IPurchaseRepository
    {
        private const string Columns =
            "id, invoiceNumber, supplierInvoiceNumber, supplierId, date, paymentMethod, status, subtotal, discount, tax, total, notes, createdBy, createdAt, updatedAt";

        public PurchaseInvoicePageResult GetPaged(string status, string query, int page, int pageSize)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                var clauses = new List<string>();
                var parameters = new DynamicParameters();

                if (!string.IsNullOrWhiteSpace(status) && status != "all")
                {
                    clauses.Add("status = @status");
                    parameters.Add("status", status);
                }
                if (!string.IsNullOrWhiteSpace(query))
                {
                    clauses.Add("(CAST(invoiceNumber AS TEXT) LIKE @like OR supplierInvoiceNumber LIKE @like)");
                    parameters.Add("like", $"%{EscapeLike(query.Trim())}%");
                }

                var where = clauses.Count > 0 ? " WHERE " + string.Join(" AND ", clauses) : "";
                parameters.Add("pageSize", pageSize);
                parameters.Add("offset", (page - 1) * pageSize);

                var total = conn.ExecuteScalar<int>($"SELECT COUNT(1) FROM PurchaseInvoice{where}", parameters);
                var postedTotal = conn.ExecuteScalar<double>(
                    $"SELECT COALESCE(SUM(total), 0) FROM PurchaseInvoice{where}{(where.Length > 0 ? " AND" : " WHERE")} status = 'posted'",
                    parameters);

                var items = conn.Query<PurchaseInvoice>(
                    $"SELECT * FROM PurchaseInvoice{where} ORDER BY date DESC, invoiceNumber DESC LIMIT @pageSize OFFSET @offset",
                    parameters).ToList();

                foreach (var inv in items)
                    AttachSupplier(conn, inv);

                return new PurchaseInvoicePageResult { Items = items, Total = total, PostedTotal = postedTotal };
            }
        }

        public PurchaseInvoice GetById(string id)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return GetByIdCore(conn, id);
        }

        /// <summary>
        /// Single transactional entry point for create/update across the lifecycle:
        /// new draft/posted, draft edit, draft→post, and posted edit implemented as an
        /// in-transaction reversal followed by re-application (spec §13). Cancelled rows
        /// are immutable.
        /// </summary>
        public PurchaseInvoice Save(PurchaseInvoice invoice, List<PurchaseInvoiceItem> items)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        var now = DateTime.Now;
                        RecalculateTotals(invoice, items);

                        var existing = conn.QueryFirstOrDefault<PurchaseInvoice>(
                            "SELECT * FROM PurchaseInvoice WHERE id = @id", new { id = invoice.Id }, transaction: tx);

                        if (existing != null && existing.Status == "cancelled")
                            throw new DomainValidationException("Cancelled purchase invoices cannot be edited");
                        if (existing != null && existing.Status == "posted" && HasPostedReturnsCore(conn, tx, existing.Id))
                            throw new DomainValidationException("Purchase invoices with returns cannot be edited");

                        // Snapshot unit data (name/factor) and validate each line's unit belongs to its product
                        foreach (var item in items)
                        {
                            var unit = conn.QueryFirstOrDefault<ProductUnit>(
                                "SELECT * FROM ProductUnit WHERE id = @id",
                                new { id = item.ProductUnitId }, transaction: tx);
                            if (unit == null || unit.ProductId != item.ProductId)
                                throw new DomainValidationException($"Invalid unit for product '{item.ProductId}'");
                            item.UnitName = unit.UnitName;
                            item.QuantityFactor = unit.QuantityFactor;
                        }

                        bool isNew = existing == null;
                        if (isNew)
                        {
                            invoice.Id = Guid.NewGuid().ToString("N");
                            invoice.CreatedAt = now;
                            invoice.InvoiceNumber = conn.ExecuteScalar<long>(
                                "SELECT COALESCE(MAX(invoiceNumber), 0) + 1 FROM PurchaseInvoice", transaction: tx);
                            conn.Execute(
                                $"INSERT INTO PurchaseInvoice ({Columns}) VALUES (@id, @invoiceNumber, @supplierInvoiceNumber, @supplierId, @date, @paymentMethod, @status, @subtotal, @discount, @tax, @total, @notes, @createdBy, @createdAt, @updatedAt)",
                                new
                                {
                                    id = invoice.Id,
                                    invoiceNumber = invoice.InvoiceNumber,
                                    supplierInvoiceNumber = invoice.SupplierInvoiceNumber,
                                    supplierId = invoice.SupplierId,
                                    date = invoice.Date,
                                    paymentMethod = invoice.PaymentMethod,
                                    status = invoice.Status,
                                    subtotal = invoice.Subtotal,
                                    discount = invoice.Discount,
                                    tax = invoice.Tax,
                                    total = invoice.Total,
                                    notes = invoice.Notes,
                                    createdBy = invoice.CreatedBy,
                                    createdAt = invoice.CreatedAt,
                                    updatedAt = invoice.UpdatedAt
                                }, transaction: tx);
                        }
                        else
                        {
                            // Editing a posted purchase: reverse its stock effects first (audit kept),
                            // then the new lines below are applied fresh.
                            if (existing.Status == "posted")
                                ReverseLines(conn, tx, existing);

                            invoice.CreatedAt = existing.CreatedAt;
                            conn.Execute(
                                "UPDATE PurchaseInvoice SET supplierInvoiceNumber = @supplierInvoiceNumber, supplierId = @supplierId, " +
                                "date = @date, paymentMethod = @paymentMethod, status = @status, subtotal = @subtotal, discount = @discount, " +
                                "tax = @tax, total = @total, notes = @notes, updatedAt = @updatedAt WHERE id = @id",
                                new
                                {
                                    supplierInvoiceNumber = invoice.SupplierInvoiceNumber,
                                    supplierId = invoice.SupplierId,
                                    date = invoice.Date,
                                    paymentMethod = invoice.PaymentMethod,
                                    status = invoice.Status,
                                    subtotal = invoice.Subtotal,
                                    discount = invoice.Discount,
                                    tax = invoice.Tax,
                                    total = invoice.Total,
                                    notes = invoice.Notes,
                                    updatedAt = now,
                                    id = invoice.Id
                                }, transaction: tx);
                            conn.Execute("DELETE FROM PurchaseInvoiceItem WHERE purchaseInvoiceId = @id",
                                new { id = invoice.Id }, transaction: tx);
                        }

                        foreach (var item in items)
                        {
                            item.Id = Guid.NewGuid().ToString("N");
                            item.PurchaseInvoiceId = invoice.Id;
                            conn.Execute(@"
                                INSERT INTO PurchaseInvoiceItem (id, purchaseInvoiceId, productId, productUnitId, unitName,
                                    quantityFactor, quantity, unitCost, lineTotal, newRetailPrice, newWholesalePrice)
                                VALUES (@id, @purchaseInvoiceId, @productId, @productUnitId, @unitName,
                                    @quantityFactor, @quantity, @unitCost, @lineTotal, @newRetailPrice, @newWholesalePrice)",
                                new
                                {
                                    id = item.Id,
                                    purchaseInvoiceId = item.PurchaseInvoiceId,
                                    productId = item.ProductId,
                                    productUnitId = item.ProductUnitId,
                                    unitName = item.UnitName,
                                    quantityFactor = item.QuantityFactor,
                                    quantity = item.Quantity,
                                    unitCost = item.UnitCost,
                                    lineTotal = item.LineTotal,
                                    newRetailPrice = item.NewRetailPrice,
                                    newWholesalePrice = item.NewWholesalePrice
                                }, transaction: tx);
                        }

                        // Posting (new or via edit) adds stock through the shared ledger, creates a
                        // cost layer per line (Phase 4) and applies optional new selling prices (spec §12.2).
                        if (invoice.Status == "posted")
                        {
                            foreach (var item in items)
                            {
                                var baseQuantity = item.Quantity * item.QuantityFactor;
                                StockLedger.Apply(conn, tx, item.ProductId, baseQuantity,
                                    StockLedger.Purchase, invoice.Id, invoice.InvoiceNumber.ToString(), requireStock: false);
                                CreateCostLayer(conn, tx, item.ProductId, invoice.Id, item.UnitCost, baseQuantity);
                                SyncBuyPrice(conn, tx, item.ProductId);

                                if (item.NewRetailPrice.HasValue)
                                    conn.Execute("UPDATE ProductUnit SET retailPrice = @p WHERE id = @id",
                                        new { p = item.NewRetailPrice.Value, id = item.ProductUnitId }, transaction: tx);
                                if (item.NewWholesalePrice.HasValue)
                                    conn.Execute("UPDATE ProductUnit SET wholesalePrice = @p WHERE id = @id",
                                        new { p = item.NewWholesalePrice.Value, id = item.ProductUnitId }, transaction: tx);
                            }
                        }

                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }

                return GetByIdCore(conn, invoice.Id);
            }
        }

        /// <summary>Safe cancellation of a posted purchase (spec §12.3): appends negative
        /// reversal movements referencing the original invoice; never deletes history.
        /// Stock may go negative if purchased goods were already sold.</summary>
        public PurchaseInvoice Cancel(string id)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        var invoice = conn.QueryFirstOrDefault<PurchaseInvoice>(
                            "SELECT * FROM PurchaseInvoice WHERE id = @id", new { id }, transaction: tx);
                        if (invoice == null)
                            throw new NotFoundException("Purchase invoice not found");
                        if (invoice.Status != "posted")
                            throw new DomainValidationException("Only posted purchase invoices can be cancelled");
                        if (HasPostedReturnsCore(conn, tx, invoice.Id))
                            throw new DomainValidationException("Purchase invoices with returns cannot be cancelled");

                        ReverseLines(conn, tx, invoice);

                        conn.Execute("UPDATE PurchaseInvoice SET status = 'cancelled', updatedAt = @updatedAt WHERE id = @id",
                            new { id, updatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }, transaction: tx);

                        foreach (var pid in conn.Query<string>(
                            "SELECT DISTINCT productId FROM PurchaseInvoiceItem WHERE purchaseInvoiceId = @id",
                            new { id }, transaction: tx))
                            SyncBuyPrice(conn, tx, pid);

                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }

                return GetByIdCore(conn, id);
            }
        }

        public List<PurchaseInvoice> ListPostedBySupplier(string supplierId)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                return conn.Query<PurchaseInvoice>(
                    "SELECT * FROM PurchaseInvoice WHERE supplierId = @supplierId AND status = 'posted' " +
                    "ORDER BY date ASC, invoiceNumber ASC LIMIT 500",
                    new { supplierId }).ToList();
            }
        }

        public long NextInvoiceNumber()
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return conn.ExecuteScalar<long>("SELECT COALESCE(MAX(invoiceNumber), 0) + 1 FROM PurchaseInvoice");
        }

        /// <summary>Writes negative reversal movements and subtracts stock for every line of a posted invoice.</summary>
        private static void ReverseLines(SqliteConnection conn, SqliteTransaction tx, PurchaseInvoice invoice)
        {
            var lines = conn.Query<PurchaseInvoiceItem>(
                "SELECT * FROM PurchaseInvoiceItem WHERE purchaseInvoiceId = @id",
                new { id = invoice.Id }, transaction: tx).ToList();

            foreach (var line in lines)
            {
                var baseQuantity = line.Quantity * line.QuantityFactor;
                StockLedger.Apply(conn, tx, line.ProductId, -baseQuantity,
                    StockLedger.PurchaseReversal, invoice.Id, invoice.InvoiceNumber.ToString(), requireStock: false);
            }

            ReverseCostLayers(conn, tx, invoice.Id);
        }

        /// <summary>One cost layer per posted purchase line (Phase 4).</summary>
        private static void CreateCostLayer(SqliteConnection conn, SqliteTransaction tx,            string productId, string sourcePurchaseId, double unitCost, double baseQuantity)
        {
            conn.Execute(@"
                INSERT INTO CostLayer (id, productId, sourcePurchaseId, quantityReceived, quantityRemaining, unitCost, createdAt)
                VALUES (@id, @productId, @sourcePurchaseId, @quantityReceived, @quantityRemaining, @unitCost, @createdAt)",
                new
                {
                    id = Guid.NewGuid().ToString("N"),
                    productId,
                    sourcePurchaseId,
                    quantityReceived = baseQuantity,
                    quantityRemaining = baseQuantity,
                    unitCost,
                    createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                }, transaction: tx);
        }

        /// <summary>Mirrors Product.buyPrice to the latest posted purchase cost for the product
        /// (FIFO-friendly reference field used by the product form, profit protection and the
        /// inventory valuation fallback). Recomputed inside the caller's transaction after any
        /// posting, edit or cancellation so it always reflects the newest remaining posted line.</summary>
        private static void SyncBuyPrice(SqliteConnection conn, SqliteTransaction tx, string productId)
        {
            conn.Execute(@"
                UPDATE Product SET buyPrice = COALESCE((
                    SELECT i.unitCost FROM PurchaseInvoiceItem i
                    JOIN PurchaseInvoice p ON p.id = i.purchaseInvoiceId
                    WHERE i.productId = @productId AND p.status = 'posted'
                    ORDER BY p.invoiceNumber DESC, i.rowid DESC
                    LIMIT 1
                ), 0)
                WHERE id = @productId",
                new { productId }, transaction: tx);
        }

        /// <summary>Reverses a purchase's layers: reduces received+remaining by the original
        /// amounts. Remaining may go negative when goods were already sold; the FIFO allocator
        /// skips non-positive layers and historical allocations are never rewritten.</summary>
        private static void ReverseCostLayers(SqliteConnection conn, SqliteTransaction tx, string sourcePurchaseId)
        {
            conn.Execute(@"
                UPDATE CostLayer
                SET quantityRemaining = quantityRemaining - quantityReceived
                WHERE sourcePurchaseId = @sourcePurchaseId",
                new { sourcePurchaseId }, transaction: tx);
        }

        /// <summary>Posted purchases with returns are frozen: editing/cancelling would rewrite
        /// history the returns were validated against (plan Phase 8).</summary>
        private static bool HasPostedReturnsCore(SqliteConnection conn, SqliteTransaction tx, string purchaseInvoiceId)
        {
            return conn.ExecuteScalar<int>(
                "SELECT COUNT(1) FROM PurchaseReturn WHERE purchaseInvoiceId = @purchaseInvoiceId AND status = 'posted'",
                new { purchaseInvoiceId }, transaction: tx) > 0;
        }

        private static void RecalculateTotals(PurchaseInvoice invoice, List<PurchaseInvoiceItem> items)
        {
            foreach (var item in items)
                item.LineTotal = Math.Round(item.Quantity * item.UnitCost, 2);
            invoice.Subtotal = Math.Round(items.Sum(i => i.LineTotal), 2);
            invoice.Total = Math.Round(invoice.Subtotal - invoice.Discount + invoice.Tax, 2);
        }

        private static PurchaseInvoice GetByIdCore(SqliteConnection conn, string id)
        {
            var invoice = conn.QueryFirstOrDefault<PurchaseInvoice>(
                "SELECT * FROM PurchaseInvoice WHERE id = @id", new { id });
            if (invoice == null)
                throw new NotFoundException("Purchase invoice not found");

            invoice.Items = conn.Query<PurchaseInvoiceItem>(
                "SELECT * FROM PurchaseInvoiceItem WHERE purchaseInvoiceId = @id", new { id }).ToList();
            foreach (var item in invoice.Items)
                item.Product = conn.QueryFirstOrDefault<Product>(
                    "SELECT * FROM Product WHERE id = @id", new { id = item.ProductId });

            AttachSupplier(conn, invoice);
            return invoice;
        }

        private static void AttachSupplier(SqliteConnection conn, PurchaseInvoice invoice)
        {
            if (!string.IsNullOrEmpty(invoice.SupplierId))
                invoice.Supplier = conn.QueryFirstOrDefault<Supplier>(
                    "SELECT * FROM Supplier WHERE id = @id", new { id = invoice.SupplierId });
        }

        private static string EscapeLike(string input)
        {
            return input.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
        }
    }
}
