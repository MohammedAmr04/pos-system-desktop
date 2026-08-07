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

        public IEnumerable<Product> Search(SqliteConnection conn, string q, int limit)
        {
            var exact = q;
            var like = $"%{EscapeLike(q)}%";
            var prefix = $"{EscapeLike(q)}%";

            return conn.Query<Product>(@"
                SELECT p.*, MIN(CASE
                    WHEN pb.barcode = @exact THEN 0
                    WHEN pb.barcode LIKE @prefix ESCAPE '\' THEN 1
                    ELSE 2
                END) AS _rank
                FROM Product p
                LEFT JOIN ProductUnit pu ON pu.productId = p.id
                LEFT JOIN ProductBarcode pb ON pb.productUnitId = pu.id
                WHERE p.name LIKE @like ESCAPE '\'
                   OR pb.barcode = @exact
                   OR pb.barcode LIKE @prefix ESCAPE '\'
                GROUP BY p.id
                ORDER BY _rank, p.createdAt DESC
                LIMIT @limit",
                new { like, prefix, exact, limit });
        }

        public (IEnumerable<Product> Items, int Total) GetPaged(SqliteConnection conn, int page, int pageSize, string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                var total = conn.ExecuteScalar<int>("SELECT COUNT(1) FROM Product");
                var items = conn.Query<Product>(
                    "SELECT * FROM Product ORDER BY createdAt DESC LIMIT @pageSize OFFSET @offset",
                    new { pageSize, offset = (page - 1) * pageSize });
                return (items, total);
            }

            var exact = q;
            var like = $"%{EscapeLike(q)}%";
            var prefix = $"{EscapeLike(q)}%";

            var totalFiltered = conn.ExecuteScalar<int>(@"
                SELECT COUNT(DISTINCT p.id) FROM Product p
                LEFT JOIN ProductUnit pu ON pu.productId = p.id
                LEFT JOIN ProductBarcode pb ON pb.productUnitId = pu.id
                WHERE p.name LIKE @like ESCAPE '\'
                   OR pb.barcode = @exact
                   OR pb.barcode LIKE @prefix ESCAPE '\'",
                new { like, prefix, exact });

            var filteredItems = conn.Query<Product>(@"
                SELECT p.*, MIN(CASE
                    WHEN pb.barcode = @exact THEN 0
                    WHEN pb.barcode LIKE @prefix ESCAPE '\' THEN 1
                    ELSE 2
                END) AS _rank
                FROM Product p
                LEFT JOIN ProductUnit pu ON pu.productId = p.id
                LEFT JOIN ProductBarcode pb ON pb.productUnitId = pu.id
                WHERE p.name LIKE @like ESCAPE '\'
                   OR pb.barcode = @exact
                   OR pb.barcode LIKE @prefix ESCAPE '\'
                GROUP BY p.id
                ORDER BY _rank, p.createdAt DESC
                LIMIT @pageSize OFFSET @offset",
                new { like, prefix, exact, pageSize, offset = (page - 1) * pageSize });

            return (filteredItems, totalFiltered);
        }

        private static string EscapeLike(string input)
        {
            return input.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
        }
    }
}
