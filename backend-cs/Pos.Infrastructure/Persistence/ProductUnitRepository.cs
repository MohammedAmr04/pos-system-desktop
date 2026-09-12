using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using PosCs.Application.Ports;
using PosCs.Domain.Entities;

namespace PosCs.Infrastructure.Persistence
{
    public class ProductUnitRepository : IProductUnitRepository
    {
        public List<ProductUnit> GetByProduct(string productId)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return conn.Query<ProductUnit>(
                    "SELECT * FROM ProductUnit WHERE productId = @productId ORDER BY isBaseUnit DESC, createdAt ASC",
                    new { productId }).ToList();
        }

        public ProductUnit GetById(string unitId)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return conn.QueryFirstOrDefault<ProductUnit>("SELECT * FROM ProductUnit WHERE id = @id", new { id = unitId });
        }

        public ProductUnit GetBaseUnit(string productId)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return conn.QueryFirstOrDefault<ProductUnit>(
                    "SELECT * FROM ProductUnit WHERE productId = @productId AND isBaseUnit = 1", new { productId });
        }

        public List<ProductBarcode> GetBarcodesByUnit(string unitId)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return conn.Query<ProductBarcode>(
                    "SELECT * FROM ProductBarcode WHERE productUnitId = @unitId ORDER BY isDefault DESC, createdAt ASC",
                    new { unitId }).ToList();
        }

        public ProductBarcode GetBarcodeById(string barcodeId)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return conn.QueryFirstOrDefault<ProductBarcode>("SELECT * FROM ProductBarcode WHERE id = @id", new { id = barcodeId });
        }

        public bool BarcodeExists(string barcode)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return UnitSql.BarcodeExists(conn, null, barcode);
        }

        public string GenerateUniqueBarcode()
        {
            var rng = new Random();
            for (int attempt = 0; attempt < 10; attempt++)
            {
                var barcode = rng.Next(0, int.MaxValue).ToString("D12");
                if (!BarcodeExists(barcode))
                    return barcode;
            }
            throw new Exception("Failed to generate unique barcode after multiple attempts");
        }

        public ProductUnit Create(ProductUnit unit)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return UnitSql.InsertUnit(conn, null, unit);
        }

        public void Update(ProductUnit unit)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                UnitSql.UpdateUnit(conn, null, unit);
        }

        public bool Delete(string unitId)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                var unit = GetById(unitId);
                if (unit == null || unit.IsBaseUnit)
                    return false;
                return conn.Execute("DELETE FROM ProductUnit WHERE id = @id", new { id = unitId }) > 0;
            }
        }

        public ProductBarcode AddBarcode(string unitId, string barcode, bool isDefault = false)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return UnitSql.InsertBarcode(conn, null, unitId, barcode, isDefault);
        }

        public bool DeleteBarcode(string barcodeId)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                var row = GetBarcodeById(barcodeId);
                if (row == null || row.IsDefault)
                    return false;
                return conn.Execute("DELETE FROM ProductBarcode WHERE id = @id", new { id = barcodeId }) > 0;
            }
        }

        public bool SetDefaultBarcode(string unitId, string barcodeId)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                var row = GetBarcodeById(barcodeId);
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

        public void SyncUnitName(string unitId, string newName)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                conn.Execute(
                    "UPDATE ProductUnit SET unitName = @name WHERE unitId = @unitId",
                    new { unitId, name = newName });
        }
    }
}
