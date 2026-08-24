using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using PosCs.Application.Models;
using PosCs.Application.Ports;
using PosCs.Domain.Entities;

namespace PosCs.Infrastructure.Persistence
{
    public class ClientRepository : IClientRepository
    {
        private const string Columns = "id, name, phone, address, notes, isActive, createdAt, updatedAt";

        public List<Client> GetAll()
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return conn.Query<Client>("SELECT * FROM Client ORDER BY name COLLATE NOCASE").ToList();
        }

        public PagedResult<Client> GetPaged(int page, int pageSize, string query)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    var total = conn.ExecuteScalar<int>("SELECT COUNT(1) FROM Client");
                    var items = conn.Query<Client>(
                        "SELECT * FROM Client ORDER BY name COLLATE NOCASE LIMIT @pageSize OFFSET @offset",
                        new { pageSize, offset = (page - 1) * pageSize }).ToList();
                    return new PagedResult<Client> { Items = items, Total = total };
                }

                var like = $"%{EscapeLike(query)}%";
                var totalFiltered = conn.ExecuteScalar<int>(
                    "SELECT COUNT(1) FROM Client WHERE name LIKE @like ESCAPE '\\' OR phone LIKE @like ESCAPE '\\'",
                    new { like });
                var filteredItems = conn.Query<Client>(
                    "SELECT * FROM Client WHERE name LIKE @like ESCAPE '\\' OR phone LIKE @like ESCAPE '\\' " +
                    "ORDER BY name COLLATE NOCASE LIMIT @pageSize OFFSET @offset",
                    new { like, pageSize, offset = (page - 1) * pageSize }).ToList();
                return new PagedResult<Client> { Items = filteredItems, Total = totalFiltered };
            }
        }

        public Client GetById(string id)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return conn.QueryFirstOrDefault<Client>("SELECT * FROM Client WHERE id = @id", new { id });
        }

        public Client Create(Client client)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                client.Id = Guid.NewGuid().ToString("N");
                var now = DateTime.Now;
                client.CreatedAt = now;
                client.UpdatedAt = now;
                conn.Execute($@"
                    INSERT INTO Client ({Columns})
                    VALUES (@id, @name, @phone, @address, @notes, @isActive, @createdAt, @updatedAt)",
                    new
                    {
                        id = client.Id,
                        name = client.Name,
                        phone = client.Phone,
                        address = client.Address,
                        notes = client.Notes,
                        isActive = client.IsActive ? 1 : 0,
                        createdAt = client.CreatedAt,
                        updatedAt = client.UpdatedAt
                    });
                return client;
            }
        }

        public Client Update(Client client)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                client.UpdatedAt = DateTime.Now;
                conn.Execute(@"
                    UPDATE Client SET name = @name, phone = @phone, address = @address,
                        notes = @notes, isActive = @isActive, updatedAt = @updatedAt
                    WHERE id = @id",
                    new
                    {
                        id = client.Id,
                        name = client.Name,
                        phone = client.Phone,
                        address = client.Address,
                        notes = client.Notes,
                        isActive = client.IsActive ? 1 : 0,
                        updatedAt = client.UpdatedAt
                    });
                return client;
            }
        }

        private static string EscapeLike(string input)
        {
            return input.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
        }
    }
}
