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
    public class ProductRepository : IProductRepository
    {
        public Product GetById(string id)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return conn.QueryFirstOrDefault<Product>("SELECT * FROM Product WHERE id = @id", new { id });
        }

        public List<Product> GetAll()
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return conn.Query<Product>("SELECT * FROM Product ORDER BY createdAt DESC").ToList();
        }

        public List<Product> GetForPOS()
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return conn.Query<Product>(
                    "SELECT * FROM Product WHERE isHiddenFromPOS = 0 ORDER BY createdAt DESC").ToList();
        }

        public List<Product> Search(string query, int limit)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return SearchCore(conn, null, query, limit).ToList();
        }

        public PagedResult<Product> GetPaged(int page, int pageSize, string query)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    var total = conn.ExecuteScalar<int>("SELECT COUNT(1) FROM Product");
                    var items = conn.Query<Product>(
                        "SELECT * FROM Product ORDER BY createdAt DESC LIMIT @pageSize OFFSET @offset",
                        new { pageSize, offset = (page - 1) * pageSize }).ToList();
                    return new PagedResult<Product> { Items = items, Total = total };
                }

                var exact = query;
                var like = $"%{EscapeLike(query)}%";
                var prefix = $"{EscapeLike(query)}%";

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
                    new { like, prefix, exact, pageSize, offset = (page - 1) * pageSize }).ToList();

                return new PagedResult<Product> { Items = filteredItems, Total = totalFiltered };
            }
        }

        public int Count()
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return conn.ExecuteScalar<int>("SELECT COUNT(1) FROM Product");
        }

        public Product CreateWithBaseUnit(Product product, ProductUnit baseUnit, string barcode)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            using (var tx = conn.BeginTransaction())
            {
                try
                {
                    InsertProduct(conn, tx, product);
                    baseUnit.ProductId = product.Id;
                    var unitRow = UnitSql.InsertUnit(conn, tx, baseUnit);
                    UnitSql.InsertBarcode(conn, tx, unitRow.Id, barcode, isDefault: false);
                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
            return product;
        }

        public void UpdateWithBaseUnit(Product product, ProductUnit baseUnit, string newDefaultBarcode)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            using (var tx = conn.BeginTransaction())
            {
                try
                {
                    if (baseUnit != null)
                        UnitSql.UpdateUnit(conn, tx, baseUnit);

                    if (!string.IsNullOrWhiteSpace(newDefaultBarcode))
                    {
                        var currentDefault = baseUnit != null
                            ? GetBarcodesByUnit(conn, tx, baseUnit.Id).FirstOrDefault(b => b.IsDefault)
                            : null;
                        if (currentDefault == null || currentDefault.Barcode != newDefaultBarcode)
                        {
                            if (UnitSql.BarcodeExists(conn, tx, newDefaultBarcode))
                                throw new DomainValidationException("Barcode already in use");

                            if (baseUnit != null && currentDefault != null)
                                conn.Execute("UPDATE ProductBarcode SET barcode = @barcode WHERE id = @id",
                                    new { barcode = newDefaultBarcode, id = currentDefault.Id }, transaction: tx);
                            else if (baseUnit != null)
                                UnitSql.InsertBarcode(conn, tx, baseUnit.Id, newDefaultBarcode, isDefault: true);
                        }
                    }

                    UpdateProduct(conn, tx, product);
                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        public bool Delete(string id)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return conn.Execute("DELETE FROM Product WHERE id = @id", new { id }) > 0;
        }

        private static IEnumerable<Product> SearchCore(SqliteConnection conn, SqliteTransaction tx, string q, int limit)
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
                WHERE (p.name LIKE @like ESCAPE '\'
                   OR pb.barcode = @exact
                   OR pb.barcode LIKE @prefix ESCAPE '\')
                  AND p.isHiddenFromPOS = 0
                GROUP BY p.id
                ORDER BY _rank, p.createdAt DESC
                LIMIT @limit",
                new { like, prefix, exact, limit }, transaction: tx);
        }

        private static void InsertProduct(SqliteConnection conn, SqliteTransaction tx, Product product)
        {
            var now = DateTime.Now;
            product.Id = Guid.NewGuid().ToString("N");
            product.CreatedAt = now;
            product.UpdatedAt = now;

            Console.WriteLine($"[DB] Creating product: Id={product.Id}, Name='{product.Name}', " +
                $"BuyPrice={product.BuyPrice}, Stock={product.StockQuantity}, " +
                $"Notes='{product.Notes}', CreatedAt={product.CreatedAt:O}, UpdatedAt={product.UpdatedAt:O}");

            conn.Execute(@"
                INSERT INTO Product (id, name, buyPrice, stockQuantity, notes, allowDiscount, lowStockThreshold, isHiddenFromPOS, categoryId, brandId, createdAt, updatedAt)
                VALUES (@id, @name, @buyPrice, @stockQuantity, @notes, @allowDiscount, @lowStockThreshold, @isHiddenFromPOS, @categoryId, @brandId, @createdAt, @updatedAt)",
                new
                {
                    id = product.Id,
                    name = product.Name,
                    buyPrice = product.BuyPrice,
                    stockQuantity = product.StockQuantity,
                    notes = product.Notes,
                    allowDiscount = product.AllowDiscount ? 1 : 0,
                    lowStockThreshold = product.LowStockThreshold,
                    isHiddenFromPOS = product.IsHiddenFromPOS ? 1 : 0,
                    categoryId = product.CategoryId,
                    brandId = product.BrandId,
                    createdAt = product.CreatedAt,
                    updatedAt = product.UpdatedAt
                }, transaction: tx);

            var saved = conn.QueryFirstOrDefault<Product>("SELECT * FROM Product WHERE id = @id",
                new { id = product.Id }, transaction: tx);
            if (saved != null)
                Console.WriteLine($"[DB] Product created successfully: Id={saved.Id}, Name='{saved.Name}'");
            else
                Console.Error.WriteLine("[DB] Product insert completed but read-back returned null!");
        }

        private static void UpdateProduct(SqliteConnection conn, SqliteTransaction tx, Product product)
        {
            product.UpdatedAt = DateTime.Now;
            conn.Execute(@"
                UPDATE Product SET name=@name, buyPrice=@buyPrice,
                    stockQuantity=@stockQuantity, notes=@notes,
                    allowDiscount=@allowDiscount, lowStockThreshold=@lowStockThreshold,
                    isHiddenFromPOS=@isHiddenFromPOS,
                    categoryId=@categoryId, brandId=@brandId,
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
                    isHiddenFromPOS = product.IsHiddenFromPOS ? 1 : 0,
                    categoryId = product.CategoryId,
                    brandId = product.BrandId,
                    updatedAt = product.UpdatedAt
                }, transaction: tx);
        }

        private static List<ProductBarcode> GetBarcodesByUnit(SqliteConnection conn, SqliteTransaction tx, string unitId)
        {
            return conn.Query<ProductBarcode>(
                "SELECT * FROM ProductBarcode WHERE productUnitId = @unitId ORDER BY isDefault DESC, createdAt ASC",
                new { unitId }, transaction: tx).ToList();
        }

        private static string EscapeLike(string input)
        {
            return input.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
        }
    }
}
