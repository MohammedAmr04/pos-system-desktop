using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using PosCs.Api;
using PosCs.Application.Models;
using PosCs.Application.Services;
using PosCs.Domain.Exceptions;
using PosCs.Attributes;

namespace PosCs.Controllers
{
    [RoutePrefix("api/users")]
    public class UsersController : ApiController
    {
        private readonly UsersService _service = CompositionRoot.UsersService;

        [Route("")]
        [HttpGet]
        [RequirePermission("users.manage")]
        public HttpResponseMessage GetAll()
        {
            try
            {
                var users = _service.GetAll(Request.GetOwinContextUserId());
                return Request.CreateResponse(HttpStatusCode.OK, users.Select(u => new
                {
                    id = u.Id,
                    name = u.Name,
                    username = u.Username,
                    isActive = u.IsActive,
                    roleIds = u.RoleIds,
                    createdAt = u.CreatedAt
                }));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to fetch users: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to fetch users");
            }
        }

        [Route("")]
        [HttpPost]
        [RequirePermission("users.manage")]
        public HttpResponseMessage Create([FromBody] CreateUserRequest dto)
        {
            try
            {
                var user = _service.Create(Request.GetOwinContextUserId(), dto);
                Console.WriteLine($"[API] Created user: {user.Name}");
                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    id = user.Id,
                    name = user.Name,
                    username = user.Username,
                    isActive = user.IsActive,
                    roleIds = user.RoleIds
                });
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to create user: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to create user");
            }
        }

        [Route("{id}")]
        [HttpPut]
        [RequirePermission("users.manage")]
        public HttpResponseMessage Update(string id, [FromBody] UpdateUserRequest dto)
        {
            try
            {
                var user = _service.Update(id, dto);
                Console.WriteLine($"[API] Updated user: {user.Name}");
                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    id = user.Id,
                    name = user.Name,
                    username = user.Username,
                    isActive = user.IsActive,
                    roleIds = user.RoleIds
                });
            }
            catch (NotFoundException)
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "User not found");
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to update user {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to update user");
            }
        }
    }
}
