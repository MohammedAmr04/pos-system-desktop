using Dapper;
using PosCs.Application.Ports;

namespace PosCs.Infrastructure.Persistence
{
    public class TenantFeatureRepository : ITenantFeatureRepository
    {
        public void UpsertFeature(string tenantId, string featureKey, bool enabled)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                conn.Execute(@"
                    INSERT INTO TenantFeature (tenantId, featureKey, enabled)
                    VALUES (@tenantId, @key, @enabled)
                    ON CONFLICT(tenantId, featureKey)
                    DO UPDATE SET enabled = @enabled",
                    new { tenantId, key = featureKey, enabled = enabled ? 1 : 0 });
            }
        }
    }
}
