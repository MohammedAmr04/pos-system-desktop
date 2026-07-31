using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Dapper;
using Microsoft.Data.Sqlite;
using PosCs.Helpers;
using PosCs.Models;
using PosCs.Repositories;

namespace PosCs.Controllers
{
    [RoutePrefix("api/products")]
    public class ProductsController : ApiController
    {
        private readonly ProductRepository _repo = new ProductRepository();
        private readonly ProductUnitRepository _unitRepo = new ProductUnitRepository();

        [Route("")]
        [HttpGet]
        public HttpResponseMessage GetAll()
        {
            try
            {
                using (var conn = DbConnectionFactory.CreateConnection())
                {
                    var products = _repo.GetAll(conn).ToList();
                    AttachUnits(conn, products);
                    return Request.CreateResponse(HttpStatusCode.OK, products);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to fetch products: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to fetch products");
            }
        }

        [Route("{id}")]
        [HttpGet]
        public HttpResponseMessage GetById(string id)
        {
            try
            {
                using (var conn = DbConnectionFactory.CreateConnection())
                {
                    var product = _repo.GetById(conn, id);
                    if (product == null)
                        return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Product not found");

                    AttachUnits(conn, product);
                    return Request.CreateResponse(HttpStatusCode.OK, product);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to fetch product {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to fetch product");
            }
        }

        [Route("")]
        [HttpPost]
        public HttpResponseMessage Create([FromBody] CreateProductDto dto)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Product name is required");

                var retailPrice = dto.RetailPrice > 0 ? dto.RetailPrice : dto.SalePrice;
                if (retailPrice <= 0)
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Retail price is required");
                if (retailPrice < 0)
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Retail price cannot be negative");
                if (dto.WholesalePrice.HasValue && dto.WholesalePrice.Value < 0)
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Wholesale price cannot be negative");

                var unitName = string.IsNullOrWhiteSpace(dto.UnitName) ? "Piece" : dto.UnitName.Trim();

                using (var conn = DbConnectionFactory.CreateConnection())
                {
                    var barcode = dto.Barcode?.Trim();
                    if (string.IsNullOrWhiteSpace(barcode))
                    {
                        barcode = _unitRepo.GenerateUniqueBarcode(conn);
                    }
                    else if (_unitRepo.BarcodeExists(conn, barcode))
                    {
                        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Barcode already in use");
                    }

                    Product product;
                    using (var tx = conn.BeginTransaction())
                    {
                        try
                        {
                            product = _repo.Create(conn, new Product
                            {
                                Name = dto.Name,
                                BuyPrice = dto.BuyPrice,
                                StockQuantity = dto.StockQuantity,
                                Notes = dto.Notes,
                                AllowDiscount = dto.AllowDiscount,
                                LowStockThreshold = dto.LowStockThreshold
                            });

                            var baseUnit = _unitRepo.Create(conn, new ProductUnit
                            {
                                ProductId = product.Id,
                                UnitName = unitName,
                                QuantityFactor = 1,
                                RetailPrice = retailPrice,
                                WholesalePrice = dto.WholesalePrice,
                                IsBaseUnit = true
                            });

                            _unitRepo.AddBarcode(conn, baseUnit.Id, barcode);
                            tx.Commit();
                        }
                        catch
                        {
                            tx.Rollback();
                            throw;
                        }
                    }

                    AttachUnits(conn, product);
                    Console.WriteLine($"[API] Created product: {product.Id} ({product.Name}) with barcode {product.Barcode}");
                    return Request.CreateResponse(HttpStatusCode.OK, product);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to create product: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to create product");
            }
        }

        [Route("{id}")]
        [HttpPut]
        public HttpResponseMessage Update(string id, [FromBody] UpdateProductDto dto)
        {
            try
            {
                if (dto == null)
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid product data");

                using (var conn = DbConnectionFactory.CreateConnection())
                {
                    var existing = _repo.GetById(conn, id);
                    if (existing == null)
                        return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Product not found");

                    existing.Name = dto.Name ?? existing.Name;
                    existing.BuyPrice = dto.BuyPrice;
                    existing.StockQuantity = dto.StockQuantity;
                    existing.Notes = dto.Notes ?? existing.Notes;
                    if (dto.AllowDiscount.HasValue)
                        existing.AllowDiscount = dto.AllowDiscount.Value;
                    if (dto.LowStockThreshold.HasValue)
                        existing.LowStockThreshold = dto.LowStockThreshold.Value;

                    var newBarcode = dto.Barcode?.Trim();
                    using (var tx = conn.BeginTransaction())
                    {
                        try
                        {
                            var baseUnit = _unitRepo.GetBaseUnit(conn, id);

                            if (dto.RetailPrice.HasValue && dto.RetailPrice.Value < 0)
                                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Retail price cannot be negative");
                            if (dto.WholesalePrice.HasValue && dto.WholesalePrice.Value < 0)
                                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Wholesale price cannot be negative");

                            if (baseUnit != null)
                            {
                                var unitName = baseUnit.UnitName;
                                if (!string.IsNullOrWhiteSpace(dto.UnitName))
                                    unitName = dto.UnitName.Trim();

                                baseUnit.UnitName = unitName;
                                if (dto.RetailPrice.HasValue)
                                    baseUnit.RetailPrice = dto.RetailPrice.Value;
                                if (dto.WholesalePrice.HasValue)
                                    baseUnit.WholesalePrice = dto.WholesalePrice.Value;

                                _unitRepo.Update(conn, baseUnit);
                            }

                            if (!string.IsNullOrWhiteSpace(newBarcode))
                            {
                                var currentDefault = baseUnit != null
                                    ? _unitRepo.GetBarcodesByUnit(conn, baseUnit.Id).FirstOrDefault(b => b.IsDefault)
                                    : null;
                                if (currentDefault == null || currentDefault.Barcode != newBarcode)
                                {
                                    if (_unitRepo.BarcodeExists(conn, newBarcode))
                                        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Barcode already in use");

                                    if (baseUnit != null && currentDefault != null)
                                        conn.Execute("UPDATE ProductBarcode SET barcode = @barcode WHERE id = @id",
                                            new { barcode = newBarcode, id = currentDefault.Id }, transaction: tx);
                                    else if (baseUnit != null)
                                        _unitRepo.AddBarcode(conn, baseUnit.Id, newBarcode, isDefault: true);
                                }
                            }

                            _repo.Update(conn, existing);
                            tx.Commit();
                        }
                        catch
                        {
                            tx.Rollback();
                            throw;
                        }
                    }

                    AttachUnits(conn, existing);
                    Console.WriteLine($"[API] Updated product: {existing.Id}");
                    return Request.CreateResponse(HttpStatusCode.OK, existing);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to update product {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to update product");
            }
        }

        [Route("{id}")]
        [HttpDelete]
        public HttpResponseMessage Delete(string id)
        {
            try
            {
                using (var conn = DbConnectionFactory.CreateConnection())
                {
                    if (!_repo.Delete(conn, id))
                        return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Product not found");

                    Console.WriteLine($"[API] Deleted product: {id}");
                    return Request.CreateResponse(HttpStatusCode.OK, new { success = true });
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to delete product {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to delete product");
            }
        }

        [Route("{id}/units")]
        [HttpPost]
        public HttpResponseMessage AddUnit(string id, [FromBody] AddUnitDto dto)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.UnitName))
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Unit name is required");
                if (dto.QuantityFactor <= 1)
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Quantity factor must be greater than 1");
                if (dto.RetailPrice <= 0)
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Retail price is required");
                if (dto.RetailPrice < 0)
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Retail price cannot be negative");
                if (dto.WholesalePrice.HasValue && dto.WholesalePrice.Value < 0)
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Wholesale price cannot be negative");

                using (var conn = DbConnectionFactory.CreateConnection())
                {
                    var product = _repo.GetById(conn, id);
                    if (product == null)
                        return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Product not found");

                    var unit = _unitRepo.Create(conn, new ProductUnit
                    {
                        ProductId = id,
                        UnitName = dto.UnitName.Trim(),
                        QuantityFactor = dto.QuantityFactor,
                        RetailPrice = dto.RetailPrice,
                        WholesalePrice = dto.WholesalePrice,
                        IsBaseUnit = false
                    });

                    unit.Barcodes = _unitRepo.GetBarcodesByUnit(conn, unit.Id).ToList();
                    Console.WriteLine($"[API] Added unit '{unit.UnitName}' to product {id}");
                    return Request.CreateResponse(HttpStatusCode.OK, unit);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to add unit to product {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to add unit");
            }
        }

        [Route("{id}/units/{unitId}")]
        [HttpPut]
        public HttpResponseMessage UpdateUnit(string id, string unitId, [FromBody] UpdateUnitDto dto)
        {
            try
            {
                if (dto == null)
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid unit data");

                using (var conn = DbConnectionFactory.CreateConnection())
                {
                    var unit = _unitRepo.GetById(conn, unitId);
                    if (unit == null || unit.ProductId != id)
                        return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Unit not found");

                    if (unit.IsBaseUnit && dto.QuantityFactor.HasValue && dto.QuantityFactor.Value != 1)
                        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Base unit quantity factor must always be 1");
                    if (!unit.IsBaseUnit && dto.QuantityFactor.HasValue && dto.QuantityFactor.Value <= 1)
                        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Quantity factor must be greater than 1");
                    if (dto.RetailPrice.HasValue && dto.RetailPrice.Value <= 0)
                        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Retail price is required");
                    if (dto.RetailPrice.HasValue && dto.RetailPrice.Value < 0)
                        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Retail price cannot be negative");
                    if (dto.WholesalePrice.HasValue && dto.WholesalePrice.Value < 0)
                        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Wholesale price cannot be negative");

                    if (!string.IsNullOrWhiteSpace(dto.UnitName))
                        unit.UnitName = dto.UnitName.Trim();
                    if (dto.QuantityFactor.HasValue)
                        unit.QuantityFactor = dto.QuantityFactor.Value;
                    if (dto.RetailPrice.HasValue)
                        unit.RetailPrice = dto.RetailPrice.Value;
                    if (dto.WholesalePrice.HasValue)
                        unit.WholesalePrice = dto.WholesalePrice.Value;

                    _unitRepo.Update(conn, unit);
                    Console.WriteLine($"[API] Updated unit {unitId} of product {id}");
                    return Request.CreateResponse(HttpStatusCode.OK, unit);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to update unit {unitId} of product {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to update unit");
            }
        }

        [Route("{id}/units/{unitId}")]
        [HttpDelete]
        public HttpResponseMessage DeleteUnit(string id, string unitId)
        {
            try
            {
                using (var conn = DbConnectionFactory.CreateConnection())
                {
                    var unit = _unitRepo.GetById(conn, unitId);
                    if (unit == null || unit.ProductId != id)
                        return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Unit not found");
                    if (unit.IsBaseUnit)
                        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "The base unit cannot be deleted.");

                    if (!_unitRepo.Delete(conn, unitId))
                        return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to delete unit");

                    Console.WriteLine($"[API] Deleted unit {unitId} from product {id}");
                    return Request.CreateResponse(HttpStatusCode.OK, new { success = true });
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to delete unit {unitId} from product {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to delete unit");
            }
        }

        [Route("{id}/units/{unitId}/barcodes")]
        [HttpPost]
        public HttpResponseMessage AddBarcode(string id, string unitId, [FromBody] AddBarcodeDto dto)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.Barcode))
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Barcode is required");

                using (var conn = DbConnectionFactory.CreateConnection())
                {
                    var unit = _unitRepo.GetById(conn, unitId);
                    if (unit == null || unit.ProductId != id)
                        return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Unit not found");

                    var barcode = dto.Barcode.Trim();
                    if (_unitRepo.BarcodeExists(conn, barcode))
                        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "This barcode is already assigned to another product.");

                    var row = _unitRepo.AddBarcode(conn, unitId, barcode);
                    Console.WriteLine($"[API] Added barcode {barcode} to unit {unitId} of product {id}");
                    return Request.CreateResponse(HttpStatusCode.OK, row);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to add barcode to unit {unitId} of product {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to add barcode");
            }
        }

        [Route("{id}/units/{unitId}/barcodes/{barcodeId}")]
        [HttpDelete]
        public HttpResponseMessage DeleteBarcode(string id, string unitId, string barcodeId)
        {
            try
            {
                using (var conn = DbConnectionFactory.CreateConnection())
                {
                    var row = _unitRepo.GetBarcodeById(conn, barcodeId);
                    if (row == null || row.ProductUnitId != unitId)
                        return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Barcode not found");

                    var unit = _unitRepo.GetById(conn, unitId);
                    if (unit == null || unit.ProductId != id)
                        return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Barcode not found");

                    if (row.IsDefault)
                        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "The default barcode cannot be deleted.");

                    if (!_unitRepo.DeleteBarcode(conn, barcodeId))
                        return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to delete barcode");

                    Console.WriteLine($"[API] Deleted barcode {barcodeId} from unit {unitId}");
                    return Request.CreateResponse(HttpStatusCode.OK, new { success = true });
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to delete barcode {barcodeId} from unit {unitId}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to delete barcode");
            }
        }

        [Route("{id}/units/{unitId}/barcodes/{barcodeId}/default")]
        [HttpPut]
        public HttpResponseMessage SetDefaultBarcode(string id, string unitId, string barcodeId)
        {
            try
            {
                using (var conn = DbConnectionFactory.CreateConnection())
                {
                    var row = _unitRepo.GetBarcodeById(conn, barcodeId);
                    if (row == null || row.ProductUnitId != unitId)
                        return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Barcode not found");

                    var unit = _unitRepo.GetById(conn, unitId);
                    if (unit == null || unit.ProductId != id)
                        return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Barcode not found");

                    _unitRepo.SetDefaultBarcode(conn, unitId, barcodeId);
                    Console.WriteLine($"[API] Set barcode {barcodeId} as default for unit {unitId}");
                    return Request.CreateResponse(HttpStatusCode.OK, new { success = true });
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to set default barcode {barcodeId} for unit {unitId}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to set default barcode");
            }
        }

        private void AttachUnits(SqliteConnection conn, IEnumerable<Product> products)
        {
            foreach (var product in products)
            {
                var units = _unitRepo.GetByProduct(conn, product.Id).ToList();
                foreach (var unit in units)
                    unit.Barcodes = _unitRepo.GetBarcodesByUnit(conn, unit.Id).ToList();

                product.Units = units;
                product.Barcodes = units.SelectMany(u => u.Barcodes).ToList();
                product.Barcode = product.Barcodes.FirstOrDefault(b => b.IsDefault)?.Barcode
                    ?? product.Barcodes.FirstOrDefault()?.Barcode;
                var baseUnit = units.FirstOrDefault(u => u.IsBaseUnit);
                product.SalePrice = baseUnit?.RetailPrice ?? 0;
            }
        }

        private void AttachUnits(SqliteConnection conn, Product product)
        {
            AttachUnits(conn, new[] { product });
        }
    }

    public class CreateProductDto
    {
        public string Name { get; set; }
        public string Barcode { get; set; }
        public double BuyPrice { get; set; }
        public double SalePrice { get; set; }
        public double RetailPrice { get; set; }
        public double? WholesalePrice { get; set; }
        public string UnitName { get; set; }
        public double StockQuantity { get; set; }
        public string Notes { get; set; }
        public bool AllowDiscount { get; set; } = true;
        public int LowStockThreshold { get; set; }
    }

    public class UpdateProductDto
    {
        public string Name { get; set; }
        public string Barcode { get; set; }
        public double BuyPrice { get; set; }
        public double StockQuantity { get; set; }
        public string Notes { get; set; }
        public bool? AllowDiscount { get; set; }
        public int? LowStockThreshold { get; set; }
        public double? RetailPrice { get; set; }
        public double? WholesalePrice { get; set; }
        public string UnitName { get; set; }
    }

    public class AddUnitDto
    {
        public string UnitName { get; set; }
        public double QuantityFactor { get; set; }
        public double RetailPrice { get; set; }
        public double? WholesalePrice { get; set; }
    }

    public class UpdateUnitDto
    {
        public string UnitName { get; set; }
        public double? QuantityFactor { get; set; }
        public double? RetailPrice { get; set; }
        public double? WholesalePrice { get; set; }
    }

    public class AddBarcodeDto
    {
        public string Barcode { get; set; }
    }
}
