using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using Microsoft.Data.Sqlite;
using PosCs.Application.Ports;
using PosCs.Domain.Entities;

namespace PosCs.Infrastructure.Persistence
{
    public class RolesRepository : IRolesRepository
    {
        public List<Role> GetAll()
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return conn.Query<Role>("SELECT * FROM Role ORDER BY isSystem DESC, name ASC").ToList();
        }

        public Role GetById(string roleId)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return GetByIdCore(conn, roleId);
        }

        public Role FindByName(string name)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return conn.QueryFirstOrDefault<Role>("SELECT * FROM Role WHERE name = @name", new { name });
        }

        public Role FindByNameExcluding(string name, string roleId)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return conn.QueryFirstOrDefault<Role>(
                    "SELECT * FROM Role WHERE name = @name AND id != @roleId", new { name, roleId });
        }

        public List<string> GetPermissionIdsForRole(string roleId)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return conn.Query<string>(
                    "SELECT permissionId FROM RolePermission WHERE roleId = @roleId", new { roleId }).ToList();
        }

        public int CountUsersForRole(string roleId)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return conn.ExecuteScalar<int>(
                    "SELECT COUNT(1) FROM UserRole WHERE roleId = @roleId", new { roleId });
        }

        public Role Create(string name, string description)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                var id = Guid.NewGuid().ToString("N");
                var now = DateTime.UtcNow;
                conn.Execute(@"
                    INSERT INTO Role (id, name, description, isSystem, createdAt, updatedAt)
                    VALUES (@id, @name, @description, 0, @createdAt, @updatedAt)",
                    new { id, name, description, createdAt = now, updatedAt = now });
                return GetByIdCore(conn, id);
            }
        }

        public bool Update(string roleId, string name, string description)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                return conn.Execute(@"
                    UPDATE Role SET name = @name, description = @description, updatedAt = @now
                    WHERE id = @roleId",
                    new { roleId, name, description, now = DateTime.UtcNow }) > 0;
            }
        }

        public bool Delete(string roleId)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return conn.Execute("DELETE FROM Role WHERE id = @roleId AND isSystem = 0", new { roleId }) > 0;
        }

        public void SetRolePermissions(string roleId, IEnumerable<string> permissionIds)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                conn.Execute("DELETE FROM RolePermission WHERE roleId = @roleId", new { roleId });
                foreach (var permissionId in permissionIds.Distinct())
                    conn.Execute("INSERT OR IGNORE INTO RolePermission (roleId, permissionId) VALUES (@roleId, @permissionId)",
                        new { roleId, permissionId });
            }
        }

        private static Role GetByIdCore(SqliteConnection conn, string roleId)
        {
            return conn.QueryFirstOrDefault<Role>("SELECT * FROM Role WHERE id = @roleId", new { roleId });
        }
    }
}
