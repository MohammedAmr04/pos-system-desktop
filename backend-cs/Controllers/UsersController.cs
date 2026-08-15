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
using PosCs.Services;

namespace PosCs.Controllers
{
    [RoutePrefix("api/users")]
    public class UsersController : ApiController
    {
        private readonly UsersRepository _usersRepo = new UsersRepository();
        private readonly RolesRepository _rolesRepo = new RolesRepository();
        private readonly AuthRepository _authRepo = new AuthRepository();

        [Route("")]
        [HttpGet]
        [RequirePermission("users.manage")]
        public HttpResponseMessage GetAll()
        {
            try
            {
                using (var conn = DbConnectionFactory.CreateConnection())
                {
                    var tenantId = AuthorizationService.GetTenantIdForUser(conn,
                        AuthorizationService.GetCurrentUserId(Request));
                    var users = _usersRepo.GetAll(conn, tenantId).Select(u => new
                    {
                        id = u.Id,
                        name = u.Name,
                        username = u.Username,
                        isActive = u.IsActive,
                        roleIds = _usersRepo.GetRoleIdsForUser(conn, u.Id),
                        createdAt = u.CreatedAt
                    });
                    return Request.CreateResponse(HttpStatusCode.OK, users);
                }
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
        public HttpResponseMessage Create([FromBody] CreateUserDto dto)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Name required");
                if (string.IsNullOrWhiteSpace(dto.Username))
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Username required");
                if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 4 || dto.Password.Length > 64)
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Password must be 4-64 characters");
                if (dto.RoleIds == null || dto.RoleIds.Count == 0)
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "At least one role required");

                using (var conn = DbConnectionFactory.CreateConnection())
                {
                    var tenantId = AuthorizationService.GetTenantIdForUser(conn,
                        AuthorizationService.GetCurrentUserId(Request));

                    var unknownRoles = dto.RoleIds.Distinct().Where(rid => _rolesRepo.GetById(conn, rid) == null).ToList();
                    if (unknownRoles.Count > 0)
                        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, $"Unknown role(s): {string.Join(", ", unknownRoles)}");

                    var existing = _authRepo.GetUserByUsername(conn, dto.Username.Trim());
                    if (existing != null)
                        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, $"Username already taken: {dto.Username.Trim()}");

                    var user = _usersRepo.Create(conn, tenantId, dto.Name.Trim(), dto.Username.Trim(), AuthService.HashPassword(dto.Password));
                    _authRepo.SetUserRoles(conn, user.Id, dto.RoleIds);

                    Console.WriteLine($"[API] Created user: {user.Name}");
                    return Request.CreateResponse(HttpStatusCode.OK, new
                    {
                        id = user.Id,
                        name = user.Name,
                        username = user.Username,
                        isActive = user.IsActive,
                        roleIds = _usersRepo.GetRoleIdsForUser(conn, user.Id)
                    });
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to create user: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to create user");
            }
        }

        [Route("{id}")]
        [HttpPut]
        [RequirePermission("users.manage")]
        public HttpResponseMessage Update(string id, [FromBody] UpdateUserDto dto)
        {
            try
            {
                if (dto == null)
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid user data");

                using (var conn = DbConnectionFactory.CreateConnection())
                {
                    var user = _usersRepo.GetById(conn, id);
                    if (user == null)
                        return Request.CreateErrorResponse(HttpStatusCode.NotFound, "User not found");

                    var name = dto.Name?.Trim();
                    if (string.IsNullOrWhiteSpace(name))
                        name = user.Name;

                    var username = dto.Username?.Trim();
                    if (string.IsNullOrWhiteSpace(username))
                        username = user.Username;

                    if (!string.Equals(username, user.Username, StringComparison.OrdinalIgnoreCase))
                    {
                        var existing = _authRepo.GetUserByUsername(conn, username);
                        if (existing != null && existing.Id != id)
                            return Request.CreateErrorResponse(HttpStatusCode.BadRequest, $"Username already taken: {username}");
                    }

                    _usersRepo.Update(conn, id, name, username, dto.IsActive ?? user.IsActive);

                    if (!string.IsNullOrWhiteSpace(dto.Password))
                    {
                        if (dto.Password.Length < 4 || dto.Password.Length > 64)
                            return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Password must be 4-64 characters");
                        _usersRepo.UpdatePassword(conn, id, AuthService.HashPassword(dto.Password));
                    }

                    if (dto.RoleIds != null)
                    {
                        var unknownRoles = dto.RoleIds.Distinct().Where(rid => _rolesRepo.GetById(conn, rid) == null).ToList();
                        if (unknownRoles.Count > 0)
                            return Request.CreateErrorResponse(HttpStatusCode.BadRequest, $"Unknown role(s): {string.Join(", ", unknownRoles)}");
                        _authRepo.SetUserRoles(conn, id, dto.RoleIds);
                    }

                    Console.WriteLine($"[API] Updated user: {name}");
                    return Request.CreateResponse(HttpStatusCode.OK, new
                    {
                        id = user.Id,
                        name,
                        username,
                        isActive = dto.IsActive ?? user.IsActive,
                        roleIds = _usersRepo.GetRoleIdsForUser(conn, id)
                    });
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to update user {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to update user");
            }
        }
    }

    public class CreateUserDto
    {
        public string Name { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public List<string> RoleIds { get; set; }
    }

    public class UpdateUserDto
    {
        public string Name { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool? IsActive { get; set; }
        public List<string> RoleIds { get; set; }
    }
}
