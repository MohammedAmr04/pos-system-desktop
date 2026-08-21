using System.Collections.Generic;
using PosCs.Domain.Entities;

namespace PosCs.Application.Ports
{
    public interface IUsersRepository
    {
        List<User> GetAll(string tenantId);
        User GetById(string userId);
        List<string> GetRoleIdsForUser(string userId);
        User Create(string tenantId, string name, string username, string passwordHash);
        bool Update(string userId, string name, string username, bool isActive);
        bool UpdatePassword(string userId, string passwordHash);
    }

    public interface IRolesRepository
    {
        List<Role> GetAll();
        Role GetById(string roleId);
        Role FindByName(string name);
        Role FindByNameExcluding(string name, string roleId);
        List<string> GetPermissionIdsForRole(string roleId);
        int CountUsersForRole(string roleId);
        Role Create(string name, string description);
        bool Update(string roleId, string name, string description);
        bool Delete(string roleId);
        void SetRolePermissions(string roleId, IEnumerable<string> permissionIds);
    }

    public interface IPermissionsRepository
    {
        List<Permission> GetAll();
        /// <summary>Returns the subset of submitted keys that exist in the Permission table.</summary>
        System.Collections.Generic.HashSet<string> GetExistingKeys(IEnumerable<string> keys);
    }

    public interface ITenantFeatureRepository
    {
        void UpsertFeature(string tenantId, string featureKey, bool enabled);
    }

    /// <summary>Access to the single-row Settings table (license state + token secret).</summary>
    public interface ILicenseRepository
    {
        Settings GetByMachineId(string machineId);
        Settings GetFirst();
        Settings Create(string machineId, bool unlocked = false);
        void Upsert(string machineId, bool unlocked);
    }
}
