using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using Microsoft.Data.Sqlite;
using PosCs.Models;

namespace PosCs.Repositories
{
    public class InsufficientStockException : Exception
    {
        public InsufficientStockException(string message) : base(message) { }
    }

    public class InvoiceRepository
    {
        public IEnumerable<Invoice> GetAll(SqliteConnection conn, DateTime? from = null, DateTime? to = null)
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
            {
                inv.InvoiceDetail = conn.Query<InvoiceDetail>(
                    "SELECT * FROM InvoiceDetail WHERE invoiceId = @invoiceId", new { invoiceId = inv.Id }).ToList();
                foreach (var detail in inv.InvoiceDetail)
                {
                    detail.Product = conn.QueryFirstOrDefault<Product>(
                        "SELECT * FROM Product WHERE id = @id", new { id = detail.ProductId });
                }
            }
            return invoices;
        }

        public Invoice GetById(SqliteConnection conn, string id)
        {
            var invoice = conn.QueryFirstOrDefault<Invoice>("SELECT * FROM Invoice WHERE id = @id", new { id });
            if (invoice != null)
            {
                invoice.InvoiceDetail = conn.Query<InvoiceDetail>(
                    "SELECT * FROM InvoiceDetail WHERE invoiceId = @invoiceId", new { invoiceId = id }).ToList();
                foreach (var detail in invoice.InvoiceDetail)
                {
                    detail.Product = conn.QueryFirstOrDefault<Product>(
                        "SELECT * FROM Product WHERE id = @id", new { id = detail.ProductId });
                }
            }
            return invoice;
        }

        public Invoice Create(SqliteConnection conn, Invoice invoice, List<InvoiceDetail> items)
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
                    return GetById(conn, invoice.Id);
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }
    }
}
