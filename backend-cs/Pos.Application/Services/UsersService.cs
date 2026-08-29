using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using PosCs.Application.Models;
using PosCs.Application.Ports;
using PosCs.Domain.Exceptions;

namespace PosCs.Application.Services
{
    /// <summary>Password complexity rules.</summary>
    public static class PasswordPolicy
    {
        public const int MinimumLength = 8;
        public const int MaximumLength = 64;

        public static void Validate(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < MinimumLength || password.Length > MaximumLength)
                throw new DomainValidationException($"Password must be {MinimumLength}-{MaximumLength} characters");
            if (!Regex.IsMatch(password, @"[a-z]"))
                throw new DomainValidationException("Password must contain at least one lowercase letter");
            if (!Regex.IsMatch(password, @"[A-Z]"))
                throw new DomainValidationException("Password must contain at least one uppercase letter");
            if (!Regex.IsMatch(password, @"[0-9]"))
                throw new DomainValidationException("Password must contain at least one digit");
            if (!Regex.IsMatch(password, @"[^a-zA-Z0-9]"))
                throw new DomainValidationException("Password must contain at least one symbol");
        }
    }

    /// <summary>User management use cases (tenant-scoped).</summary>
    public class UsersService
    {
        private readonly IUsersRepository _users;
        private readonly IAuthRepository _auth;
        private readonly IRolesRepository _roles;
        private readonly IPasswordHasher _hasher;
        private readonly IAccessControl _access;

        public UsersService(IUsersRepository users, IAuthRepository auth, IRolesRepository roles,
            IPasswordHasher hasher, IAccessControl access)
        {
            _users = users;
            _auth = auth;
            _roles = roles;
            _hasher = hasher;
            _access = access;
        }

        public List<UserSummary> GetAll(string currentUserId)
        {
            var tenantId = _access.GetTenantIdForUser(currentUserId);
            return _users.GetAll(tenantId)
                .Select(u => new UserSummary
                {
                    Id = u.Id,
                    Name = u.Name,
                    Username = u.Username,
                    IsActive = u.IsActive,
                    RoleIds = _users.GetRoleIdsForUser(u.Id),
                    CreatedAt = u.CreatedAt
                })
                .ToList();
        }

        public UserSummary Create(string currentUserId, CreateUserRequest dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
                throw new DomainValidationException("Name required");
            if (string.IsNullOrWhiteSpace(dto.Username))
                throw new DomainValidationException("Username required");
            PasswordPolicy.Validate(dto.Password);
            if (dto.RoleIds == null || dto.RoleIds.Count == 0)
                throw new DomainValidationException("At least one role required");

            var tenantId = _access.GetTenantIdForUser(currentUserId);

            ValidateRolesExist(dto.RoleIds);

            var username = dto.Username.Trim();
            var existing = _auth.GetUserByUsername(username);
            if (existing != null)
                throw new DomainValidationException($"Username already taken: {username}");

            var user = _users.Create(tenantId, dto.Name.Trim(), username, _hasher.Hash(dto.Password));
            _auth.SetUserRoles(user.Id, dto.RoleIds);

            return new UserSummary
            {
                Id = user.Id,
                Name = user.Name,
                Username = user.Username,
                IsActive = user.IsActive,
                RoleIds = _users.GetRoleIdsForUser(user.Id)
            };
        }

        public UserSummary Update(string id, UpdateUserRequest dto)
        {
            if (dto == null)
                throw new DomainValidationException("Invalid user data");

            var user = _users.GetById(id);
            if (user == null)
                throw new NotFoundException("User not found");

            var name = dto.Name?.Trim();
            if (string.IsNullOrWhiteSpace(name))
                name = user.Name;

            var username = dto.Username?.Trim();
            if (string.IsNullOrWhiteSpace(username))
                username = user.Username;

            if (!string.Equals(username, user.Username, StringComparison.OrdinalIgnoreCase))
            {
                var existing = _auth.GetUserByUsername(username);
                if (existing != null && existing.Id != id)
                    throw new DomainValidationException($"Username already taken: {username}");
            }

            var isActive = dto.IsActive ?? user.IsActive;
            _users.Update(id, name, username, isActive);

            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                PasswordPolicy.Validate(dto.Password);
                _users.UpdatePassword(id, _hasher.Hash(dto.Password));
            }

            if (dto.RoleIds != null)
            {
                ValidateRolesExist(dto.RoleIds);
                _auth.SetUserRoles(id, dto.RoleIds);
            }

            return new UserSummary
            {
                Id = user.Id,
                Name = name,
                Username = username,
                IsActive = isActive,
                RoleIds = _users.GetRoleIdsForUser(id)
            };
        }

        public void ChangePassword(string userId, string currentPassword, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword))
                throw new DomainValidationException("New password required");

            var user = _users.GetById(userId);
            if (user == null)
                throw new NotFoundException("User not found");

            if (!_hasher.Verify(user.PasswordHash, currentPassword))
                throw new DomainValidationException("Current password is incorrect");

            PasswordPolicy.Validate(newPassword);
            _users.ChangePassword(userId, _hasher.Hash(newPassword));
        }

        private void ValidateRolesExist(List<string> roleIds)
        {
            var unknownRoles = roleIds.Distinct().Where(rid => _roles.GetById(rid) == null).ToList();
            if (unknownRoles.Count > 0)
                throw new DomainValidationException($"Unknown role(s): {string.Join(", ", unknownRoles)}");
        }
    }
}
