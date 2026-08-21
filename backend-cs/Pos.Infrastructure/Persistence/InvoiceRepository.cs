using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using Microsoft.Data.Sqlite;
using PosCs.Application.Models;
using PosCs.Application.Ports;
using PosCs.Domain.Entities;
using PosCs.Domain.Exceptions;

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

        public InvoicePageResult GetPaged(DateTime? from, DateTime? to, string query, int page, int pageSize)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                var clauses = new List<string>();
                var parameters = new DynamicParameters();

                if (from.HasValue)
                {
                    clauses.Add("createdAt >= @from");
                    parameters.Add("from", from.Value.ToString("yyyy-MM-dd HH:mm:ss"));
                }
                if (to.HasValue)
                {
                    clauses.Add("createdAt <= @to");
                    parameters.Add("to", to.Value.ToString("yyyy-MM-dd HH:mm:ss"));
                }
                if (!string.IsNullOrWhiteSpace(query))
                {
                    clauses.Add("CAST(invoiceNumber AS TEXT) LIKE @like");
                    parameters.Add("like", $"%{EscapeLike(query.Trim())}%");
                }

                var where = clauses.Count > 0 ? " WHERE " + string.Join(" AND ", clauses) : "";
                parameters.Add("pageSize", pageSize);
                parameters.Add("offset", (page - 1) * pageSize);

                var total = conn.ExecuteScalar<int>($"SELECT COUNT(1) FROM Invoice{where}", parameters);
                var revenue = conn.ExecuteScalar<double>($"SELECT COALESCE(SUM(totalAmount), 0) FROM Invoice{where}", parameters);
                var discounts = conn.ExecuteScalar<double>($"SELECT COALESCE(SUM(discount), 0) FROM Invoice{where}", parameters);

                var items = conn.Query<Invoice>(
                    $"SELECT * FROM Invoice{where} ORDER BY createdAt DESC LIMIT @pageSize OFFSET @offset",
                    parameters).ToList();

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

                        conn.Execute(@"
                            INSERT INTO Invoice (id, invoiceNumber, totalAmount, discount, discountType, discountValue, discountAmount, priceMode, createdAt)
                            VALUES (
                                @id,
                                (SELECT COALESCE(MAX(invoiceNumber), 0) + 1 FROM Invoice),
                                @totalAmount,
                                @discount,
                                @discountType,
                                @discountValue,
                                @discountAmount,
                                @priceMode,
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
                                createdAt = invoice.CreatedAt
                            }, transaction: tx);

                        foreach (var item in items)
                        {
                            var detailId = Guid.NewGuid().ToString("N");
                            conn.Execute(@"
                                INSERT INTO InvoiceDetail (id, invoiceId, productId, productUnitId, unitName, quantity, buyPrice, salePrice,
                                    originalUnitPrice, unitPrice, discountType, discountValue, discountAmount, lineSubtotal, finalTotal, priceEditNote)
                                VALUES (@id, @invoiceId, @productId, @productUnitId, @unitName, @quantity, @buyPrice, @salePrice,
                                    @originalUnitPrice, @unitPrice, @discountType, @discountValue, @discountAmount, @lineSubtotal, @finalTotal, @priceEditNote)",
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
                                    priceEditNote = item.PriceEditNote
                                }, transaction: tx);

                            var baseQuantity = item.Quantity * item.QuantityFactor;
                            var affected = conn.Execute(@"
                                UPDATE Product SET stockQuantity = stockQuantity - @baseQuantity
                                WHERE id = @productId AND stockQuantity >= @baseQuantity",
                                new { baseQuantity, productId = item.ProductId },
                                transaction: tx);

                            if (affected == 0)
                                throw new InsufficientStockException($"Insufficient stock for '{item.Product?.Name ?? item.ProductId}'");
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

        private static Invoice GetByIdCore(SqliteConnection conn, string id)
        {
            var invoice = conn.QueryFirstOrDefault<Invoice>("SELECT * FROM Invoice WHERE id = @id", new { id });
            if (invoice != null)
                AttachDetails(conn, invoice);
            return invoice;
        }

        private static void AttachDetails(SqliteConnection conn, Invoice invoice)
        {
            invoice.InvoiceDetail = conn.Query<InvoiceDetail>(
                "SELECT * FROM InvoiceDetail WHERE invoiceId = @invoiceId", new { invoiceId = invoice.Id }).ToList();
            foreach (var detail in invoice.InvoiceDetail)
            {
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
