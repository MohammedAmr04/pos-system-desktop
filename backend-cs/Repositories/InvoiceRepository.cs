using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using Microsoft.Data.Sqlite;
using PosCs.Models;

namespace PosCs.Repositories
{
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

        public Invoice Create(SqliteConnection conn, Invoice invoice, List<(string productId, int quantity, double buyPrice, double salePrice)> items)
        {
            using (var tx = conn.BeginTransaction())
            {
                try
                {
                    invoice.Id = Guid.NewGuid().ToString("N");
                    invoice.CreatedAt = DateTime.Now;

                    conn.Execute(@"
                        INSERT INTO Invoice (id, totalAmount, discount, createdAt)
                        VALUES (@id, @totalAmount, @discount, @createdAt)",
                        new
                        {
                            id = invoice.Id,
                            totalAmount = invoice.TotalAmount,
                            discount = invoice.Discount,
                            createdAt = invoice.CreatedAt
                        }, transaction: tx);

                    foreach (var item in items)
                    {
                        var detailId = Guid.NewGuid().ToString("N");
                        conn.Execute(@"
                            INSERT INTO InvoiceDetail (id, invoiceId, productId, quantity, buyPrice, salePrice)
                            VALUES (@id, @invoiceId, @productId, @quantity, @buyPrice, @salePrice)",
                            new { id = detailId, invoiceId = invoice.Id, item.productId, item.quantity, item.buyPrice, item.salePrice },
                            transaction: tx);

                        conn.Execute(@"
                            UPDATE Product SET stockQuantity = stockQuantity - @qty
                            WHERE id = @productId AND stockQuantity >= @qty",
                            new { qty = item.quantity, productId = item.productId },
                            transaction: tx);
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
