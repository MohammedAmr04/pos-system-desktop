using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using PosCs.Api;

namespace PosCs.Attributes
{
    /// <summary>
    /// Backend authorization filter. Requires a valid bearer token (set by the OWIN
    /// auth middleware) and, optionally, a permission and/or a tenant feature.
    /// The frontend is never the security boundary — this runs server-side.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class RequirePermissionAttribute : Attribute, IAuthorizationFilter
    {
        public string Permission { get; }
        public string Feature { get; }

        public RequirePermissionAttribute(string permission = null, string feature = null)
        {
            Permission = permission;
            Feature = feature;
        }

        public bool AllowMultiple => false;

        public Task<HttpResponseMessage> ExecuteAuthorizationFilterAsync(
            HttpActionContext actionContext,
            CancellationToken cancellationToken,
            Func<Task<HttpResponseMessage>> continuation)
        {
            var request = actionContext.Request;
            var userId = request.GetOwinContextUserId();

            if (string.IsNullOrEmpty(userId))
            {
                var response = request.CreateResponse(HttpStatusCode.Unauthorized,
                    new { error = "Authentication required" });
                return Task.FromResult(response);
            }

            var access = CompositionRoot.Access;
            var hasPermission = string.IsNullOrEmpty(Permission) ||
                access.HasPermission(userId, Permission);
            var hasFeature = string.IsNullOrEmpty(Feature) ||
                access.HasFeature(access.GetTenantIdForUser(userId), Feature);

            if (!hasPermission || !hasFeature)
            {
                var reason = !hasFeature ? "Feature disabled" : "Permission denied";
                var response = request.CreateResponse(HttpStatusCode.Forbidden, new { error = reason });
                return Task.FromResult(response);
            }

            return continuation();
        }
    }
}
