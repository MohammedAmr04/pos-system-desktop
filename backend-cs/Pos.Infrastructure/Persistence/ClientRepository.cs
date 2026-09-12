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

        /// <summary>
        /// Balance mirrors ClientService.GetStatement exactly: posted invoices minus
        /// all received payments (cash-sale auto payments cancel their own invoice).
        /// Rounded to 2 decimals so the zero filter is not tripped by float noise.
        /// </summary>
        private const string BalanceExpr =
            "ROUND(COALESCE((SELECT SUM(totalAmount) FROM Invoice WHERE clientId = c.id AND status = 'posted'), 0) " +
            "- COALESCE((SELECT SUM(amount) FROM Payment WHERE clientId = c.id), 0), 2)";

        public PagedResult<Client> GetPaged(int page, int pageSize, string query, string balanceFilter)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                var clauses = new List<string>();
                var parameters = new DynamicParameters();
                parameters.Add("pageSize", pageSize);
                parameters.Add("offset", (page - 1) * pageSize);

                if (!string.IsNullOrWhiteSpace(query))
                {
                    clauses.Add("(c.name LIKE @like ESCAPE '\\' OR c.phone LIKE @like ESCAPE '\\')");
                    parameters.Add("like", $"%{EscapeLike(query)}%");
                }

                if (balanceFilter == "positive")
                    clauses.Add($"({BalanceExpr}) > 0");
                else if (balanceFilter == "negative")
                    clauses.Add($"({BalanceExpr}) < 0");
                else if (balanceFilter == "zero")
                    clauses.Add($"({BalanceExpr}) = 0");

                var where = clauses.Count > 0 ? " WHERE " + string.Join(" AND ", clauses) : "";
                var total = conn.ExecuteScalar<int>($"SELECT COUNT(1) FROM Client c{where}", parameters);
                var items = conn.Query<Client>(
                    $"SELECT c.*, ({BalanceExpr}) AS Balance FROM Client c{where} " +
                    "ORDER BY c.name COLLATE NOCASE LIMIT @pageSize OFFSET @offset",
                    parameters).ToList();
                return new PagedResult<Client> { Items = items, Total = total };
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
