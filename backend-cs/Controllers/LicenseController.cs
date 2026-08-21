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
    [RoutePrefix("api/license")]
    public class LicenseController : ApiController
    {
        private readonly LicenseService _service = CompositionRoot.LicenseService;

        [Route("")]
        [HttpGet]
        public HttpResponseMessage Get()
        {
            try
            {
                var status = _service.GetStatus();
                if (status.DaysSinceActivation.HasValue)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, new
                    {
                        status = status.Status,
                        machineId = status.MachineId,
                        daysSinceActivation = status.DaysSinceActivation.Value
                    });
                }
                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    status = status.Status,
                    machineId = status.MachineId
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] License check failed: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "License check failed");
            }
        }

        // Unlock requires an authenticated user with license.manage AND a valid
        // server-side unlock code. The old behavior (any caller, hardcoded client code)
        // is gone.
        [Route("unlock")]
        [HttpPost]
        [RequirePermission("license.manage")]
        public HttpResponseMessage Unlock([FromBody] UnlockRequest dto)
        {
            try
            {
                var machineId = _service.Unlock(dto?.MachineId, dto?.Code);
                Console.WriteLine($"[API] License unlocked for machine: {machineId} (by {Request.GetOwinContextUserId()})");
                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    success = true,
                    machineId
                });
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] License unlock failed: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "License unlock failed");
            }
        }
    }
}
