using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using PosCs.Api;
using PosCs.Application.Models;
using PosCs.Attributes;

namespace PosCs.Controllers
{
    [RoutePrefix("api/terminals")]
    public class TerminalController : ApiController
    {
        [Route("")]
        [HttpGet]
        [RequirePermission("branches.view")]
        public HttpResponseMessage GetPaged(int page = 1, int pageSize = 20, string branchId = null, bool activeOnly = true)
        {
            try
            {
                var result = CompositionRoot.TerminalService.GetPaged(page, pageSize, branchId, activeOnly);
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
                Console.Error.WriteLine($"[API ERR] Failed to page terminals: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to page terminals");
            }
        }

        [Route("{id}")]
        [HttpGet]
        [RequirePermission("branches.view")]
        public HttpResponseMessage GetById(string id)
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK, CompositionRoot.TerminalService.GetById(id));
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to load terminal {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to load terminal");
            }
        }

        [Route("{id}/history")]
        [HttpGet]
        [RequirePermission("branches.view")]
        public HttpResponseMessage History(string id, int limit = 20)
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK, CompositionRoot.TerminalService.GetHistory(id, limit));
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to load terminal history {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to load terminal history");
            }
        }

        [Route("")]
        [HttpPost]
        [RequirePermission("branches.manage")]
        public HttpResponseMessage Create([FromBody] CreateTerminalRequest request)
        {
            try
            {
                var terminal = CompositionRoot.TerminalService.Create(request);
                Console.WriteLine($"[API] Registered terminal: {terminal.Id} ({terminal.Name})");
                return Request.CreateResponse(HttpStatusCode.OK, terminal);
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to register terminal: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to register terminal");
            }
        }

        [Route("{id}")]
        [HttpPut]
        [RequirePermission("branches.manage")]
        public HttpResponseMessage Update(string id, [FromBody] UpdateTerminalRequest request)
        {
            try
            {
                var terminal = CompositionRoot.TerminalService.Update(id, request);
                Console.WriteLine($"[API] Updated terminal: {terminal.Id}");
                return Request.CreateResponse(HttpStatusCode.OK, terminal);
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to update terminal {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to update terminal");
            }
        }

        [Route("{id}/activate")]
        [HttpPost]
        [RequirePermission("branches.manage")]
        public HttpResponseMessage Activate(string id)
        {
            return SetActive(id, true);
        }

        [Route("{id}/deactivate")]
        [HttpPost]
        [RequirePermission("branches.manage")]
        public HttpResponseMessage Deactivate(string id)
        {
            return SetActive(id, false);
        }

        private HttpResponseMessage SetActive(string id, bool isActive)
        {
            try
            {
                CompositionRoot.TerminalService.SetActive(id, isActive);
                return Request.CreateResponse(HttpStatusCode.OK, new { success = true });
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to update terminal state: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to update terminal state");
            }
        }
    }
}
