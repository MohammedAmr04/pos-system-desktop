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
    [RoutePrefix("api/roles")]
    public class RolesController : ApiController
    {
        private readonly RolesService _service = CompositionRoot.RolesService;

        [Route("")]
        [HttpGet]
        [RequirePermission("roles.manage")]
        public HttpResponseMessage GetAll()
        {
            try
            {
                var roles = _service.GetAll();
                return Request.CreateResponse(HttpStatusCode.OK, roles.Select(r => new
                {
                    id = r.Id,
                    name = r.Name,
                    description = r.Description,
                    isSystem = r.IsSystem,
                    userCount = r.UserCount,
                    permissionCount = r.PermissionCount
                }));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to fetch roles: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to fetch roles");
            }
        }

        [Route("{id}")]
        [HttpGet]
        [RequirePermission("roles.manage")]
        public HttpResponseMessage GetById(string id)
        {
            try
            {
                var role = _service.GetById(id);
                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    id = role.Id,
                    name = role.Name,
                    description = role.Description,
                    isSystem = role.IsSystem,
                    userCount = role.UserCount
                });
            }
            catch (NotFoundException)
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Role not found");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to fetch role {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to fetch role");
            }
        }

        [Route("{id}/permissions")]
        [HttpGet]
        [RequirePermission("roles.manage")]
        public HttpResponseMessage GetPermissions(string id)
        {
            try
            {
                var permissionIds = _service.GetPermissionIds(id);
                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    roleId = id,
                    permissionIds
                });
            }
            catch (NotFoundException)
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Role not found");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to fetch role permissions {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to fetch role permissions");
            }
        }

        [Route("{id}/permissions")]
        [HttpPut]
        [RequirePermission("roles.manage")]
        public HttpResponseMessage SetPermissions(string id, [FromBody] SetRolePermissionsRequest dto)
        {
            try
            {
                var roleName = _service.FindRoleName(id);
                var distinctIds = _service.SetPermissions(id, dto);
                Console.WriteLine($"[API] Updated permissions for role {id} ({roleName})");
                return Request.CreateResponse(HttpStatusCode.OK, new { success = true, permissionIds = distinctIds });
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to set role permissions {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to set role permissions");
            }
        }

        [Route("")]
        [HttpPost]
        [RequirePermission("roles.manage")]
        public HttpResponseMessage Create([FromBody] CreateRoleRequest dto)
        {
            try
            {
                var role = _service.Create(dto);
                return Request.CreateResponse(HttpStatusCode.OK, role);
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to create role: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to create role");
            }
        }

        [Route("{id}")]
        [HttpPut]
        [RequirePermission("roles.manage")]
        public HttpResponseMessage Update(string id, [FromBody] UpdateRoleRequest dto)
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK, _service.Update(id, dto));
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to update role {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to update role");
            }
        }

        [Route("{id}")]
        [HttpDelete]
        [RequirePermission("roles.manage")]
        public HttpResponseMessage Delete(string id)
        {
            try
            {
                _service.Delete(id);
                return Request.CreateResponse(HttpStatusCode.OK, new { success = true });
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to delete role {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to delete role");
            }
        }
    }
}
