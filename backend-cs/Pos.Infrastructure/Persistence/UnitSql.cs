using System;
using Dapper;
using Microsoft.Data.Sqlite;
using PosCs.Domain.Entities;

namespace PosCs.Infrastructure.Persistence
{
    /// <summary>Shared unit/barcode SQL so composite product operations can join the same transaction.</summary>
    internal static class UnitSql
    {
        public static ProductUnit InsertUnit(SqliteConnection conn, SqliteTransaction tx, ProductUnit unit)
        {
            var row = new ProductUnit
            {
                Id = Guid.NewGuid().ToString("N"),
                ProductId = unit.ProductId,
                UnitName = unit.UnitName,
                UnitId = unit.UnitId,
                QuantityFactor = unit.QuantityFactor,
                RetailPrice = unit.RetailPrice,
                WholesalePrice = unit.WholesalePrice,
                IsBaseUnit = unit.IsBaseUnit,
                CreatedAt = DateTime.Now
            };

            conn.Execute(@"
                INSERT INTO ProductUnit (id, productId, unitName, unitId, quantityFactor, retailPrice, wholesalePrice, isBaseUnit, createdAt)
                VALUES (@id, @productId, @unitName, @unitId, @quantityFactor, @retailPrice, @wholesalePrice, @isBaseUnit, @createdAt)",
                new
                {
                    id = row.Id,
                    productId = row.ProductId,
                    unitName = row.UnitName,
                    unitId = row.UnitId,
                    quantityFactor = row.QuantityFactor,
                    retailPrice = row.RetailPrice,
                    wholesalePrice = row.WholesalePrice,
                    isBaseUnit = row.IsBaseUnit ? 1 : 0,
                    createdAt = row.CreatedAt
                }, transaction: tx);

            return row;
        }

        public static void UpdateUnit(SqliteConnection conn, SqliteTransaction tx, ProductUnit unit)
        {
            conn.Execute(@"
                UPDATE ProductUnit SET unitName = @unitName, unitId = @unitId, quantityFactor = @quantityFactor,
                    retailPrice = @retailPrice, wholesalePrice = @wholesalePrice
                WHERE id = @id",
                new
                {
                    unitName = unit.UnitName,
                    unitId = unit.UnitId,
                    quantityFactor = unit.QuantityFactor,
                    retailPrice = unit.RetailPrice,
                    wholesalePrice = unit.WholesalePrice,
                    id = unit.Id
                }, transaction: tx);
        }

        public static bool BarcodeExists(SqliteConnection conn, SqliteTransaction tx, string barcode)
        {
            return conn.ExecuteScalar<int>(
                "SELECT COUNT(1) FROM ProductBarcode WHERE barcode = @barcode", new { barcode }, transaction: tx) > 0;
        }

        /// <summary>Inserts a barcode row; auto-defaults when the unit has no barcodes yet.</summary>
        public static ProductBarcode InsertBarcode(SqliteConnection conn, SqliteTransaction tx, string unitId, string barcode, bool isDefault)
        {
            var count = conn.ExecuteScalar<int>(
                "SELECT COUNT(1) FROM ProductBarcode WHERE productUnitId = @unitId", new { unitId }, transaction: tx);

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
                }, transaction: tx);

            return row;
        }
    }
}
