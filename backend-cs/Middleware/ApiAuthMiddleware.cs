using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using PosCs.Application.Services;

namespace PosCs.Middleware
{
    /// <summary>
    /// Parses the bearer token (issued by AuthService) and stores the current
    /// user id in the OWIN environment under "PosCs.UserId". Enforcement happens in
    /// the RequirePermissionAttribute — this middleware only authenticates.
    /// Public endpoints (health, license check, auth login) skip validation.
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
                var header = context.Request.Headers.Get("Authorization");
                if (!string.IsNullOrWhiteSpace(header) &&
                    header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    var token = header.Substring("Bearer ".Length).Trim();
                    try
                    {
                        var bundle = CompositionRoot.AuthService.GetBundleForToken(token);
                        if (bundle?.User != null)
                        {
                            context.Set("PosCs.UserId", bundle.User.Id);
                            context.Set("PosCs.TenantId", bundle.TenantId);
                        }
                    }
                    catch
                    {
                        // Treat as unauthenticated; RequirePermission will respond 401
                    }
                }
            }

            await Next.Invoke(context);
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
