using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using PosCs.Api;
using PosCs.Application.Models;
using PosCs.Application.Services;
using PosCs.Attributes;

namespace PosCs.Controllers
{
    [RoutePrefix("api/tenant")]
    public class TenantFeaturesController : ApiController
    {
        private readonly TenantFeaturesService _service = CompositionRoot.TenantFeaturesService;

        [Route("features")]
        [HttpGet]
        [RequirePermission("settings.view")]
        public HttpResponseMessage GetFeatures()
        {
            try
            {
                var features = _service.GetFeatures(Request.GetOwinContextUserId());
                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    features = features.Select(f => new { key = f.Key, enabled = f.Enabled })
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to fetch features: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to fetch features");
            }
        }

        [Route("features")]
        [HttpPut]
        [RequirePermission("settings.update")]
        public HttpResponseMessage SetFeatures([FromBody] SetFeaturesRequest dto)
        {
            try
            {
                var tenantId = _service.SetFeatures(Request.GetOwinContextUserId(), dto);
                Console.WriteLine($"[API] Updated tenant features for {tenantId}");
                return Request.CreateResponse(HttpStatusCode.OK, new { success = true });
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to update features: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to update features");
            }
        }
    }
}
