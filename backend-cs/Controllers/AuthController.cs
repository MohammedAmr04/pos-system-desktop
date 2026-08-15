using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using PosCs.Attributes;
using PosCs.Helpers;
using PosCs.Services;

namespace PosCs.Controllers
{
    [RoutePrefix("api/auth")]
    public class AuthController : ApiController
    {
        private static object ToAccessPayload(PosCs.Services.AccessBundle bundle)
        {
            return new
            {
                user = new
                {
                    id = bundle.User.Id,
                    name = bundle.User.Name,
                    isActive = bundle.User.IsActive,
                    tenantId = bundle.TenantId
                },
                roles = bundle.Roles,
                permissions = bundle.Permissions,
                features = bundle.Features
            };
        }

        [Route("login")]
        [HttpPost]
        public HttpResponseMessage Login([FromBody] LoginDto dto)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.Username))
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Username required");
                if (string.IsNullOrWhiteSpace(dto.Password))
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Password required");

                var username = dto.Username.Trim();
                if (AuthService.IsLockedOut(username))
                    return Request.CreateErrorResponse((HttpStatusCode)429,
                        "Too many attempts. Try again in a minute.");

                using (var conn = DbConnectionFactory.CreateConnection())
                {
                    var result = AuthService.Login(conn, username, dto.Password);
                    if (result.Bundle == null)
                    {
                        AuthService.RecordFailure(username);
                        return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "Invalid username or password");
                    }

                    AuthService.ResetThrottle(username);
                    Console.WriteLine($"[API] Login: {result.Bundle.User.Name} ({result.Bundle.Roles.FirstOrDefault() ?? "no role"})");
                    return Request.CreateResponse(HttpStatusCode.OK, new
                    {
                        token = result.Token,
                        access = ToAccessPayload(result.Bundle)
                    });
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Login failed: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Login failed");
            }
        }

        [Route("me")]
        [HttpGet]
        [RequirePermission]
        public HttpResponseMessage Me()
        {
            try
            {
                var userId = AuthorizationService.GetCurrentUserId(Request);
                using (var conn = DbConnectionFactory.CreateConnection())
                {
                    var bundle = AuthService.GetBundleForToken(conn, ExtractBearerToken());
                    if (bundle?.User == null || bundle.User.Id != userId)
                        return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "Invalid session");
                    return Request.CreateResponse(HttpStatusCode.OK, ToAccessPayload(bundle));
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] /auth/me failed: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to load access");
            }
        }

        private string ExtractBearerToken()
        {
            var header = Request.Headers.Authorization;
            if (header != null &&
                string.Equals(header.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase))
                return header.Parameter;
            return null;
        }
    }

    public class LoginDto
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
