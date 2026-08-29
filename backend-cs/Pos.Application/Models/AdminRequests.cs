using System.Collections.Generic;

namespace PosCs.Application.Models
{
    public sealed class CreateUserRequest
    {
        public string Name { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public List<string> RoleIds { get; set; }
    }

    public sealed class UpdateUserRequest
    {
        public string Name { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool? IsActive { get; set; }
        public List<string> RoleIds { get; set; }
    }

    public sealed class CreateRoleRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public sealed class UpdateRoleRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public sealed class SetRolePermissionsRequest
    {
        public List<string> PermissionIds { get; set; }
    }

    public sealed class SetFeaturesRequest
    {
        public List<FeatureToggleRequest> Features { get; set; }
    }

    public sealed class FeatureToggleRequest
    {
        public string Key { get; set; }
        public bool Enabled { get; set; }
    }

    public sealed class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public sealed class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
    }

    public sealed class UnlockRequest
    {
        public string MachineId { get; set; }
        public string Code { get; set; }
    }
}
