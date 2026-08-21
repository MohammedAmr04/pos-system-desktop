using PosCs.Domain.Entities;

namespace PosCs.Application.Ports
{
    /// <summary>Read access needed for authentication and access bundles.</summary>
    public interface IAuthRepository
    {
        User GetUserByUsername(string username);
        User GetUserById(string userId);
        System.Collections.Generic.List<string> GetRoleIdsForUser(string userId);
        System.Collections.Generic.List<string> GetPermissionKeysForUser(string userId);
        string GetTenantIdForUser(string userId);
        System.Collections.Generic.List<string> GetEnabledFeatureKeys(string tenantId);
        System.Collections.Generic.List<TenantFeature> GetTenantFeatures(string tenantId);
        void SetUserRoles(string userId, System.Collections.Generic.IEnumerable<string> roleIds);
        /// <summary>Returns the persisted token signing secret, creating it on first use.</summary>
        string EnsureTokenSecret();
    }
}
