using System.Net.Http;
using Dapper;
using Microsoft.Data.Sqlite;
using PosCs.Repositories;

namespace PosCs.Services
{
    public static class AuthorizationService
    {
        private static readonly AuthRepository _repo = new AuthRepository();

        public static string GetCurrentUserId(HttpRequestMessage request)
        {
            try
            {
                var owin = request.GetOwinContext();
                if (owin?.Environment != null &&
                    owin.Environment.TryGetValue("PosCs.UserId", out var value) &&
                    value is string userId)
                {
                    return userId;
                }
            }
            catch
            {
                // OWIN context unavailable
            }
            return null;
        }

        public static bool HasPermission(SqliteConnection conn, string userId, string permissionKey)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(permissionKey))
                return false;
            var count = conn.ExecuteScalar<int>(@"
                SELECT COUNT(1)
                FROM Permission p
                JOIN RolePermission rp ON rp.permissionId = p.id
                JOIN UserRole ur ON ur.roleId = rp.roleId
                WHERE ur.userId = @userId AND p.key = @permissionKey",
                new { userId, permissionKey });
            return count > 0;
        }

        public static bool HasFeature(SqliteConnection conn, string tenantId, string featureKey)
        {
            if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(featureKey))
                return false;
            var count = conn.ExecuteScalar<int>(@"
                SELECT COUNT(1) FROM TenantFeature
                WHERE tenantId = @tenantId AND featureKey = @featureKey AND enabled = 1",
                new { tenantId, featureKey });
            return count > 0;
        }

        public static string GetTenantIdForUser(SqliteConnection conn, string userId)
        {
            return _repo.GetTenantIdForUser(conn, userId);
        }

        /// <summary>
        /// Checks that the user has the given permission AND that the tenant has the
        /// given feature enabled (when provided). Returns true when both hold.
        /// </summary>
        public static bool IsAllowed(SqliteConnection conn, string userId, string permissionKey = null, string featureKey = null)
        {
            if (string.IsNullOrEmpty(userId))
                return false;

            if (!string.IsNullOrEmpty(permissionKey) && !HasPermission(conn, userId, permissionKey))
                return false;

            if (!string.IsNullOrEmpty(featureKey))
            {
                var tenantId = _repo.GetTenantIdForUser(conn, userId);
                if (!HasFeature(conn, tenantId, featureKey))
                    return false;
            }

            return true;
        }
    }
}
