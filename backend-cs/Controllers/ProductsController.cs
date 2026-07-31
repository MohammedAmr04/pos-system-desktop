using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
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
        private readonly ProductBarcodeRepository _barcodeRepo = new ProductBarcodeRepository();

        [Route("")]
        [HttpGet]
        public HttpResponseMessage GetAll()
        {
            try
            {
                using (var conn = DbConnectionFactory.CreateConnection())
                {
                    var products = _repo.GetAll(conn).ToList();
                    AttachBarcodes(conn, products);
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

                    AttachBarcodes(conn, product);
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

                using (var conn = DbConnectionFactory.CreateConnection())
                {
                    var barcode = dto.Barcode?.Trim();
                    if (string.IsNullOrWhiteSpace(barcode))
                    {
                        barcode = _barcodeRepo.GenerateUniqueBarcode(conn);
                    }
                    else if (_barcodeRepo.BarcodeExists(conn, barcode))
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
                                SalePrice = dto.SalePrice,
                                StockQuantity = dto.StockQuantity,
                                Notes = dto.Notes,
                                AllowDiscount = dto.AllowDiscount,
                                LowStockThreshold = dto.LowStockThreshold
                            });

                            _barcodeRepo.Add(conn, product.Id, barcode);
                            tx.Commit();
                        }
                        catch
                        {
                            tx.Rollback();
                            throw;
                        }
                    }

                    AttachBarcodes(conn, product);
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
                    existing.SalePrice = dto.SalePrice;
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
                            if (!string.IsNullOrWhiteSpace(newBarcode))
                            {
                                var currentDefault = _barcodeRepo.GetDefault(conn, id);
                                if (currentDefault == null || currentDefault.Barcode != newBarcode)
                                {
                                    if (_barcodeRepo.BarcodeExists(conn, newBarcode))
                                        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Barcode already in use");

                                    if (currentDefault != null)
                                        _barcodeRepo.UpdateBarcode(conn, currentDefault.Id, newBarcode);
                                    else
                                        _barcodeRepo.Add(conn, id, newBarcode, isDefault: true);
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

                    AttachBarcodes(conn, existing);
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

        [Route("{id}/barcodes")]
        [HttpPost]
        public HttpResponseMessage AddBarcode(string id, [FromBody] AddBarcodeDto dto)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.Barcode))
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Barcode is required");

                using (var conn = DbConnectionFactory.CreateConnection())
                {
                    var product = _repo.GetById(conn, id);
                    if (product == null)
                        return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Product not found");

                    var barcode = dto.Barcode.Trim();
                    if (_barcodeRepo.BarcodeExists(conn, barcode))
                        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "This barcode is already assigned to another product.");

                    var row = _barcodeRepo.Add(conn, id, barcode);
                    Console.WriteLine($"[API] Added barcode {barcode} to product {id}");
                    return Request.CreateResponse(HttpStatusCode.OK, row);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to add barcode to product {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to add barcode");
            }
        }

        [Route("{id}/barcodes/{barcodeId}")]
        [HttpDelete]
        public HttpResponseMessage DeleteBarcode(string id, string barcodeId)
        {
            try
            {
                using (var conn = DbConnectionFactory.CreateConnection())
                {
                    var row = _barcodeRepo.GetById(conn, barcodeId);
                    if (row == null || row.ProductId != id)
                        return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Barcode not found");

                    if (row.IsDefault)
                        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "The default barcode cannot be deleted.");

                    if (!_barcodeRepo.Delete(conn, barcodeId))
                        return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to delete barcode");

                    Console.WriteLine($"[API] Deleted barcode {barcodeId} from product {id}");
                    return Request.CreateResponse(HttpStatusCode.OK, new { success = true });
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to delete barcode {barcodeId} from product {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to delete barcode");
            }
        }

        [Route("{id}/barcodes/{barcodeId}/default")]
        [HttpPut]
        public HttpResponseMessage SetDefaultBarcode(string id, string barcodeId)
        {
            try
            {
                using (var conn = DbConnectionFactory.CreateConnection())
                {
                    var row = _barcodeRepo.GetById(conn, barcodeId);
                    if (row == null || row.ProductId != id)
                        return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Barcode not found");

                    _barcodeRepo.SetDefault(conn, id, barcodeId);
                    Console.WriteLine($"[API] Set barcode {barcodeId} as default for product {id}");
                    return Request.CreateResponse(HttpStatusCode.OK, new { success = true });
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to set default barcode {barcodeId} for product {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to set default barcode");
            }
        }

        private void AttachBarcodes(SqliteConnection conn, IEnumerable<Product> products)
        {
            foreach (var product in products)
            {
                product.Barcodes = _barcodeRepo.GetByProduct(conn, product.Id).ToList();
                product.Barcode = product.Barcodes.FirstOrDefault(b => b.IsDefault)?.Barcode
                    ?? product.Barcodes.FirstOrDefault()?.Barcode;
            }
        }

        private void AttachBarcodes(SqliteConnection conn, Product product)
        {
            AttachBarcodes(conn, new[] { product });
        }
    }

    public class CreateProductDto
    {
        public string Name { get; set; }
        public string Barcode { get; set; }
        public double BuyPrice { get; set; }
        public double SalePrice { get; set; }
        public int StockQuantity { get; set; }
        public string Notes { get; set; }
        public bool AllowDiscount { get; set; } = true;
        public int LowStockThreshold { get; set; }
    }

    public class UpdateProductDto
    {
        public string Name { get; set; }
        public string Barcode { get; set; }
        public double BuyPrice { get; set; }
        public double SalePrice { get; set; }
        public int StockQuantity { get; set; }
        public string Notes { get; set; }
        public bool? AllowDiscount { get; set; }
        public int? LowStockThreshold { get; set; }
    }

    public class AddBarcodeDto
    {
        public string Barcode { get; set; }
    }
}
