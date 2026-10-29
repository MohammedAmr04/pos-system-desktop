using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Owin;
using PosCs.Application.Ports;
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
                if (IsBranchAgentEndpoint(path))
                {
                    await Next.Invoke(context);
                    return;
                }

                if (IsLocalAgentEndpoint(path))
                {
                    if (!IsLoopbackRequest(context))
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync("{\"error\":\"Local agent endpoints are loopback only\"}");
                        return;
                    }

                    context.Set("PosCs.LocalAgent", true);
                    await Next.Invoke(context);
                    return;
                }

                if (IsEnrollmentEndpoint(path))
                {
                    await Next.Invoke(context);
                    return;
                }

                var userId = TryAuthenticate(context);
                if (string.IsNullOrEmpty(userId))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("{\"error\":\"Authentication required\"}");
                    return;
                }

                SetBranchScope(userId);
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

        /// <summary>Resolves the terminal's branch scope for this request. Cashier operations are
        /// forced to this branch; only users with <c>branches.view</c> (admin/manager) cross branches.</summary>
        private static void SetBranchScope(string userId)
        {
            var runtime = CompositionRoot.BranchesRepo.GetRuntimeContext();
            var canCrossBranch = CompositionRoot.Access.HasPermission(userId, "branches.view");
            BranchScope.Set(
                runtime == null || runtime.Branch == null ? null : runtime.Branch.Id,
                runtime == null || runtime.Terminal == null ? null : runtime.Terminal.Id,
                canCrossBranch);
        }

        private static bool IsPublicEndpoint(string path)
        {
            if (path.Equals("/api/auth/login", StringComparison.OrdinalIgnoreCase))
                return true;
            if (path.Equals("/api/license", StringComparison.OrdinalIgnoreCase))
                return true;
            if (path.Equals("/api/health", StringComparison.OrdinalIgnoreCase))
                return true;
            if (path.Equals("/api/branch/setup-status", StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }

        private static bool IsBranchAgentEndpoint(string path)
        {
            return path.StartsWith("/api/branch-sync/", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsLocalAgentEndpoint(string path)
        {
            return path.StartsWith("/api/local-agent/", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsEnrollmentEndpoint(string path)
        {
            return path.Equals("/api/branch/enroll", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsLoopbackRequest(IOwinContext context)
        {
            var address = context.Request.RemoteIpAddress;
            if (string.IsNullOrWhiteSpace(address)) return false;
            IPAddress parsed;
            return IPAddress.TryParse(address, out parsed) && IPAddress.IsLoopback(parsed);
        }
    }
}
