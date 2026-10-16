using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using PosCs.Application.Ports;
using PosCs.Domain.Entities;

namespace PosCs.Infrastructure.Persistence
{
    public class TerminalRepository : ITerminalRepository
    {
        public List<Terminal> GetPaged(int page, int pageSize, string branchId, bool activeOnly, out int total)
        {
            using (var connection = DbConnectionFactory.CreateConnection())
            {
                var where = "WHERE 1=1";
                if (activeOnly) where += " AND isActive=1";
                object parameters;
                if (string.IsNullOrWhiteSpace(branchId))
                {
                    parameters = new { pageSize, offset = (page - 1) * pageSize };
                }
                else
                {
                    where += " AND branchId=@branchId";
                    parameters = new { branchId, pageSize, offset = (page - 1) * pageSize };
                }

                total = connection.ExecuteScalar<int>($"SELECT COUNT(1) FROM Terminal {where}", parameters);
                return connection.Query<Terminal>(
                    $"SELECT * FROM Terminal {where} ORDER BY name COLLATE NOCASE LIMIT @pageSize OFFSET @offset",
                    parameters).ToList();
            }
        }

        public List<Terminal> GetAll(string branchId, bool activeOnly)
        {
            using (var connection = DbConnectionFactory.CreateConnection())
            {
                var where = "WHERE 1=1";
                if (activeOnly) where += " AND isActive=1";
                object parameters;
                if (string.IsNullOrWhiteSpace(branchId))
                {
                    parameters = null;
                }
                else
                {
                    where += " AND branchId=@branchId";
                    parameters = new { branchId };
                }
                return connection.Query<Terminal>(
                    $"SELECT * FROM Terminal {where} ORDER BY name COLLATE NOCASE", parameters).ToList();
            }
        }

        public Terminal GetById(string id)
        {
            using (var connection = DbConnectionFactory.CreateConnection())
                return connection.QueryFirstOrDefault<Terminal>("SELECT * FROM Terminal WHERE id=@id", new { id });
        }

        public Terminal Create(Terminal terminal)
        {
            using (var connection = DbConnectionFactory.CreateConnection())
            {
                var now = DateTime.Now;
                if (string.IsNullOrWhiteSpace(terminal.Id)) terminal.Id = Guid.NewGuid().ToString("N");
                terminal.CreatedAt = now;
                terminal.UpdatedAt = now;
                connection.Execute(@"INSERT INTO Terminal (id,branchId,name,isActive,notes,createdAt,updatedAt)
                    VALUES (@id,@branchId,@name,@isActive,@notes,@createdAt,@updatedAt)",
                    new
                    {
                        id = terminal.Id,
                        branchId = terminal.BranchId,
                        name = terminal.Name,
                        isActive = terminal.IsActive ? 1 : 0,
                        notes = terminal.Notes,
                        createdAt = terminal.CreatedAt,
                        updatedAt = terminal.UpdatedAt
                    });
                return terminal;
            }
        }

        public Terminal Update(Terminal terminal)
        {
            using (var connection = DbConnectionFactory.CreateConnection())
            {
                terminal.UpdatedAt = DateTime.Now;
                connection.Execute(@"UPDATE Terminal SET name=@name, notes=@notes, branchId=@branchId, updatedAt=@updatedAt
                    WHERE id=@id",
                    new
                    {
                        id = terminal.Id,
                        name = terminal.Name,
                        notes = terminal.Notes,
                        branchId = terminal.BranchId,
                        updatedAt = terminal.UpdatedAt
                    });
                return terminal;
            }
        }

        public void SetActive(string id, bool isActive)
        {
            using (var connection = DbConnectionFactory.CreateConnection())
                connection.Execute("UPDATE Terminal SET isActive=@isActive, updatedAt=@updatedAt WHERE id=@id",
                    new { id, isActive = isActive ? 1 : 0, updatedAt = DateTime.Now });
        }

        public void SetBranch(string id, string branchId)
        {
            using (var connection = DbConnectionFactory.CreateConnection())
                connection.Execute("UPDATE Terminal SET branchId=@branchId, updatedAt=@updatedAt WHERE id=@id",
                    new { id, branchId, updatedAt = DateTime.Now });
        }

        public void TouchLastSeen(string id)
        {
            using (var connection = DbConnectionFactory.CreateConnection())
                connection.Execute("UPDATE Terminal SET lastSeenAt=@now WHERE id=@id", new { id, now = DateTime.Now });
        }

        public int CountTransactionReferences(string terminalId)
        {
            using (var connection = DbConnectionFactory.CreateConnection())
            {
                const string sql = @"
                    SELECT
                        (SELECT COUNT(1) FROM Invoice WHERE terminalId=@terminalId) +
                        (SELECT COUNT(1) FROM Payment WHERE terminalId=@terminalId) +
                        (SELECT COUNT(1) FROM Shift WHERE terminalId=@terminalId) +
                        (SELECT COUNT(1) FROM Expense WHERE terminalId=@terminalId) +
                        (SELECT COUNT(1) FROM StockMovement WHERE terminalId=@terminalId)";
                return connection.ExecuteScalar<int>(sql, new { terminalId });
            }
        }
    }
}
