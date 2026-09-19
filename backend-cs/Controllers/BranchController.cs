using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using PosCs.Api;
using PosCs.Application.Models;
using PosCs.Attributes;

namespace PosCs.Controllers
{
    [RoutePrefix("api/branch")]
    public class BranchController : ApiController
    {
        [Route("context")]
        [HttpGet]
        [RequirePermission]
        public HttpResponseMessage Context()
        {
            try
            {
                var context = CompositionRoot.BranchService.GetRuntimeContext();
                return Request.CreateResponse(HttpStatusCode.OK, context);
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to load branch context: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to load branch context");
            }
        }

        [Route("list")]
        [HttpGet]
        [RequirePermission("branches.view")]
        public HttpResponseMessage List(bool activeOnly = true)
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK, CompositionRoot.BranchService.GetAll(activeOnly));
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to list branches: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to list branches");
            }
        }

        [Route("runtime")]
        [HttpPost]
        [RequirePermission("branches.manage")]
        public HttpResponseMessage Configure([FromBody] BranchRuntimeConfigurationRequest request)
        {
            try
            {
                if (request == null) return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid branch configuration");
                CompositionRoot.BranchService.Configure(request.BranchId, request.TerminalId, request.NodeRole, request.CentralBaseUrl);
                return Request.CreateResponse(HttpStatusCode.OK, CompositionRoot.BranchService.GetRuntimeContext());
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to configure branch runtime: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to configure branch runtime");
            }
        }
    }
}
