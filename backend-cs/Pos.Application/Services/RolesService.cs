using System;
using System.Collections.Generic;
using System.Linq;
using PosCs.Application.Models;
using PosCs.Application.Ports;
using PosCs.Domain.Entities;
using PosCs.Domain.Exceptions;

namespace PosCs.Application.Services
{
    /// <summary>Role management use cases, including permission assignment.</summary>
    public class RolesService
    {
        private const string AdminRoleId = "role-admin";

        private readonly IRolesRepository _roles;
        private readonly IPermissionsRepository _permissions;

        public RolesService(IRolesRepository roles, IPermissionsRepository permissions)
        {
            _roles = roles;
            _permissions = permissions;
        }

        public List<RoleSummary> GetAll()
        {
            return _roles.GetAll()
                .Select(r => new RoleSummary
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    IsSystem = r.IsSystem,
                    UserCount = _roles.CountUsersForRole(r.Id),
                    PermissionCount = _roles.GetPermissionIdsForRole(r.Id).Count
                })
                .ToList();
        }

        public RoleSummary GetById(string id)
        {
            var role = _roles.GetById(id);
            if (role == null)
                throw new NotFoundException("Role not found");
            return ToSummary(role);
        }

        public List<string> GetPermissionIds(string id)
        {
            var role = _roles.GetById(id);
            if (role == null)
                throw new NotFoundException("Role not found");
            return _roles.GetPermissionIdsForRole(id);
        }

        /// <summary>Role display name for audit logging; null when the role does not exist.</summary>
        public string FindRoleName(string id)
        {
            return _roles.GetById(id)?.Name;
        }

        public List<string> SetPermissions(string id, SetRolePermissionsRequest dto)
        {
            if (dto?.PermissionIds == null)
                throw new DomainValidationException("permissionIds required");

            var role = _roles.GetById(id);
            if (role == null)
                throw new NotFoundException("Role not found");

            if (string.Equals(id, AdminRoleId, StringComparison.OrdinalIgnoreCase))
                throw new DomainValidationException("The Admin role permissions cannot be modified");

            var distinctIds = dto.PermissionIds.Distinct().ToList();
            var known = _permissions.GetExistingKeys(distinctIds);
            var unknown = distinctIds.Where(k => !known.Contains(k)).ToList();
            if (unknown.Count > 0)
                throw new DomainValidationException($"Unknown permission(s): {string.Join(", ", unknown)}");

            _roles.SetRolePermissions(id, distinctIds);
            return distinctIds;
        }

        public Role Create(CreateRoleRequest dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
                throw new DomainValidationException("Role name required");

            var name = dto.Name.Trim();
            if (_roles.FindByName(name) != null)
                throw new DomainValidationException("A role with this name already exists");

            return _roles.Create(name, dto.Description?.Trim());
        }

        public Role Update(string id, UpdateRoleRequest dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
                throw new DomainValidationException("Role name required");

            var role = _roles.GetById(id);
            if (role == null)
                throw new NotFoundException("Role not found");

            if (role.IsSystem)
                throw new DomainValidationException("System roles cannot be renamed");

            var name = dto.Name.Trim();
            if (_roles.FindByNameExcluding(name, id) != null)
                throw new DomainValidationException("A role with this name already exists");

            _roles.Update(id, name, dto.Description?.Trim());
            return _roles.GetById(id);
        }

        public void Delete(string id)
        {
            var role = _roles.GetById(id);
            if (role == null)
                throw new NotFoundException("Role not found");

            if (role.IsSystem)
                throw new DomainValidationException("System roles cannot be deleted");

            if (_roles.CountUsersForRole(id) > 0)
                throw new DomainValidationException("Cannot delete a role that has users assigned");

            _roles.Delete(id);
        }

        private RoleSummary ToSummary(Role role)
        {
            return new RoleSummary
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                IsSystem = role.IsSystem,
                UserCount = _roles.CountUsersForRole(role.Id),
                PermissionCount = 0
            };
        }
    }
}
