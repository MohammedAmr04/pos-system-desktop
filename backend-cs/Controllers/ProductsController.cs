using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using PosCs.Api;
using PosCs.Application.Models;
using PosCs.Application.Services;
using PosCs.Attributes;
using PosCs.Domain.Exceptions;

namespace PosCs.Controllers
{
    [RoutePrefix("api/products")]
    public class ProductsController : ApiController
    {
        private readonly ProductService _service = CompositionRoot.ProductService;

        [Route("")]
        [HttpGet]
        [RequirePermission("products.view")]
        public HttpResponseMessage GetAll()
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK, _service.GetAll());
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to fetch products: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to fetch products");
            }
        }

        [Route("{id}")]
        [HttpGet]
        [RequirePermission("products.view")]
        public HttpResponseMessage GetById(string id)
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK, _service.GetById(id));
            }
            catch (NotFoundException)
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Product not found");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to fetch product {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to fetch product");
            }
        }

        [Route("search")]
        [HttpGet]
        [RequirePermission("products.view")]
        public HttpResponseMessage Search(string q, int limit = 20)
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK, _service.Search(q, limit));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to search products: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to search products");
            }
        }

        [Route("paged")]
        [HttpGet]
        [RequirePermission("products.view")]
        public HttpResponseMessage GetPaged(int page = 1, int pageSize = 20, string q = null)
        {
            try
            {
                var result = _service.GetPaged(page, pageSize, q);
                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    items = result.Items,
                    total = result.Total,
                    page,
                    pageSize
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to fetch paged products: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to fetch paged products");
            }
        }

        [Route("count")]
        [HttpGet]
        [RequirePermission("products.view")]
        public HttpResponseMessage GetCount()
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK, _service.Count());
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to count products: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to count products");
            }
        }

        [Route("")]
        [HttpPost]
        [RequirePermission("products.create")]
        public HttpResponseMessage Create([FromBody] CreateProductRequest dto)
        {
            try
            {
                var product = _service.Create(dto);
                Console.WriteLine($"[API] Created product: {product.Id} ({product.Name}) with barcode {product.Barcode}");
                return Request.CreateResponse(HttpStatusCode.OK, product);
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to create product: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to create product");
            }
        }

        [Route("{id}")]
        [HttpPut]
        [RequirePermission("products.update")]
        public HttpResponseMessage Update(string id, [FromBody] UpdateProductRequest dto)
        {
            try
            {
                var product = _service.Update(id, dto);
                Console.WriteLine($"[API] Updated product: {product.Id}");
                return Request.CreateResponse(HttpStatusCode.OK, product);
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to update product {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to update product");
            }
        }

        [Route("{id}")]
        [HttpDelete]
        [RequirePermission("products.delete")]
        public HttpResponseMessage Delete(string id)
        {
            try
            {
                _service.Delete(id);
                Console.WriteLine($"[API] Deleted product: {id}");
                return Request.CreateResponse(HttpStatusCode.OK, new { success = true });
            }
            catch (NotFoundException)
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Product not found");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to delete product {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to delete product");
            }
        }

        [Route("{id}/units")]
        [HttpPost]
        [RequirePermission("products.update", "multiple_units")]
        public HttpResponseMessage AddUnit(string id, [FromBody] AddUnitRequest dto)
        {
            try
            {
                var unit = _service.AddUnit(id, dto);
                Console.WriteLine($"[API] Added unit '{unit.UnitName}' to product {id}");
                return Request.CreateResponse(HttpStatusCode.OK, unit);
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to add unit to product {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to add unit");
            }
        }

        [Route("{id}/units/{unitId}")]
        [HttpPut]
        [RequirePermission("products.update", "multiple_units")]
        public HttpResponseMessage UpdateUnit(string id, string unitId, [FromBody] UpdateUnitRequest dto)
        {
            try
            {
                var unit = _service.UpdateUnit(id, unitId, dto);
                Console.WriteLine($"[API] Updated unit {unitId} of product {id}");
                return Request.CreateResponse(HttpStatusCode.OK, unit);
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to update unit {unitId} of product {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to update unit");
            }
        }

        [Route("{id}/units/{unitId}")]
        [HttpDelete]
        [RequirePermission("products.update", "multiple_units")]
        public HttpResponseMessage DeleteUnit(string id, string unitId)
        {
            try
            {
                _service.DeleteUnit(id, unitId);
                Console.WriteLine($"[API] Deleted unit {unitId} from product {id}");
                return Request.CreateResponse(HttpStatusCode.OK, new { success = true });
            }
            catch (NotFoundException)
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Unit not found");
            }
            catch (DomainValidationException v)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, v.Message);
            }
            catch (InvalidOperationException)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to delete unit");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to delete unit {unitId} from product {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to delete unit");
            }
        }

        [Route("{id}/units/{unitId}/barcodes")]
        [HttpPost]
        [RequirePermission("products.update", "multiple_barcodes")]
        public HttpResponseMessage AddBarcode(string id, string unitId, [FromBody] AddBarcodeRequest dto)
        {
            try
            {
                var row = _service.AddBarcode(id, unitId, dto);
                Console.WriteLine($"[API] Added barcode {row.Barcode} to unit {unitId} of product {id}");
                return Request.CreateResponse(HttpStatusCode.OK, row);
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to add barcode to unit {unitId} of product {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to add barcode");
            }
        }

        [Route("{id}/units/{unitId}/barcodes/{barcodeId}")]
        [HttpDelete]
        [RequirePermission("products.update", "multiple_barcodes")]
        public HttpResponseMessage DeleteBarcode(string id, string unitId, string barcodeId)
        {
            try
            {
                _service.DeleteBarcode(id, unitId, barcodeId);
                Console.WriteLine($"[API] Deleted barcode {barcodeId} from unit {unitId}");
                return Request.CreateResponse(HttpStatusCode.OK, new { success = true });
            }
            catch (NotFoundException)
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Barcode not found");
            }
            catch (DomainValidationException v)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, v.Message);
            }
            catch (InvalidOperationException)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to delete barcode");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to delete barcode {barcodeId} from unit {unitId}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to delete barcode");
            }
        }

        [Route("{id}/units/{unitId}/barcodes/{barcodeId}/default")]
        [HttpPut]
        [RequirePermission("products.update", "multiple_barcodes")]
        public HttpResponseMessage SetDefaultBarcode(string id, string unitId, string barcodeId)
        {
            try
            {
                _service.SetDefaultBarcode(id, unitId, barcodeId);
                Console.WriteLine($"[API] Set barcode {barcodeId} as default for unit {unitId}");
                return Request.CreateResponse(HttpStatusCode.OK, new { success = true });
            }
            catch (NotFoundException)
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Barcode not found");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to set default barcode {barcodeId} for unit {unitId}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to set default barcode");
            }
        }
    }
}
