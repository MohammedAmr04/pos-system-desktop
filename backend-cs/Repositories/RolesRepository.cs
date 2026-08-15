using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using Microsoft.Data.Sqlite;
using PosCs.Models;

namespace PosCs.Repositories
{
    public class RolesRepository
    {
        public IEnumerable<Role> GetAll(SqliteConnection conn)
        {
            return conn.Query<Role>("SELECT * FROM Role ORDER BY isSystem DESC, name ASC");
        }

        public Role GetById(SqliteConnection conn, string roleId)
        {
            return conn.QueryFirstOrDefault<Role>("SELECT * FROM Role WHERE id = @roleId", new { roleId });
        }

        public IEnumerable<string> GetPermissionIdsForRole(SqliteConnection conn, string roleId)
        {
            return conn.Query<string>(
                "SELECT permissionId FROM RolePermission WHERE roleId = @roleId", new { roleId });
        }

        public int CountUsersForRole(SqliteConnection conn, string roleId)
        {
            return conn.ExecuteScalar<int>(
                "SELECT COUNT(1) FROM UserRole WHERE roleId = @roleId", new { roleId });
        }

        public Role Create(SqliteConnection conn, string name, string description)
        {
            var id = Guid.NewGuid().ToString("N");
            var now = DateTime.UtcNow;
            conn.Execute(@"
                INSERT INTO Role (id, name, description, isSystem, createdAt, updatedAt)
                VALUES (@id, @name, @description, 0, @createdAt, @updatedAt)",
                new { id, name, description, createdAt = now, updatedAt = now });
            return GetById(conn, id);
        }

        public bool Update(SqliteConnection conn, string roleId, string name, string description)
        {
            var affected = conn.Execute(@"
                UPDATE Role SET name = @name, description = @description, updatedAt = @now
                WHERE id = @roleId",
                new { roleId, name, description, now = DateTime.UtcNow });
            return affected > 0;
        }

        public bool Delete(SqliteConnection conn, string roleId)
        {
            return conn.Execute("DELETE FROM Role WHERE id = @roleId AND isSystem = 0", new { roleId }) > 0;
        }

        public void SetRolePermissions(SqliteConnection conn, string roleId, IEnumerable<string> permissionIds)
        {
            conn.Execute("DELETE FROM RolePermission WHERE roleId = @roleId", new { roleId });
            foreach (var permissionId in permissionIds.Distinct())
                conn.Execute("INSERT OR IGNORE INTO RolePermission (roleId, permissionId) VALUES (@roleId, @permissionId)",
                    new { roleId, permissionId });
        }
    }
}
