using Dapper;
using PosCs.Application.Ports;

namespace PosCs.Infrastructure.Persistence
{
    /// <summary>Permission/feature checks against the database.</summary>
    public class AccessControl : IAccessControl
    {
        public bool HasPermission(string userId, string permissionKey)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(permissionKey))
                return false;
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                var count = conn.ExecuteScalar<int>(@"
                    SELECT COUNT(1)
                    FROM Permission p
                    JOIN RolePermission rp ON rp.permissionId = p.id
                    JOIN UserRole ur ON ur.roleId = rp.roleId
                    WHERE ur.userId = @userId AND p.key = @permissionKey",
                    new { userId, permissionKey });
                return count > 0;
            }
        }

        public bool HasFeature(string tenantId, string featureKey)
        {
            if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(featureKey))
                return false;
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                var count = conn.ExecuteScalar<int>(@"
                    SELECT COUNT(1) FROM TenantFeature
                    WHERE tenantId = @tenantId AND featureKey = @featureKey AND enabled = 1",
                    new { tenantId, featureKey });
                return count > 0;
            }
        }

        public string GetTenantIdForUser(string userId)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return conn.ExecuteScalar<string>(
                    "SELECT tenantId FROM User WHERE id = @userId", new { userId });
        }
    }
}
