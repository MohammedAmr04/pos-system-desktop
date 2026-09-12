using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Owin;
using PosCs.Application.Services;

namespace PosCs.Middleware
{
    /// <summary>
    /// Parses the bearer token (issued by AuthService) and stores the current
    /// user id in the OWIN environment under "PosCs.UserId". All /api/* routes
    /// except the explicit public allow-list require a valid token. Controller-level
    /// permission enforcement remains in RequirePermissionAttribute.
    /// Public endpoints: /api/auth/login, /api/license.
    /// </summary>
    public class ApiAuthMiddleware : OwinMiddleware
    {
        public ApiAuthMiddleware(OwinMiddleware next) : base(next) { }

        public override async Task Invoke(IOwinContext context)
        {
            var path = context.Request.Path.Value ?? "";
            bool isApi = path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase);

            if (isApi && !IsPublicEndpoint(path))
            {
                var userId = TryAuthenticate(context);
                if (string.IsNullOrEmpty(userId))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("{\"error\":\"Authentication required\"}");
                    return;
                }
            }

            await Next.Invoke(context);
        }

        private static string TryAuthenticate(IOwinContext context)
        {
            var header = context.Request.Headers.Get("Authorization");
            if (string.IsNullOrWhiteSpace(header) ||
                !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var token = header.Substring("Bearer ".Length).Trim();
            try
            {
                var bundle = CompositionRoot.AuthService.GetBundleForToken(token);
                if (bundle?.User == null)
                    return null;

                context.Set("PosCs.UserId", bundle.User.Id);
                context.Set("PosCs.TenantId", bundle.TenantId);
                return bundle.User.Id;
            }
            catch
            {
                return null;
            }
        }

        private static bool IsPublicEndpoint(string path)
        {
            if (path.Equals("/api/auth/login", StringComparison.OrdinalIgnoreCase))
                return true;
            if (path.Equals("/api/license", StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }
    }
}
