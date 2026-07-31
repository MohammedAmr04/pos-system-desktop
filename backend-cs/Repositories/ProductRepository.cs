using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using Microsoft.Data.Sqlite;
using PosCs.Models;

namespace PosCs.Repositories
{
    public class ProductRepository
    {
        public IEnumerable<Product> GetAll(SqliteConnection conn)
        {
            return conn.Query<Product>("SELECT * FROM Product ORDER BY createdAt DESC");
        }

        public Product GetById(SqliteConnection conn, string id)
        {
            return conn.QueryFirstOrDefault<Product>("SELECT * FROM Product WHERE id = @id", new { id });
        }

        public Product Create(SqliteConnection conn, Product product)
        {
            var now = DateTime.Now;
            product.Id = Guid.NewGuid().ToString("N");
            product.CreatedAt = now;
            product.UpdatedAt = now;

            Console.WriteLine($"[DB] Creating product: Id={product.Id}, Name='{product.Name}', " +
                $"BuyPrice={product.BuyPrice}, Stock={product.StockQuantity}, " +
                $"Notes='{product.Notes}', CreatedAt={product.CreatedAt:O}, UpdatedAt={product.UpdatedAt:O}");

            conn.Execute(@"
                INSERT INTO Product (id, name, buyPrice, stockQuantity, notes, allowDiscount, lowStockThreshold, createdAt, updatedAt)
                VALUES (@id, @name, @buyPrice, @stockQuantity, @notes, @allowDiscount, @lowStockThreshold, @createdAt, @updatedAt)",
                new
                {
                    id = product.Id,
                    name = product.Name,
                    buyPrice = product.BuyPrice,
                    stockQuantity = product.StockQuantity,
                    notes = product.Notes,
                    allowDiscount = product.AllowDiscount ? 1 : 0,
                    lowStockThreshold = product.LowStockThreshold,
                    createdAt = product.CreatedAt,
                    updatedAt = product.UpdatedAt
                });

            var saved = conn.QueryFirstOrDefault<Product>("SELECT * FROM Product WHERE id = @id", new { id = product.Id });
            if (saved != null)
                Console.WriteLine($"[DB] Product created successfully: Id={saved.Id}, Name='{saved.Name}'");
            else
                Console.Error.WriteLine($"[DB] Product insert completed but read-back returned null!");

            return product;
        }

        public void Update(SqliteConnection conn, Product product)
        {
            product.UpdatedAt = DateTime.UtcNow;
            conn.Execute(@"
                UPDATE Product SET name=@name, buyPrice=@buyPrice,
                    stockQuantity=@stockQuantity, notes=@notes,
                    allowDiscount=@allowDiscount, lowStockThreshold=@lowStockThreshold,
                    updatedAt=@updatedAt
                WHERE id=@id",
                new
                {
                    id = product.Id,
                    name = product.Name,
                    buyPrice = product.BuyPrice,
                    stockQuantity = product.StockQuantity,
                    notes = product.Notes,
                    allowDiscount = product.AllowDiscount ? 1 : 0,
                    lowStockThreshold = product.LowStockThreshold,
                    updatedAt = product.UpdatedAt
                });
        }

        public bool Delete(SqliteConnection conn, string id)
        {
            var rows = conn.Execute("DELETE FROM Product WHERE id = @id", new { id });
            return rows > 0;
        }

        public int GetCount(SqliteConnection conn)
        {
            return conn.ExecuteScalar<int>("SELECT COUNT(1) FROM Product");
        }
    }
}
