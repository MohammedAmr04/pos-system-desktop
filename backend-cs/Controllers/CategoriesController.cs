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
    [RoutePrefix("api/categories")]
    public class CategoriesController : ApiController
    {
        private readonly CategoryService _service = CompositionRoot.CategoryService;

        [Route("")]
        [HttpGet]
        [RequirePermission("categories.view", "categories")]
        public HttpResponseMessage GetActive()
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK, _service.GetActive());
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to fetch categories: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to fetch categories");
            }
        }

        [Route("paged")]
        [HttpGet]
        [RequirePermission("categories.view", "categories")]
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
                Console.Error.WriteLine($"[API ERR] Failed to fetch categories (paged): {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to fetch categories");
            }
        }

        [Route("{id}")]
        [HttpGet]
        [RequirePermission("categories.view", "categories")]
        public HttpResponseMessage GetById(string id)
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK, _service.GetById(id));
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to fetch category {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to fetch category");
            }
        }

        [Route("")]
        [HttpPost]
        [RequirePermission("categories.create", "categories")]
        public HttpResponseMessage Create([FromBody] CreateCategoryRequest dto)
        {
            try
            {
                var category = _service.Create(dto);
                Console.WriteLine($"[API] Created category: {category.Id} '{category.Name}'");
                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    id = category.Id,
                    name = category.Name,
                    description = category.Description,
                    isActive = category.IsActive,
                    createdAt = category.CreatedAt,
                    updatedAt = category.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to create category: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to create category");
            }
        }

        [Route("{id}")]
        [HttpPut]
        [RequirePermission("categories.update", "categories")]
        public HttpResponseMessage Update(string id, [FromBody] UpdateCategoryRequest dto)
        {
            try
            {
                var category = _service.Update(id, dto);
                Console.WriteLine($"[API] Updated category: {category.Id} '{category.Name}'");
                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    id = category.Id,
                    name = category.Name,
                    description = category.Description,
                    isActive = category.IsActive,
                    createdAt = category.CreatedAt,
                    updatedAt = category.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to update category {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to update category");
            }
        }

        // NOTE: no DELETE — categories are deactivated only (spec §3.3 rule 6).
    }
}
