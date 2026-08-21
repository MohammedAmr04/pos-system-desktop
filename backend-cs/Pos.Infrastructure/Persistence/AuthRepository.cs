using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using PosCs.Application.Ports;
using PosCs.Domain.Entities;

namespace PosCs.Infrastructure.Persistence
{
    public class AuthRepository : IAuthRepository
    {
        private readonly IMachineIdProvider _machineId;

        public AuthRepository(IMachineIdProvider machineId)
        {
            _machineId = machineId;
        }

        public Domain.Entities.User GetUserByUsername(string username)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return conn.QueryFirstOrDefault<Domain.Entities.User>(
                    "SELECT * FROM User WHERE username = @username COLLATE NOCASE",
                    new { username });
        }

        public Domain.Entities.User GetUserById(string userId)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return conn.QueryFirstOrDefault<Domain.Entities.User>("SELECT * FROM User WHERE id = @userId", new { userId });
        }

        public List<string> GetRoleIdsForUser(string userId)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return conn.Query<string>(
                    "SELECT roleId FROM UserRole WHERE userId = @userId", new { userId }).ToList();
        }

        public List<string> GetPermissionKeysForUser(string userId)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return conn.Query<string>(@"
                    SELECT DISTINCT p.key
                    FROM Permission p
                    JOIN RolePermission rp ON rp.permissionId = p.id
                    JOIN UserRole ur ON ur.roleId = rp.roleId
                    WHERE ur.userId = @userId", new { userId }).ToList();
        }

        public string GetTenantIdForUser(string userId)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return conn.ExecuteScalar<string>(
                    "SELECT tenantId FROM User WHERE id = @userId", new { userId });
        }

        public List<string> GetEnabledFeatureKeys(string tenantId)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return conn.Query<string>(
                    "SELECT featureKey FROM TenantFeature WHERE tenantId = @tenantId AND enabled = 1",
                    new { tenantId }).ToList();
        }

        public List<TenantFeature> GetTenantFeatures(string tenantId)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return conn.Query<TenantFeature>(
                    "SELECT * FROM TenantFeature WHERE tenantId = @tenantId", new { tenantId }).ToList();
        }

        public void SetUserRoles(string userId, IEnumerable<string> roleIds)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                conn.Execute("DELETE FROM UserRole WHERE userId = @userId", new { userId });
                foreach (var roleId in roleIds.Distinct())
                    conn.Execute("INSERT OR IGNORE INTO UserRole (userId, roleId) VALUES (@userId, @roleId)",
                        new { userId, roleId });
            }
        }

        public string EnsureTokenSecret()
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                var settings = SettingsRepository.GetFirstCore(conn);
                if (settings == null)
                    settings = SettingsRepository.CreateCore(conn, _machineId.GetMachineId(), false);

                if (string.IsNullOrWhiteSpace(settings.TokenSecret))
                {
                    var secret = Convert.ToBase64String(Guid.NewGuid().ToByteArray()
                        .Concat(Guid.NewGuid().ToByteArray()).ToArray());
                    conn.Execute("UPDATE Settings SET tokenSecret = @secret, updatedAt = @now WHERE id = @id",
                        new { secret, now = DateTime.UtcNow, id = settings.Id });
                    return secret;
                }
                return settings.TokenSecret;
            }
        }
    }
}
