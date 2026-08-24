using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using PosCs.Api;
using PosCs.Application.Models;
using PosCs.Application.Services;
using PosCs.Attributes;

namespace PosCs.Controllers
{
    [RoutePrefix("api/brands")]
    public class BrandsController : ApiController
    {
        private readonly BrandService _service = CompositionRoot.BrandService;

        [Route("")]
        [HttpGet]
        [RequirePermission("brands.view", "brands")]
        public HttpResponseMessage GetActive()
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK, _service.GetActive());
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to fetch brands: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to fetch brands");
            }
        }

        [Route("paged")]
        [HttpGet]
        [RequirePermission("brands.view", "brands")]
        public HttpResponseMessage GetPaged([FromUri] int page = 1, [FromUri] int pageSize = 20, [FromUri] string q = null)
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
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to fetch brands (paged): {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to fetch brands");
            }
        }

        [Route("{id}")]
        [HttpGet]
        [RequirePermission("brands.view", "brands")]
        public HttpResponseMessage GetById(string id)
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK, _service.GetById(id));
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to fetch brand {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to fetch brand");
            }
        }

        [Route("")]
        [HttpPost]
        [RequirePermission("brands.create", "brands")]
        public HttpResponseMessage Create([FromBody] CreateBrandRequest dto)
        {
            try
            {
                var brand = _service.Create(dto);
                Console.WriteLine($"[API] Created brand: {brand.Id} '{brand.Name}'");
                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    id = brand.Id,
                    name = brand.Name,
                    isActive = brand.IsActive,
                    createdAt = brand.CreatedAt,
                    updatedAt = brand.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to create brand: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to create brand");
            }
        }

        [Route("{id}")]
        [HttpPut]
        [RequirePermission("brands.update", "brands")]
        public HttpResponseMessage Update(string id, [FromBody] UpdateBrandRequest dto)
        {
            try
            {
                var brand = _service.Update(id, dto);
                Console.WriteLine($"[API] Updated brand: {brand.Id} '{brand.Name}'");
                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    id = brand.Id,
                    name = brand.Name,
                    isActive = brand.IsActive,
                    createdAt = brand.CreatedAt,
                    updatedAt = brand.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to update brand {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to update brand");
            }
        }

        [Route("{id}")]
        [HttpDelete]
        [RequirePermission("brands.delete", "brands")]
        public HttpResponseMessage Delete(string id)
        {
            try
            {
                _service.Delete(id);
                Console.WriteLine($"[API] Deleted brand: {id}");
                return Request.CreateResponse(HttpStatusCode.OK, new { success = true });
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to delete brand {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to delete brand");
            }
        }
    }
}
