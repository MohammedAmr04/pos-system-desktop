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
    [RoutePrefix("api/suppliers")]
    public class SuppliersController : ApiController
    {
        private readonly SupplierService _service = CompositionRoot.SupplierService;

        [Route("")]
        [HttpGet]
        [RequirePermission("suppliers.view")]
        public HttpResponseMessage GetActive()
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK, _service.GetActive());
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to fetch suppliers: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to fetch suppliers");
            }
        }

        [Route("paged")]
        [HttpGet]
        [RequirePermission("suppliers.view")]
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
                Console.Error.WriteLine($"[API ERR] Failed to fetch suppliers (paged): {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to fetch suppliers");
            }
        }

        [Route("{id}")]
        [HttpGet]
        [RequirePermission("suppliers.view")]
        public HttpResponseMessage GetById(string id)
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK, _service.GetById(id));
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to fetch supplier {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to fetch supplier");
            }
        }

        [Route("")]
        [HttpPost]
        [RequirePermission("suppliers.create")]
        public HttpResponseMessage Create([FromBody] CreateSupplierRequest dto)
        {
            try
            {
                var supplier = _service.Create(dto);
                Console.WriteLine($"[API] Created supplier: {supplier.Id} '{supplier.Name}'");
                return Request.CreateResponse(HttpStatusCode.OK, supplier);
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to create supplier: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to create supplier");
            }
        }

        [Route("{id}")]
        [HttpPut]
        [RequirePermission("suppliers.update")]
        public HttpResponseMessage Update(string id, [FromBody] UpdateSupplierRequest dto)
        {
            try
            {
                var supplier = _service.Update(id, dto);
                Console.WriteLine($"[API] Updated supplier: {supplier.Id} '{supplier.Name}'");
                return Request.CreateResponse(HttpStatusCode.OK, supplier);
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to update supplier {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to update supplier");
            }
        }

        // NOTE: no DELETE — referenced suppliers must keep their identity; deactivate instead (spec §9.2 rule 7).

        [Route("{id}/statement")]
        [HttpGet]
        [RequirePermission("suppliers.view")]
        public HttpResponseMessage GetStatement(string id)
        {
            try
            {
                var statement = _service.GetStatement(id);
                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    partyId = statement.PartyId,
                    balance = statement.Balance,
                    entries = statement.Entries
                });
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to fetch supplier statement {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to fetch supplier statement");
            }
        }
    }
}
