using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using Microsoft.Data.Sqlite;
using PosCs.Helpers;
using PosCs.Models;

namespace PosCs.Repositories
{
    public class AuthRepository
    {
        public IEnumerable<User> GetActiveUsers(SqliteConnection conn)
        {
            return conn.Query<User>("SELECT * FROM User WHERE isActive = 1");
        }

        public User GetUserByUsername(SqliteConnection conn, string username)
        {
            return conn.QueryFirstOrDefault<User>(
                "SELECT * FROM User WHERE username = @username COLLATE NOCASE",
                new { username });
        }

        public User GetUserById(SqliteConnection conn, string userId)
        {
            return conn.QueryFirstOrDefault<User>("SELECT * FROM User WHERE id = @userId", new { userId });
        }

        public IEnumerable<string> GetRoleIdsForUser(SqliteConnection conn, string userId)
        {
            return conn.Query<string>(
                "SELECT roleId FROM UserRole WHERE userId = @userId", new { userId });
        }

        public IEnumerable<string> GetPermissionKeysForUser(SqliteConnection conn, string userId)
        {
            return conn.Query<string>(@"
                SELECT DISTINCT p.key
                FROM Permission p
                JOIN RolePermission rp ON rp.permissionId = p.id
                JOIN UserRole ur ON ur.roleId = rp.roleId
                WHERE ur.userId = @userId", new { userId });
        }

        public IEnumerable<string> GetEnabledFeatureKeys(SqliteConnection conn, string tenantId)
        {
            return conn.Query<string>(
                "SELECT featureKey FROM TenantFeature WHERE tenantId = @tenantId AND enabled = 1",
                new { tenantId });
        }

        public IEnumerable<TenantFeature> GetTenantFeatures(SqliteConnection conn, string tenantId)
        {
            return conn.Query<TenantFeature>(
                "SELECT * FROM TenantFeature WHERE tenantId = @tenantId", new { tenantId });
        }

        public string GetTenantIdForUser(SqliteConnection conn, string userId)
        {
            return conn.ExecuteScalar<string>(
                "SELECT tenantId FROM User WHERE id = @userId", new { userId });
        }

        public void SetUserRoles(SqliteConnection conn, string userId, IEnumerable<string> roleIds)
        {
            conn.Execute("DELETE FROM UserRole WHERE userId = @userId", new { userId });
            foreach (var roleId in roleIds.Distinct())
                conn.Execute("INSERT OR IGNORE INTO UserRole (userId, roleId) VALUES (@userId, @roleId)",
                    new { userId, roleId });
        }

        public string EnsureTokenSecret(SqliteConnection conn)
        {
            var settingsRepo = new SettingsRepository();
            var settings = settingsRepo.GetFirst(conn);
            if (settings == null)
            {
                settings = settingsRepo.Create(conn, MachineId.GetMachineId(), false);
            }

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
