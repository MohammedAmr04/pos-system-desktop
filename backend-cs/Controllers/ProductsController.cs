using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using PosCs.Helpers;
using PosCs.Models;
using PosCs.Repositories;

namespace PosCs.Controllers
{
    [RoutePrefix("api/products")]
    public class ProductsController : ApiController
    {
        private readonly ProductRepository _repo = new ProductRepository();

        [Route("")]
        [HttpGet]
        public HttpResponseMessage GetAll()
        {
            try
            {
                using (var conn = DbConnectionFactory.CreateConnection())
                {
                    var products = _repo.GetAll(conn);
                    return Request.CreateResponse(HttpStatusCode.OK, products);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to fetch products: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to fetch products");
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
                    var barcode = dto.Barcode;
                    if (string.IsNullOrWhiteSpace(barcode))
                    {
                        barcode = _repo.GenerateUniqueBarcode(conn);
                    }
                    else if (_repo.BarcodeExists(conn, barcode))
                    {
                        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Barcode already in use");
                    }

                    var product = _repo.Create(conn, new Product
                    {
                        Name = dto.Name,
                        Barcode = barcode,
                        BuyPrice = dto.BuyPrice,
                        SalePrice = dto.SalePrice,
                        StockQuantity = dto.StockQuantity,
                        Notes = dto.Notes,
                        AllowDiscount = dto.AllowDiscount,
                        LowStockThreshold = dto.LowStockThreshold
                    });

                    Console.WriteLine($"[API] Created product: {product.Id} ({product.Name})");
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
                    existing.Barcode = dto.Barcode ?? existing.Barcode;
                    existing.BuyPrice = dto.BuyPrice;
                    existing.SalePrice = dto.SalePrice;
                    existing.StockQuantity = dto.StockQuantity;
                    existing.Notes = dto.Notes ?? existing.Notes;
                    if (dto.AllowDiscount.HasValue)
                        existing.AllowDiscount = dto.AllowDiscount.Value;
                    if (dto.LowStockThreshold.HasValue)
                        existing.LowStockThreshold = dto.LowStockThreshold.Value;

                    _repo.Update(conn, existing);

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
}
