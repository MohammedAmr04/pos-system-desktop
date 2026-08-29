using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using PosCs.Api;
using PosCs.Application.Models;
using PosCs.Application.Services;
using PosCs.Attributes;
using PosCs.Domain.Exceptions;

namespace PosCs.Controllers
{
    [RoutePrefix("api/auth")]
    public class AuthController : ApiController
    {
        private readonly AuthService _auth = CompositionRoot.AuthService;
        private readonly UsersService _users = CompositionRoot.UsersService;

        private static object ToAccessPayload(AccessBundle bundle)
        {
            return new
            {
                user = new
                {
                    id = bundle.User.Id,
                    name = bundle.User.Name,
                    isActive = bundle.User.IsActive,
                    mustChangePassword = bundle.User.MustChangePassword,
                    tenantId = bundle.TenantId
                },
                roles = bundle.Roles,
                permissions = bundle.Permissions,
                features = bundle.Features
            };
        }

        [Route("login")]
        [HttpPost]
        public HttpResponseMessage Login([FromBody] LoginRequest dto)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.Username))
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Username required");
                if (string.IsNullOrWhiteSpace(dto.Password))
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Password required");

                var result = _auth.Login(dto.Username.Trim(), dto.Password);
                if (result.Bundle == null)
                    return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "Invalid username or password");

                Console.WriteLine($"[API] Login: {result.Bundle.User.Name} ({result.Bundle.Roles.FirstOrDefault() ?? "no role"})");
                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    token = result.Token,
                    access = ToAccessPayload(result.Bundle)
                });
            }
            catch (Exception ex)
            {
                if (ex is LoginLockedException)
                    return ApiErrors.From(Request, ex, null);
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
                var userId = Request.GetOwinContextUserId();
                var bundle = _auth.GetBundleForToken(ExtractBearerToken());
                if (bundle?.User == null || bundle.User.Id != userId)
                    return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "Invalid session");
                return Request.CreateResponse(HttpStatusCode.OK, ToAccessPayload(bundle));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] /auth/me failed: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to load access");
            }
        }

        [Route("change-password")]
        [HttpPost]
        [RequirePermission]
        public HttpResponseMessage ChangePassword([FromBody] ChangePasswordRequest dto)
        {
            try
            {
                if (dto == null)
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid request");

                var userId = Request.GetOwinContextUserId();
                if (string.IsNullOrEmpty(userId))
                    return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "Authentication required");

                _users.ChangePassword(userId, dto.CurrentPassword, dto.NewPassword);
                Console.WriteLine($"[API] Password changed for user {userId}");
                return Request.CreateResponse(HttpStatusCode.OK, new { success = true });
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Change password failed: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to change password");
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
}
