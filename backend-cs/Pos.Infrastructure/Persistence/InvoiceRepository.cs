using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using Microsoft.Data.Sqlite;
using PosCs.Application.Models;
using PosCs.Application.Ports;
using PosCs.Domain.Entities;
using PosCs.Domain.Exceptions;
using PosCs.Domain.Rules;
using Newtonsoft.Json;

namespace PosCs.Infrastructure.Persistence
{
    public class InvoiceRepository : IInvoiceRepository
    {
        public Invoice GetById(string id)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return GetByIdCore(conn, id);
        }

        public List<Invoice> GetRange(DateTime? from, DateTime? to)
        {            using (var conn = DbConnectionFactory.CreateConnection())
            {
                var sql = "SELECT * FROM Invoice WHERE 1=1";
                var parameters = new DynamicParameters();

                if (from.HasValue)
                {
                    sql += " AND createdAt >= @from";
                    parameters.Add("from", from.Value.ToString("yyyy-MM-dd HH:mm:ss"));
                }
                if (to.HasValue)
                {
                    sql += " AND createdAt <= @to";
                    parameters.Add("to", to.Value.ToString("yyyy-MM-dd HH:mm:ss"));
                }
                sql += " ORDER BY createdAt DESC";

                var invoices = conn.Query<Invoice>(sql, parameters).ToList();
                foreach (var inv in invoices)
                    AttachDetails(conn, inv);
                return invoices;
            }
        }

        public InvoicePageResult GetPaged(DateTime? from, DateTime? to, string query, string status, int page, int pageSize, string employeeId = null)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                var clauses = new List<string>();
                var parameters = new DynamicParameters();

                if (from.HasValue)
                {
                    clauses.Add("i.createdAt >= @from");
                    parameters.Add("from", from.Value.ToString("yyyy-MM-dd HH:mm:ss"));
                }
                if (to.HasValue)
                {
                    clauses.Add("i.createdAt <= @to");
                    parameters.Add("to", to.Value.ToString("yyyy-MM-dd HH:mm:ss"));
                }
                if (!string.IsNullOrWhiteSpace(query))
                {
                    clauses.Add("CAST(i.invoiceNumber AS TEXT) LIKE @like");
                    parameters.Add("like", $"%{EscapeLike(query.Trim())}%");
                }
                if (!string.IsNullOrWhiteSpace(status) && status != "all")
                {
                    clauses.Add("i.status = @status");
                    parameters.Add("status", status);
                }
                if (!string.IsNullOrWhiteSpace(employeeId))
                {
                    clauses.Add("i.employeeId = @employeeId");
                    parameters.Add("employeeId", employeeId.Trim());
                }

                var where = clauses.Count > 0 ? " WHERE " + string.Join(" AND ", clauses) : "";
                parameters.Add("pageSize", pageSize);
                parameters.Add("offset", (page - 1) * pageSize);

                var total = conn.ExecuteScalar<int>($"SELECT COUNT(1) FROM Invoice i{where}", parameters);
                // Drafts are not revenue; only posted sales count toward totals.
                var revenueWhere = where.Length > 0 ? where + " AND i.status = 'posted'" : " WHERE i.status = 'posted'";
                var revenue = conn.ExecuteScalar<double>($"SELECT COALESCE(SUM(i.totalAmount), 0) FROM Invoice i{revenueWhere}", parameters);
                var returns = conn.ExecuteScalar<double>(
                    $"SELECT COALESCE(SUM(r.totalAmount), 0) FROM SaleReturn r JOIN Invoice i ON i.id = r.invoiceId" +
                    $"{revenueWhere} AND r.status = 'posted'", parameters);
                revenue = InvoiceRevenue.Net(revenue, returns);
                var discounts = conn.ExecuteScalar<double>($"SELECT COALESCE(SUM(i.discount), 0) FROM Invoice i{revenueWhere}", parameters);

                var items = conn.Query<Invoice>(
                    $"SELECT i.* FROM Invoice i{where} ORDER BY i.createdAt DESC LIMIT @pageSize OFFSET @offset",
                    parameters).ToList();

                foreach (var inv in items)
                {
                    AttachClient(conn, inv);
                    AttachEmployee(conn, inv);
                }

                return new InvoicePageResult { Items = items, Total = total, Revenue = revenue, Discounts = discounts };
            }
        }

        public Invoice Create(Invoice invoice, List<InvoiceDetail> items)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        invoice.Id = Guid.NewGuid().ToString("N");
                        invoice.CreatedAt = DateTime.Now;
                        var isDraft = invoice.Status == "draft";
                        if (!isDraft)
                            invoice.ShiftId = RequireActiveShiftId(conn, tx);

                        conn.Execute(@"
                            INSERT INTO Invoice (id, invoiceNumber, totalAmount, discount, discountType, discountValue, discountAmount, priceMode, status, clientId, employeeId, paymentMethod, createdBy, shiftId, createdAt)
                            VALUES (
                                @id,
                                (SELECT COALESCE(MAX(invoiceNumber), 0) + 1 FROM Invoice),
                                @totalAmount,
                                @discount,
                                @discountType,
                                @discountValue,
                                @discountAmount,
                                @priceMode,
                                @status,
                                @clientId,
                                @employeeId,
                                @paymentMethod,
                                @createdBy,
                                @shiftId,
                                @createdAt
                            )",
                            new
                            {
                                id = invoice.Id,
                                totalAmount = invoice.TotalAmount,
                                discount = invoice.Discount,
                                discountType = invoice.DiscountType,
                                discountValue = invoice.DiscountValue,
                                discountAmount = invoice.DiscountAmount,
                                priceMode = invoice.PriceMode,
                                status = isDraft ? "draft" : "posted",
                                clientId = string.IsNullOrEmpty(invoice.ClientId) ? null : invoice.ClientId,
                                employeeId = string.IsNullOrEmpty(invoice.EmployeeId) ? null : invoice.EmployeeId,
                                paymentMethod = invoice.PaymentMethod ?? "cash",
                                createdBy = invoice.CreatedBy,
                                shiftId = string.IsNullOrEmpty(invoice.ShiftId) ? null : invoice.ShiftId,
                                createdAt = invoice.CreatedAt
                            }, transaction: tx);

                        invoice.InvoiceNumber = conn.ExecuteScalar<int>(
                            "SELECT invoiceNumber FROM Invoice WHERE id = @id", new { id = invoice.Id }, transaction: tx);

                        InsertLines(conn, (SqliteTransaction)tx, invoice, items, applySideEffects: !isDraft);

                        if (!isDraft && invoice.PaymentMethod != "credit")
                            InsertAutoPayment(conn, (SqliteTransaction)tx, invoice);

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

        /// <summary>Draft-only edit: replaces header pricing fields and lines; zero side effects.</summary>
        public Invoice Update(string id, Invoice invoice, List<InvoiceDetail> items)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        var status = conn.ExecuteScalar<string>(
                            "SELECT status FROM Invoice WHERE id = @id", new { id }, transaction: tx);
                        if (status == null)
                            throw new NotFoundException("Invoice not found");
                        if (status != "draft")
                            throw new DomainValidationException("Only draft invoices can be edited");

                        conn.Execute(@"
                            UPDATE Invoice SET totalAmount = @totalAmount, discount = @discount,
                                discountType = @discountType, discountValue = @discountValue,
                                discountAmount = @discountAmount, priceMode = @priceMode,
                                clientId = @clientId, employeeId = @employeeId, paymentMethod = @paymentMethod
                            WHERE id = @id",
                            new
                            {
                                id,
                                totalAmount = invoice.TotalAmount,
                                discount = invoice.Discount,
                                discountType = invoice.DiscountType,
                                discountValue = invoice.DiscountValue,
                                discountAmount = invoice.DiscountAmount,
                                priceMode = invoice.PriceMode,
                                clientId = string.IsNullOrEmpty(invoice.ClientId) ? null : invoice.ClientId,
                                employeeId = string.IsNullOrEmpty(invoice.EmployeeId) ? null : invoice.EmployeeId,
                                paymentMethod = invoice.PaymentMethod ?? "cash"
                            }, transaction: tx);

                        conn.Execute("DELETE FROM InvoiceDetail WHERE invoiceId = @id", new { id }, transaction: tx);
                        invoice.InvoiceNumber = conn.ExecuteScalar<int>(
                            "SELECT invoiceNumber FROM Invoice WHERE id = @id", new { id }, transaction: tx);

                        InsertLines(conn, (SqliteTransaction)tx, invoice, items, applySideEffects: false);

                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }

                // Lines were replaced inside the same transaction; return the fresh state.
                return GetByIdCore(conn, id);
            }
        }

        /// <summary>Posts a draft: runs the full pipeline (stock ledger + FIFO + COGS +
        /// auto cash payment) atomically. Draft lines were stored without side effects.</summary>
        public Invoice Post(string id)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        var invoice = conn.QueryFirstOrDefault<Invoice>(
                            "SELECT * FROM Invoice WHERE id = @id", new { id }, transaction: tx);
                        if (invoice == null)
                            throw new NotFoundException("Invoice not found");
                        if (invoice.Status != "draft")
                            throw new DomainValidationException("Only draft invoices can be posted");

                        invoice.ShiftId = RequireActiveShiftId(conn, tx);

                        var items = conn.Query<InvoiceDetail>(
                            "SELECT * FROM InvoiceDetail WHERE invoiceId = @id ORDER BY rowid", new { id }, transaction: tx).ToList();

                        // Draft rows were stored without COGS; replace them with the same lines
                        // re-run through the full pipeline (ledger + FIFO + totalCost).
                        conn.Execute("DELETE FROM InvoiceDetail WHERE invoiceId = @id", new { id }, transaction: tx);

                        InsertLines(conn, (SqliteTransaction)tx, invoice, items, applySideEffects: true);

                        if (invoice.PaymentMethod != "credit")
                            InsertAutoPayment(conn, (SqliteTransaction)tx, invoice);

                        conn.Execute("UPDATE Invoice SET status = 'posted', shiftId = @shiftId WHERE id = @id",
                            new { id, shiftId = invoice.ShiftId }, transaction: tx);

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

        /// <summary>Safe cancellation of a posted sale — stock returns through reversal rows and
        /// FIFO layers are restored from the original allocations. Deletes nothing.</summary>
        public Invoice Cancel(string id)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        var invoice = conn.QueryFirstOrDefault<Invoice>(
                            "SELECT * FROM Invoice WHERE id = @id", new { id }, transaction: tx);
                        if (invoice == null)
                            throw new NotFoundException("Invoice not found");
                        if (invoice.Status != "posted")
                            throw new DomainValidationException("Only posted invoices can be cancelled");

                        // A returned sale already moved money/stock back out; cancelling it
                        // would double-restore (plan Phase 7).
                        var hasReturns = conn.ExecuteScalar<int>(
                            "SELECT COUNT(1) FROM SaleReturn WHERE invoiceId = @id AND status = 'posted'",
                            new { id }, transaction: tx);
                        if (hasReturns > 0)
                            throw new DomainValidationException("Invoices with returns cannot be cancelled");

                        var items = conn.Query<InvoiceDetail>(
                            "SELECT * FROM InvoiceDetail WHERE invoiceId = @id", new { id }, transaction: tx).ToList();

                        foreach (var item in items)
                        {
                            var baseQuantity = item.Quantity * item.QuantityFactor;
                            var productType = conn.ExecuteScalar<string>(
                                "SELECT productType FROM Product WHERE id = @productId", new { productId = item.ProductId }, transaction: tx) ?? "product";
                            if (productType == "bundle")
                            {
                                foreach (var component in ParseBundleComponents(item.BundleComponentsJson))
                                {
                                    if (component.ProductType == "service") continue;
                                    StockLedger.Apply(conn, (SqliteTransaction)tx, component.ProductId,
                                        component.Quantity * item.Quantity, StockLedger.SaleReversal,
                                        invoice.Id, invoice.InvoiceNumber.ToString(), requireStock: false);
                                }
                            }
                            else if (productType != "service")
                            {
                                StockLedger.Apply(conn, (SqliteTransaction)tx, item.ProductId, baseQuantity,
                                    StockLedger.SaleReversal, invoice.Id, invoice.InvoiceNumber.ToString(), requireStock: false);
                            }
                        }

                        // Restore exactly what this sale consumed (plan Phase 4 allocations).
                        conn.Execute(@"
                            UPDATE CostLayer SET quantityRemaining = quantityRemaining + a.quantity
                            FROM SaleCostAllocation a
                            WHERE a.costLayerId = CostLayer.id AND a.invoiceId = @id",
                            new { id }, transaction: tx);

                        conn.Execute("UPDATE Invoice SET status = 'cancelled' WHERE id = @id",
                            new { id }, transaction: tx);

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

        /// <summary>Inserts sale lines; when applySideEffects is false (draft) nothing touches
        /// stock, ledger or cost layers.</summary>
        private static void InsertLines(SqliteConnection conn, SqliteTransaction tx, Invoice invoice,
            List<InvoiceDetail> items, bool applySideEffects)
        {
            foreach (var item in items)
            {
                var detailId = Guid.NewGuid().ToString("N");
                var baseQuantity = item.Quantity * item.QuantityFactor;

                if (applySideEffects)
                {
                    var productType = conn.ExecuteScalar<string>(
                        "SELECT productType FROM Product WHERE id = @productId", new { productId = item.ProductId }, transaction: tx) ?? "product";
                    if (productType == "service")
                    {
                        item.TotalCost = Math.Round(item.BuyPrice * item.Quantity, 2);
                    }
                    else if (productType == "bundle")
                    {
                        var components = ParseBundleComponents(item.BundleComponentsJson);
                        var totalCost = 0.0;
                        foreach (var component in components)
                        {
                            var componentQuantity = component.Quantity * item.Quantity;
                            if (component.ProductType == "service")
                            {
                                totalCost += component.ServiceCost * componentQuantity;
                                continue;
                            }
                            StockLedger.Apply(conn, tx, component.ProductId, -componentQuantity,
                                StockLedger.Sale, invoice.Id, invoice.InvoiceNumber.ToString(), requireStock: true);
                            totalCost += ConsumeFifo(conn, tx, invoice.Id, detailId, component.ProductId, componentQuantity);
                        }
                        item.TotalCost = Math.Round(totalCost, 2);
                    }
                    else
                    {
                        StockLedger.Apply(conn, tx, item.ProductId, -baseQuantity,
                            StockLedger.Sale, invoice.Id, invoice.InvoiceNumber.ToString(), requireStock: true);
                        item.TotalCost = ConsumeFifo(conn, tx, invoice.Id, detailId, item.ProductId, baseQuantity);
                    }
                }

                conn.Execute(@"
                    INSERT INTO InvoiceDetail (id, invoiceId, productId, productUnitId, unitName, quantity, buyPrice, salePrice,
                        originalUnitPrice, unitPrice, discountType, discountValue, discountAmount, lineSubtotal, finalTotal, priceEditNote, quantityFactor, totalCost, bundleComponentsJson)
                    VALUES (@id, @invoiceId, @productId, @productUnitId, @unitName, @quantity, @buyPrice, @salePrice,
                        @originalUnitPrice, @unitPrice, @discountType, @discountValue, @discountAmount, @lineSubtotal, @finalTotal, @priceEditNote, @quantityFactor, @totalCost, @bundleComponentsJson)",
                    new
                    {
                        id = detailId,
                        invoiceId = invoice.Id,
                        productId = item.ProductId,
                        productUnitId = item.ProductUnitId,
                        unitName = item.UnitName,
                        quantity = item.Quantity,
                        buyPrice = item.BuyPrice,
                        salePrice = item.UnitPrice,
                        originalUnitPrice = item.OriginalUnitPrice,
                        unitPrice = item.UnitPrice,
                        discountType = item.DiscountType,
                        discountValue = item.DiscountValue,
                        discountAmount = item.DiscountAmount,
                        lineSubtotal = item.LineSubtotal,
                        finalTotal = item.FinalTotal,
                        priceEditNote = item.PriceEditNote,
                        quantityFactor = item.QuantityFactor,
                        totalCost = item.TotalCost,
                        bundleComponentsJson = item.BundleComponentsJson ?? (item.BundleComponents != null && item.BundleComponents.Count > 0
                            ? JsonConvert.SerializeObject(item.BundleComponents)
                            : null)
                    }, transaction: tx);
                item.Id = detailId;
            }
        }

        private static List<InvoiceBundleComponent> ParseBundleComponents(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<InvoiceBundleComponent>();
            return JsonConvert.DeserializeObject<List<InvoiceBundleComponent>>(json)
                ?? new List<InvoiceBundleComponent>();
        }

        /// <summary>Posted sales for one client, oldest first (account statements).</summary>
        public List<Invoice> ListPostedByClient(string clientId)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                return conn.Query<Invoice>(
                    "SELECT * FROM Invoice WHERE clientId = @clientId AND status = 'posted' " +
                    "ORDER BY createdAt ASC, invoiceNumber ASC LIMIT 500",
                    new { clientId }).ToList();
            }
        }

        /// <summary>POS selling requires an open shift (plan Phase 9): posting a sale without
        /// one is rejected inside the posting transaction. Returns the shift to stamp.</summary>
        private static string RequireActiveShiftId(SqliteConnection conn, SqliteTransaction tx)
        {
            var shiftId = conn.ExecuteScalar<string>(
                "SELECT id FROM Shift WHERE status = 'open' ORDER BY number DESC LIMIT 1", transaction: tx);
            if (shiftId == null)
                throw new DomainValidationException("No open shift — open a shift before selling");
            return shiftId;
        }

        /// <summary>Non-credit sales settle immediately (spec §21): an automatic payment keeps the
        /// derived paid/status consistent without any mutable PaidAmount field. The payment's
        /// method mirrors the invoice's (cash/card/bank_transfer) so card and bank transfers are
        /// excluded from the cash drawer at shift close.</summary>
        private static void InsertAutoPayment(SqliteConnection conn, SqliteTransaction tx, Invoice invoice)
        {
            var method = (string.IsNullOrEmpty(invoice.PaymentMethod) || invoice.PaymentMethod == "credit") ? "cash" : invoice.PaymentMethod;
            conn.Execute(@"
                INSERT INTO Payment (id, amount, paymentMethod, date, invoiceId, clientId, supplierId, reference, notes, createdBy, shiftId, createdAt)
                VALUES (@id, @amount, @paymentMethod, @date, @invoiceId, @clientId, NULL, @reference, NULL, @createdBy, @shiftId, @createdAt)",
                new
                {
                    id = Guid.NewGuid().ToString("N"),
                    amount = invoice.TotalAmount,
                    paymentMethod = method,
                    date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    invoiceId = invoice.Id,
                    clientId = string.IsNullOrEmpty(invoice.ClientId) ? null : invoice.ClientId,
                    reference = $"auto: {method} sale #{invoice.InvoiceNumber}",
                    createdBy = invoice.CreatedBy,
                    shiftId = string.IsNullOrEmpty(invoice.ShiftId) ? null : invoice.ShiftId,
                    createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                }, transaction: tx);
        }

        private sealed class LayerRow
        {
            public string Id { get; set; }
            public double Remaining { get; set; }
            public double UnitCost { get; set; }
        }

        /// <summary>Consumes cost layers FIFO (plan Phase 4) and returns the line's historical COGS.
        /// Writes one immutable SaleCostAllocation row per consumed layer. Stock that has no layer
        /// (legacy rows created before Phase 4) falls back to the product's current buy price.</summary>
        private static double ConsumeFifo(SqliteConnection conn, SqliteTransaction tx,
            string invoiceId, string detailId, string productId, double baseQuantity)
        {
            var layers = conn.Query<LayerRow>(
                "SELECT id AS Id, quantityRemaining AS Remaining, unitCost AS UnitCost " +
                "FROM CostLayer WHERE productId = @productId AND quantityRemaining > 0 ORDER BY createdAt, rowid",
                new { productId }, transaction: tx).ToList();

            var buyPrice = conn.ExecuteScalar<double>(
                "SELECT COALESCE(buyPrice, 0) FROM Product WHERE id = @productId", new { productId }, transaction: tx);

            var fromLayers = Math.Min(Math.Max(baseQuantity, 0), layers.Sum(l => Math.Max(l.Remaining, 0)));
            var totalCost = 0.0;

            foreach (var alloc in FifoAllocator.Allocate(
                layers.Select(l => new FifoAllocator.Layer(l.Id, l.Remaining)), fromLayers))
            {
                var layer = layers.First(l => l.Id == alloc.LayerId);
                conn.Execute("UPDATE CostLayer SET quantityRemaining = quantityRemaining - @q WHERE id = @id",
                    new { q = alloc.Quantity, id = alloc.LayerId }, transaction: tx);
                conn.Execute(@"
                    INSERT INTO SaleCostAllocation (id, invoiceId, invoiceDetailId, productId, costLayerId, quantity, unitCost, createdAt)
                    VALUES (@id, @invoiceId, @invoiceDetailId, @productId, @costLayerId, @quantity, @unitCost, @createdAt)",
                    new
                    {
                        id = Guid.NewGuid().ToString("N"),
                        invoiceId,
                        invoiceDetailId = detailId,
                        productId,
                        costLayerId = alloc.LayerId,
                        quantity = alloc.Quantity,
                        unitCost = layer.UnitCost,
                        createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    }, transaction: tx);
                totalCost += alloc.Quantity * layer.UnitCost;
            }

            var shortfall = baseQuantity - fromLayers;
            if (shortfall > 0)
                totalCost += shortfall * buyPrice;

            return Math.Round(totalCost, 2);
        }

        private static Invoice GetByIdCore(SqliteConnection conn, string id)        {
            var invoice = conn.QueryFirstOrDefault<Invoice>("SELECT * FROM Invoice WHERE id = @id", new { id });
            if (invoice != null)
            {
                AttachDetails(conn, invoice);
                AttachClient(conn, invoice);
                AttachEmployee(conn, invoice);
            }
            return invoice;
        }

        private static void AttachClient(SqliteConnection conn, Invoice invoice)
        {
            if (!string.IsNullOrEmpty(invoice.ClientId))
                invoice.Client = conn.QueryFirstOrDefault<Client>(
                    "SELECT * FROM Client WHERE id = @id", new { id = invoice.ClientId });
        }

        private static void AttachEmployee(SqliteConnection conn, Invoice invoice)
        {
            if (!string.IsNullOrEmpty(invoice.EmployeeId))
                invoice.Employee = conn.QueryFirstOrDefault<Employee>(
                    "SELECT * FROM Employee WHERE id = @id", new { id = invoice.EmployeeId });
        }

        private static void AttachDetails(SqliteConnection conn, Invoice invoice)
        {
            invoice.InvoiceDetail = conn.Query<InvoiceDetail>(
                "SELECT * FROM InvoiceDetail WHERE invoiceId = @invoiceId", new { invoiceId = invoice.Id }).ToList();
            foreach (var detail in invoice.InvoiceDetail)
            {
                detail.BundleComponents = ParseBundleComponents(detail.BundleComponentsJson);
                detail.Product = conn.QueryFirstOrDefault<Product>(
                    "SELECT * FROM Product WHERE id = @id", new { id = detail.ProductId });
            }
        }

        private static string EscapeLike(string input)
        {
            return input.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
        }
    }
}
