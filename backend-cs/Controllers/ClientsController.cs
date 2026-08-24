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
    [RoutePrefix("api/clients")]
    public class ClientsController : ApiController
    {
        private readonly ClientService _service = CompositionRoot.ClientService;

        [Route("")]
        [HttpGet]
        [RequirePermission("clients.view")]
        public HttpResponseMessage GetActive()
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK, _service.GetActive());
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to fetch clients: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to fetch clients");
            }
        }

        [Route("paged")]
        [HttpGet]
        [RequirePermission("clients.view")]
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
                Console.Error.WriteLine($"[API ERR] Failed to fetch clients (paged): {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to fetch clients");
            }
        }

        [Route("{id}")]
        [HttpGet]
        [RequirePermission("clients.view")]
        public HttpResponseMessage GetById(string id)
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK, _service.GetById(id));
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to fetch client {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to fetch client");
            }
        }

        [Route("")]
        [HttpPost]
        [RequirePermission("clients.create")]
        public HttpResponseMessage Create([FromBody] CreateClientRequest dto)
        {
            try
            {
                var client = _service.Create(dto);
                Console.WriteLine($"[API] Created client: {client.Id} '{client.Name}'");
                return Request.CreateResponse(HttpStatusCode.OK, client);
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to create client: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to create client");
            }
        }

        [Route("{id}")]
        [HttpPut]
        [RequirePermission("clients.update")]
        public HttpResponseMessage Update(string id, [FromBody] UpdateClientRequest dto)
        {
            try
            {
                var client = _service.Update(id, dto);
                Console.WriteLine($"[API] Updated client: {client.Id} '{client.Name}'");
                return Request.CreateResponse(HttpStatusCode.OK, client);
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to update client {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to update client");
            }
        }

        // NOTE: no DELETE — credit sales require a stable client identity; deactivate instead (spec §10.2).

        [Route("{id}/statement")]
        [HttpGet]
        [RequirePermission("clients.view")]
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
                Console.Error.WriteLine($"[API ERR] Failed to fetch client statement {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to fetch client statement");
            }
        }
    }
}
