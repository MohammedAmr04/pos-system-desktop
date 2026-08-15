using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Dapper;
using PosCs.Attributes;
using PosCs.Helpers;
using PosCs.Repositories;
using PosCs.Services;

namespace PosCs.Controllers
{
    [RoutePrefix("api/tenant")]
    public class TenantFeaturesController : ApiController
    {
        private readonly AuthRepository _authRepo = new AuthRepository();

        [Route("features")]
        [HttpGet]
        [RequirePermission("settings.view")]
        public HttpResponseMessage GetFeatures()
        {
            try
            {
                using (var conn = DbConnectionFactory.CreateConnection())
                {
                    var userId = AuthorizationService.GetCurrentUserId(Request);
                    var tenantId = AuthorizationService.GetTenantIdForUser(conn, userId);
                    var rows = _authRepo.GetTenantFeatures(conn, tenantId).ToList();
                    var byKey = rows.ToDictionary(r => r.FeatureKey, r => r.Enabled);

                    var features = FeatureCatalog.Keys.Select(key => new
                    {
                        key,
                        enabled = byKey.TryGetValue(key, out var enabled) ? enabled : false
                    });
                    return Request.CreateResponse(HttpStatusCode.OK, new { features });
                }
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
        public HttpResponseMessage SetFeatures([FromBody] SetFeaturesDto dto)
        {
            try
            {
                if (dto?.Features == null || dto.Features.Count == 0)
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "features required");

                using (var conn = DbConnectionFactory.CreateConnection())
                {
                    var userId = AuthorizationService.GetCurrentUserId(Request);
                    var tenantId = AuthorizationService.GetTenantIdForUser(conn, userId);

                    foreach (var feature in dto.Features)
                    {
                        if (!FeatureCatalog.IsKnown(feature.Key))
                            return Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                                $"Unknown feature: {feature.Key}");

                        conn.Execute(@"
                            INSERT INTO TenantFeature (tenantId, featureKey, enabled)
                            VALUES (@tenantId, @key, @enabled)
                            ON CONFLICT(tenantId, featureKey)
                            DO UPDATE SET enabled = @enabled",
                            new { tenantId, key = feature.Key, enabled = feature.Enabled ? 1 : 0 });
                    }

                    Console.WriteLine($"[API] Updated tenant features for {tenantId}");
                    return Request.CreateResponse(HttpStatusCode.OK, new { success = true });
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to update features: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to update features");
            }
        }
    }

    public class SetFeaturesDto
    {
        public List<FeatureToggleDto> Features { get; set; }
    }

    public class FeatureToggleDto
    {
        public string Key { get; set; }
        public bool Enabled { get; set; }
    }
}
