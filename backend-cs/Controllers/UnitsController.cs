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
    [RoutePrefix("api/units")]
    public class UnitsController : ApiController
    {
        private readonly UnitMasterService _service = CompositionRoot.UnitMasterService;

        [Route("")]
        [HttpGet]
        [RequirePermission("units.view")]
        public HttpResponseMessage GetActive()
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK, _service.GetActive());
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to fetch units: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to fetch units");
            }
        }

        [Route("paged")]
        [HttpGet]
        [RequirePermission("units.view")]
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
                Console.Error.WriteLine($"[API ERR] Failed to fetch units (paged): {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to fetch units");
            }
        }

        [Route("{id}")]
        [HttpGet]
        [RequirePermission("units.view")]
        public HttpResponseMessage GetById(string id)
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK, _service.GetById(id));
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to fetch unit {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to fetch unit");
            }
        }

        [Route("")]
        [HttpPost]
        [RequirePermission("units.create")]
        public HttpResponseMessage Create([FromBody] CreateUnitRequest dto)
        {
            try
            {
                var unit = _service.Create(dto);
                Console.WriteLine($"[API] Created unit: {unit.Id} '{unit.Name}'");
                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    id = unit.Id,
                    name = unit.Name,
                    isActive = unit.IsActive,
                    createdAt = unit.CreatedAt,
                    updatedAt = unit.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to create unit: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to create unit");
            }
        }

        [Route("{id}")]
        [HttpPut]
        [RequirePermission("units.update")]
        public HttpResponseMessage Update(string id, [FromBody] UpdateMasterUnitRequest dto)
        {
            try
            {
                var unit = _service.Update(id, dto);
                Console.WriteLine($"[API] Updated unit: {unit.Id} '{unit.Name}'");
                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    id = unit.Id,
                    name = unit.Name,
                    isActive = unit.IsActive,
                    createdAt = unit.CreatedAt,
                    updatedAt = unit.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to update unit {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to update unit");
            }
        }

        [Route("{id}")]
        [HttpDelete]
        [RequirePermission("units.delete")]
        public HttpResponseMessage Delete(string id)
        {
            try
            {
                _service.Delete(id);
                Console.WriteLine($"[API] Deleted unit: {id}");
                return Request.CreateResponse(HttpStatusCode.OK, new { success = true });
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to delete unit {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to delete unit");
            }
        }
    }
}
