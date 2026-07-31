using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using Microsoft.Data.Sqlite;
using PosCs.Models;

namespace PosCs.Repositories
{
    public class ProductBarcodeRepository
    {
        public IEnumerable<ProductBarcode> GetByProduct(SqliteConnection conn, string productId)
        {
            return conn.Query<ProductBarcode>(
                "SELECT * FROM ProductBarcode WHERE productId = @productId ORDER BY isDefault DESC, createdAt ASC",
                new { productId });
        }

        public ProductBarcode GetById(SqliteConnection conn, string id)
        {
            return conn.QueryFirstOrDefault<ProductBarcode>(
                "SELECT * FROM ProductBarcode WHERE id = @id", new { id });
        }

        public ProductBarcode GetByBarcode(SqliteConnection conn, string barcode)
        {
            return conn.QueryFirstOrDefault<ProductBarcode>(
                "SELECT * FROM ProductBarcode WHERE barcode = @barcode", new { barcode });
        }

        public ProductBarcode GetDefault(SqliteConnection conn, string productId)
        {
            return conn.QueryFirstOrDefault<ProductBarcode>(
                "SELECT * FROM ProductBarcode WHERE productId = @productId AND isDefault = 1",
                new { productId });
        }

        public bool BarcodeExists(SqliteConnection conn, string barcode)
        {
            return conn.ExecuteScalar<int>(
                "SELECT COUNT(1) FROM ProductBarcode WHERE barcode = @barcode", new { barcode }) > 0;
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

        public ProductBarcode Add(SqliteConnection conn, string productId, string barcode, bool isDefault = false)
        {
            var count = conn.ExecuteScalar<int>(
                "SELECT COUNT(1) FROM ProductBarcode WHERE productId = @productId", new { productId });

            var row = new ProductBarcode
            {
                Id = Guid.NewGuid().ToString("N"),
                ProductId = productId,
                Barcode = barcode,
                IsDefault = isDefault || count == 0,
                CreatedAt = DateTime.Now
            };

            conn.Execute(@"
                INSERT INTO ProductBarcode (id, productId, barcode, isDefault, createdAt)
                VALUES (@id, @productId, @barcode, @isDefault, @createdAt)",
                new
                {
                    id = row.Id,
                    productId = row.ProductId,
                    barcode = row.Barcode,
                    isDefault = row.IsDefault ? 1 : 0,
                    createdAt = row.CreatedAt
                });

            return row;
        }

        public bool Delete(SqliteConnection conn, string id)
        {
            var row = GetById(conn, id);
            if (row == null)
                return false;
            if (row.IsDefault)
                return false;

            var rows = conn.Execute("DELETE FROM ProductBarcode WHERE id = @id", new { id });
            return rows > 0;
        }

        public bool SetDefault(SqliteConnection conn, string productId, string barcodeId)
        {
            var row = GetById(conn, barcodeId);
            if (row == null || row.ProductId != productId)
                return false;

            using (var tx = conn.BeginTransaction())
            {
                try
                {
                    conn.Execute(
                        "UPDATE ProductBarcode SET isDefault = 0 WHERE productId = @productId",
                        new { productId }, transaction: tx);
                    conn.Execute(
                        "UPDATE ProductBarcode SET isDefault = 1 WHERE id = @id",
                        new { id = barcodeId }, transaction: tx);
                    tx.Commit();
                    return true;
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        public void UpdateBarcode(SqliteConnection conn, string id, string barcode)
        {
            conn.Execute("UPDATE ProductBarcode SET barcode = @barcode WHERE id = @id", new { id, barcode });
        }
    }
}
