using System.Collections.Generic;
using System.Linq;
using Dapper;
using PosCs.Application.Ports;
using PosCs.Domain.Entities;

namespace PosCs.Infrastructure.Persistence
{
    public class PermissionsRepository : IPermissionsRepository
    {
        public List<Permission> GetAll()
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return conn.Query<Permission>("SELECT * FROM Permission ORDER BY resource ASC, action ASC").ToList();
        }

        public HashSet<string> GetExistingKeys(IEnumerable<string> keys)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                var rows = conn.Query<string>(
                    "SELECT key FROM Permission WHERE key IN @ids", new { ids = keys.ToList() });
                return new HashSet<string>(rows);
            }
        }
    }
}
