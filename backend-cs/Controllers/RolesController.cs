using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Dapper;
using PosCs.Attributes;
using PosCs.Helpers;
using PosCs.Models;
using PosCs.Repositories;

namespace PosCs.Controllers
{
    [RoutePrefix("api/roles")]
    public class RolesController : ApiController
    {
        private const string AdminRoleId = "role-admin";

        private readonly RolesRepository _rolesRepo = new RolesRepository();
        private readonly UsersRepository _usersRepo = new UsersRepository();

        [Route("")]
        [HttpGet]
        [RequirePermission("roles.manage")]
        public HttpResponseMessage GetAll()
        {
            try
            {
                using (var conn = DbConnectionFactory.CreateConnection())
                {
                    var roles = _rolesRepo.GetAll(conn).Select(r => new
                    {
                        id = r.Id,
                        name = r.Name,
                        description = r.Description,
                        isSystem = r.IsSystem,
                        userCount = _rolesRepo.CountUsersForRole(conn, r.Id),
                        permissionCount = _rolesRepo.GetPermissionIdsForRole(conn, r.Id).Count()
                    });
                    return Request.CreateResponse(HttpStatusCode.OK, roles);
                }
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
                using (var conn = DbConnectionFactory.CreateConnection())
                {
                    var role = _rolesRepo.GetById(conn, id);
                    if (role == null)
                        return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Role not found");
                    return Request.CreateResponse(HttpStatusCode.OK, new
                    {
                        id = role.Id,
                        name = role.Name,
                        description = role.Description,
                        isSystem = role.IsSystem,
                        userCount = _rolesRepo.CountUsersForRole(conn, role.Id)
                    });
                }
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
                using (var conn = DbConnectionFactory.CreateConnection())
                {
                    var role = _rolesRepo.GetById(conn, id);
                    if (role == null)
                        return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Role not found");
                    return Request.CreateResponse(HttpStatusCode.OK, new
                    {
                        roleId = id,
                        permissionIds = _rolesRepo.GetPermissionIdsForRole(conn, id)
                    });
                }
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
        public HttpResponseMessage SetPermissions(string id, [FromBody] SetRolePermissionsDto dto)
        {
            try
            {
                if (dto?.PermissionIds == null)
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "permissionIds required");

                using (var conn = DbConnectionFactory.CreateConnection())
                {
                    var role = _rolesRepo.GetById(conn, id);
                    if (role == null)
                        return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Role not found");

                    if (string.Equals(id, AdminRoleId, StringComparison.OrdinalIgnoreCase))
                        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "The Admin role permissions cannot be modified");

                    var distinctIds = dto.PermissionIds.Distinct().ToList();
                    var known = conn.Query<string>(
                        "SELECT key FROM Permission WHERE key IN @ids", new { ids = distinctIds }).ToHashSet();
                    var unknown = distinctIds.Where(k => !known.Contains(k)).ToList();
                    if (unknown.Count > 0)
                        return Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                            $"Unknown permission(s): {string.Join(", ", unknown)}");

                    _rolesRepo.SetRolePermissions(conn, id, distinctIds);
                    Console.WriteLine($"[API] Updated permissions for role {id} ({role.Name})");
                    return Request.CreateResponse(HttpStatusCode.OK, new { success = true, permissionIds = distinctIds });
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to set role permissions {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to set role permissions");
            }
        }

        [Route("")]
        [HttpPost]
        [RequirePermission("roles.manage")]
        public HttpResponseMessage Create([FromBody] CreateRoleDto dto)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Role name required");

                using (var conn = DbConnectionFactory.CreateConnection())
                {
                    var name = dto.Name.Trim();
                    var existing = conn.QueryFirstOrDefault<Role>(
                        "SELECT * FROM Role WHERE name = @name", new { name });
                    if (existing != null)
                        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "A role with this name already exists");

                    var role = _rolesRepo.Create(conn, name, dto.Description?.Trim());
                    return Request.CreateResponse(HttpStatusCode.OK, role);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to create role: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to create role");
            }
        }

        [Route("{id}")]
        [HttpPut]
        [RequirePermission("roles.manage")]
        public HttpResponseMessage Update(string id, [FromBody] UpdateRoleDto dto)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Role name required");

                using (var conn = DbConnectionFactory.CreateConnection())
                {
                    var role = _rolesRepo.GetById(conn, id);
                    if (role == null)
                        return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Role not found");

                    if (role.IsSystem)
                        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "System roles cannot be renamed");

                    var name = dto.Name.Trim();
                    var existing = conn.QueryFirstOrDefault<Role>(
                        "SELECT * FROM Role WHERE name = @name AND id != @id", new { name, id });
                    if (existing != null)
                        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "A role with this name already exists");

                    _rolesRepo.Update(conn, id, name, dto.Description?.Trim());
                    return Request.CreateResponse(HttpStatusCode.OK, _rolesRepo.GetById(conn, id));
                }
            }
            catch (Exception ex)
            {
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
                using (var conn = DbConnectionFactory.CreateConnection())
                {
                    var role = _rolesRepo.GetById(conn, id);
                    if (role == null)
                        return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Role not found");

                    if (role.IsSystem)
                        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "System roles cannot be deleted");

                    if (_rolesRepo.CountUsersForRole(conn, id) > 0)
                        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Cannot delete a role that has users assigned");

                    _rolesRepo.Delete(conn, id);
                    return Request.CreateResponse(HttpStatusCode.OK, new { success = true });
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to delete role {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to delete role");
            }
        }
    }

    public class SetRolePermissionsDto
    {
        public List<string> PermissionIds { get; set; }
    }

    public class CreateRoleDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class UpdateRoleDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
