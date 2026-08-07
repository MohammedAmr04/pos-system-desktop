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

        public Product GetByBarcode(SqliteConnection conn, string barcode)
        {
            return conn.QueryFirstOrDefault<Product>("SELECT * FROM Product WHERE barcode = @barcode", new { barcode });
        }

        public bool BarcodeExists(SqliteConnection conn, string barcode)
        {
            return conn.ExecuteScalar<int>("SELECT COUNT(1) FROM Product WHERE barcode = @barcode", new { barcode }) > 0;
        }

        public string GenerateUniqueBarcode(SqliteConnection conn)
        {
            var rng = new Random();
            for (int attempt = 0; attempt < 10; attempt++)
            {
                var barcode = rng.Next(0, int.MaxValue).ToString("D12");
                if (!BarcodeExists(conn, barcode))
                    return barcode;
            }
            throw new Exception("Failed to generate unique barcode after multiple attempts");
        }

        public Product Create(SqliteConnection conn, Product product)
        {
            var now = DateTime.Now;
            product.Id = Guid.NewGuid().ToString("N");
            product.CreatedAt = now;
            product.UpdatedAt = now;

            Console.WriteLine($"[DB] Creating product: Id={product.Id}, Name='{product.Name}', Barcode={product.Barcode}, " +
                $"BuyPrice={product.BuyPrice}, SalePrice={product.SalePrice}, Stock={product.StockQuantity}, " +
                $"Notes='{product.Notes}', CreatedAt={product.CreatedAt:O}, UpdatedAt={product.UpdatedAt:O}");

            conn.Execute(@"
                INSERT INTO Product (id, barcode, name, buyPrice, salePrice, stockQuantity, notes, createdAt, updatedAt)
                VALUES (@id, @barcode, @name, @buyPrice, @salePrice, @stockQuantity, @notes, @createdAt, @updatedAt)",
                new
                {
                    id = product.Id,
                    barcode = product.Barcode,
                    name = product.Name,
                    buyPrice = product.BuyPrice,
                    salePrice = product.SalePrice,
                    stockQuantity = product.StockQuantity,
                    notes = product.Notes,
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
                UPDATE Product SET barcode=@barcode, name=@name, buyPrice=@buyPrice,
                    salePrice=@salePrice, stockQuantity=@stockQuantity, notes=@notes,
                    updatedAt=@updatedAt
                WHERE id=@id",
                new
                {
                    id = product.Id,
                    barcode = product.Barcode,
                    name = product.Name,
                    buyPrice = product.BuyPrice,
                    salePrice = product.SalePrice,
                    stockQuantity = product.StockQuantity,
                    notes = product.Notes,
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
                SELECT * FROM Product
                WHERE name LIKE @like ESCAPE '\'
                   OR barcode = @exact
                   OR barcode LIKE @prefix ESCAPE '\'
                ORDER BY CASE
                    WHEN barcode = @exact THEN 0
                    WHEN barcode LIKE @prefix ESCAPE '\' THEN 1
                    ELSE 2
                END, createdAt DESC
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
                SELECT COUNT(1) FROM Product
                WHERE name LIKE @like ESCAPE '\'
                   OR barcode = @exact
                   OR barcode LIKE @prefix ESCAPE '\'",
                new { like, prefix, exact });

            var filteredItems = conn.Query<Product>(@"
                SELECT * FROM Product
                WHERE name LIKE @like ESCAPE '\'
                   OR barcode = @exact
                   OR barcode LIKE @prefix ESCAPE '\'
                ORDER BY CASE
                    WHEN barcode = @exact THEN 0
                    WHEN barcode LIKE @prefix ESCAPE '\' THEN 1
                    ELSE 2
                END, createdAt DESC
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
