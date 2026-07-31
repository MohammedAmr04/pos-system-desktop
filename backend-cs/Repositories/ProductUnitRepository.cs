using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using Microsoft.Data.Sqlite;
using PosCs.Models;

namespace PosCs.Repositories
{
    public class ProductUnitRepository
    {
        public IEnumerable<ProductUnit> GetByProduct(SqliteConnection conn, string productId)
        {
            return conn.Query<ProductUnit>(
                "SELECT * FROM ProductUnit WHERE productId = @productId ORDER BY isBaseUnit DESC, createdAt ASC",
                new { productId });
        }

        public ProductUnit GetById(SqliteConnection conn, string id)
        {
            return conn.QueryFirstOrDefault<ProductUnit>("SELECT * FROM ProductUnit WHERE id = @id", new { id });
        }

        public ProductUnit GetBaseUnit(SqliteConnection conn, string productId)
        {
            return conn.QueryFirstOrDefault<ProductUnit>(
                "SELECT * FROM ProductUnit WHERE productId = @productId AND isBaseUnit = 1", new { productId });
        }

        public IEnumerable<ProductBarcode> GetBarcodesByUnit(SqliteConnection conn, string unitId)
        {
            return conn.Query<ProductBarcode>(
                "SELECT * FROM ProductBarcode WHERE productUnitId = @unitId ORDER BY isDefault DESC, createdAt ASC",
                new { unitId });
        }

        public ProductBarcode GetBarcodeById(SqliteConnection conn, string id)
        {
            return conn.QueryFirstOrDefault<ProductBarcode>("SELECT * FROM ProductBarcode WHERE id = @id", new { id });
        }

        public ProductBarcode GetBarcodeByCode(SqliteConnection conn, string barcode)
        {
            return conn.QueryFirstOrDefault<ProductBarcode>(
                "SELECT * FROM ProductBarcode WHERE barcode = @barcode", new { barcode });
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

        public ProductUnit Create(SqliteConnection conn, ProductUnit unit)
        {
            var row = new ProductUnit
            {
                Id = Guid.NewGuid().ToString("N"),
                ProductId = unit.ProductId,
                UnitName = unit.UnitName,
                QuantityFactor = unit.QuantityFactor,
                RetailPrice = unit.RetailPrice,
                WholesalePrice = unit.WholesalePrice,
                IsBaseUnit = unit.IsBaseUnit,
                CreatedAt = DateTime.Now
            };

            conn.Execute(@"
                INSERT INTO ProductUnit (id, productId, unitName, quantityFactor, retailPrice, wholesalePrice, isBaseUnit, createdAt)
                VALUES (@id, @productId, @unitName, @quantityFactor, @retailPrice, @wholesalePrice, @isBaseUnit, @createdAt)",
                new
                {
                    id = row.Id,
                    productId = row.ProductId,
                    unitName = row.UnitName,
                    quantityFactor = row.QuantityFactor,
                    retailPrice = row.RetailPrice,
                    wholesalePrice = row.WholesalePrice,
                    isBaseUnit = row.IsBaseUnit ? 1 : 0,
                    createdAt = row.CreatedAt
                });

            return row;
        }

        public void Update(SqliteConnection conn, ProductUnit unit)
        {
            conn.Execute(@"
                UPDATE ProductUnit SET unitName = @unitName, quantityFactor = @quantityFactor,
                    retailPrice = @retailPrice, wholesalePrice = @wholesalePrice
                WHERE id = @id",
                new
                {
                    unitName = unit.UnitName,
                    quantityFactor = unit.QuantityFactor,
                    retailPrice = unit.RetailPrice,
                    wholesalePrice = unit.WholesalePrice,
                    id = unit.Id
                });
        }

        public bool Delete(SqliteConnection conn, string unitId)
        {
            var unit = GetById(conn, unitId);
            if (unit == null || unit.IsBaseUnit)
                return false;
            var rows = conn.Execute("DELETE FROM ProductUnit WHERE id = @id", new { id = unitId });
            return rows > 0;
        }

        public ProductBarcode AddBarcode(SqliteConnection conn, string unitId, string barcode, bool isDefault = false)
        {
            var count = conn.ExecuteScalar<int>(
                "SELECT COUNT(1) FROM ProductBarcode WHERE productUnitId = @unitId", new { unitId });

            var row = new ProductBarcode
            {
                Id = Guid.NewGuid().ToString("N"),
                ProductUnitId = unitId,
                Barcode = barcode,
                IsDefault = isDefault || count == 0,
                CreatedAt = DateTime.Now
            };

            conn.Execute(@"
                INSERT INTO ProductBarcode (id, productUnitId, barcode, isDefault, createdAt)
                VALUES (@id, @productUnitId, @barcode, @isDefault, @createdAt)",
                new
                {
                    id = row.Id,
                    productUnitId = row.ProductUnitId,
                    barcode = row.Barcode,
                    isDefault = row.IsDefault ? 1 : 0,
                    createdAt = row.CreatedAt
                });

            return row;
        }

        public bool DeleteBarcode(SqliteConnection conn, string id)
        {
            var row = GetBarcodeById(conn, id);
            if (row == null || row.IsDefault)
                return false;
            var rows = conn.Execute("DELETE FROM ProductBarcode WHERE id = @id", new { id });
            return rows > 0;
        }

        public bool SetDefaultBarcode(SqliteConnection conn, string unitId, string barcodeId)
        {
            var row = GetBarcodeById(conn, barcodeId);
            if (row == null || row.ProductUnitId != unitId)
                return false;

            using (var tx = conn.BeginTransaction())
            {
                try
                {
                    conn.Execute(
                        "UPDATE ProductBarcode SET isDefault = 0 WHERE productUnitId = @unitId",
                        new { unitId }, transaction: tx);
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
    }
}
