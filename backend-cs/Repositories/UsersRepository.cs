using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using Microsoft.Data.Sqlite;
using PosCs.Models;

namespace PosCs.Repositories
{
    public class UsersRepository
    {
        public IEnumerable<User> GetAll(SqliteConnection conn, string tenantId)
        {
            return conn.Query<User>("SELECT * FROM User WHERE tenantId = @tenantId ORDER BY name ASC", new { tenantId });
        }

        public User GetById(SqliteConnection conn, string userId)
        {
            return conn.QueryFirstOrDefault<User>("SELECT * FROM User WHERE id = @userId", new { userId });
        }

        public IEnumerable<string> GetRoleIdsForUser(SqliteConnection conn, string userId)
        {
            return conn.Query<string>("SELECT roleId FROM UserRole WHERE userId = @userId", new { userId });
        }

        public User Create(SqliteConnection conn, string tenantId, string name, string username, string passwordHash)
        {
            var id = Guid.NewGuid().ToString("N");
            var now = DateTime.UtcNow;
            conn.Execute(@"
                INSERT INTO User (id, tenantId, name, username, passwordHash, isActive, createdAt, updatedAt)
                VALUES (@id, @tenantId, @name, @username, @passwordHash, 1, @createdAt, @updatedAt)",
                new { id, tenantId, name, username, passwordHash, createdAt = now, updatedAt = now });
            return GetById(conn, id);
        }

        public bool Update(SqliteConnection conn, string userId, string name, string username, bool isActive)
        {
            return conn.Execute(@"
                UPDATE User SET name = @name, username = @username, isActive = @isActive, updatedAt = @now
                WHERE id = @userId",
                new { userId, name, username, isActive = isActive ? 1 : 0, now = DateTime.UtcNow }) > 0;
        }

        public bool UpdatePassword(SqliteConnection conn, string userId, string passwordHash)
        {
            return conn.Execute(@"
                UPDATE User SET passwordHash = @passwordHash, updatedAt = @now WHERE id = @userId",
                new { userId, passwordHash, now = DateTime.UtcNow }) > 0;
        }

        public IEnumerable<string> GetUserIdsForRole(SqliteConnection conn, string roleId)
        {
            return conn.Query<string>("SELECT userId FROM UserRole WHERE roleId = @roleId", new { roleId });
        }
    }
}
