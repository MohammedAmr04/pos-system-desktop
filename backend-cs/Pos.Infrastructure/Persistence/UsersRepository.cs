using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using PosCs.Application.Ports;
using PosCs.Domain.Entities;

namespace PosCs.Infrastructure.Persistence
{
    public class UsersRepository : IUsersRepository
    {
        public List<User> GetAll(string tenantId)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return conn.Query<User>("SELECT * FROM User WHERE tenantId = @tenantId ORDER BY name ASC",
                    new { tenantId }).ToList();
        }

        public User GetById(string userId)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return conn.QueryFirstOrDefault<User>("SELECT * FROM User WHERE id = @userId", new { userId });
        }

        public List<string> GetRoleIdsForUser(string userId)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return conn.Query<string>("SELECT roleId FROM UserRole WHERE userId = @userId", new { userId }).ToList();
        }

        public User Create(string tenantId, string name, string username, string passwordHash)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                var id = Guid.NewGuid().ToString("N");
                var now = DateTime.UtcNow;
                conn.Execute(@"
                    INSERT INTO User (id, tenantId, name, username, passwordHash, isActive, createdAt, updatedAt)
                    VALUES (@id, @tenantId, @name, @username, @passwordHash, 1, @createdAt, @updatedAt)",
                    new { id, tenantId, name, username, passwordHash, createdAt = now, updatedAt = now });
                return conn.QueryFirstOrDefault<User>("SELECT * FROM User WHERE id = @userId", new { userId = id });
            }
        }

        public bool Update(string userId, string name, string username, bool isActive)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                return conn.Execute(@"
                    UPDATE User SET name = @name, username = @username, isActive = @isActive, updatedAt = @now
                    WHERE id = @userId",
                    new { userId, name, username, isActive = isActive ? 1 : 0, now = DateTime.UtcNow }) > 0;
            }
        }

        public bool UpdatePassword(string userId, string passwordHash)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                return conn.Execute(@"
                    UPDATE User SET passwordHash = @passwordHash, updatedAt = @now WHERE id = @userId",
                    new { userId, passwordHash, now = DateTime.UtcNow }) > 0;
            }
        }

        public bool ChangePassword(string userId, string passwordHash)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                return conn.Execute(@"
                    UPDATE User SET passwordHash = @passwordHash, mustChangePassword = 0, updatedAt = @now
                    WHERE id = @userId",
                    new { userId, passwordHash, now = DateTime.UtcNow }) > 0;
            }
        }
    }
}
