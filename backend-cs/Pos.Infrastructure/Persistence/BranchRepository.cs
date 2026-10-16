using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using PosCs.Application.Models;
using PosCs.Application.Ports;
using PosCs.Domain.Entities;
using PosCs.Domain.Exceptions;

namespace PosCs.Infrastructure.Persistence
{
    public class BranchRepository : IBranchRepository
    {
        public BranchRuntimeContext GetRuntimeContext()
        {
            using (var connection = DbConnectionFactory.CreateConnection())
            {
                var config = connection.QueryFirstOrDefault<RuntimeRow>(
                    "SELECT branchId,terminalId,nodeRole,centralBaseUrl,status FROM BranchRuntimeConfig WHERE id=1");
                if (config == null) return null;
                var branch = connection.QueryFirstOrDefault<Branch>("SELECT * FROM Branch WHERE id=@id", new { id = config.BranchId });
                var terminal = connection.QueryFirstOrDefault<Terminal>("SELECT * FROM Terminal WHERE id=@id AND branchId=@branchId", new { id = config.TerminalId, branchId = config.BranchId });
                return new BranchRuntimeContext
                {
                    Branch = branch,
                    Terminal = terminal,
                    NodeRole = config.NodeRole,
                    CentralBaseUrl = config.CentralBaseUrl,
                    Status = string.IsNullOrWhiteSpace(config.Status) ? "active" : config.Status
                };
            }
        }

        public List<Branch> GetAll(bool activeOnly)
        {
            using (var connection = DbConnectionFactory.CreateConnection())
            {
                var sql = activeOnly ? "SELECT * FROM Branch WHERE isActive=1 ORDER BY name" : "SELECT * FROM Branch ORDER BY name";
                return connection.Query<Branch>(sql).ToList();
            }
        }

        public PagedResult<Branch> GetPaged(int page, int pageSize, string query, bool activeOnly)
        {
            using (var connection = DbConnectionFactory.CreateConnection())
            {
                var where = activeOnly ? "WHERE isActive=1" : "WHERE 1=1";
                object parameters;
                if (string.IsNullOrWhiteSpace(query))
                {
                    parameters = new { pageSize, offset = (page - 1) * pageSize };
                }
                else
                {
                    where += " AND (name LIKE @like ESCAPE '\\' OR code LIKE @like ESCAPE '\\')";
                    parameters = new { like = $"%{EscapeLike(query)}%", pageSize, offset = (page - 1) * pageSize };
                }

                var total = connection.ExecuteScalar<int>($"SELECT COUNT(1) FROM Branch {where}", parameters);
                var items = connection.Query<Branch>(
                    $"SELECT * FROM Branch {where} ORDER BY name COLLATE NOCASE LIMIT @pageSize OFFSET @offset",
                    parameters).ToList();
                return new PagedResult<Branch> { Items = items, Total = total };
            }
        }

        public Branch GetById(string id)
        {
            using (var connection = DbConnectionFactory.CreateConnection())
                return connection.QueryFirstOrDefault<Branch>("SELECT * FROM Branch WHERE id=@id", new { id });
        }

        public Branch GetByCode(string code)
        {
            using (var connection = DbConnectionFactory.CreateConnection())
                return connection.QueryFirstOrDefault<Branch>("SELECT * FROM Branch WHERE code=@code COLLATE NOCASE", new { code });
        }

        public Branch Create(Branch branch)
        {
            using (var connection = DbConnectionFactory.CreateConnection())
            {
                branch.Id = Guid.NewGuid().ToString("N");
                var now = DateTime.Now;
                branch.CreatedAt = now;
                branch.UpdatedAt = now;
                connection.Execute(@"INSERT INTO Branch (id,code,name,address,isActive,createdAt,updatedAt)
                    VALUES (@id,@code,@name,@address,@isActive,@createdAt,@updatedAt)",
                    new
                    {
                        id = branch.Id,
                        code = branch.Code,
                        name = branch.Name,
                        address = branch.Address,
                        isActive = branch.IsActive ? 1 : 0,
                        createdAt = branch.CreatedAt,
                        updatedAt = branch.UpdatedAt
                    });
                return branch;
            }
        }

        public Branch Update(Branch branch)
        {
            using (var connection = DbConnectionFactory.CreateConnection())
            {
                branch.UpdatedAt = DateTime.Now;
                connection.Execute(@"UPDATE Branch SET code=@code,name=@name,address=@address,updatedAt=@updatedAt
                    WHERE id=@id",
                    new
                    {
                        id = branch.Id,
                        code = branch.Code,
                        name = branch.Name,
                        address = branch.Address,
                        updatedAt = branch.UpdatedAt
                    });
                return branch;
            }
        }

        public void SetActive(string id, bool isActive)
        {
            using (var connection = DbConnectionFactory.CreateConnection())
                connection.Execute("UPDATE Branch SET isActive=@isActive, updatedAt=@updatedAt WHERE id=@id",
                    new { id, isActive = isActive ? 1 : 0, updatedAt = DateTime.Now });
        }

        public bool Delete(string id)
        {
            using (var connection = DbConnectionFactory.CreateConnection())
                return connection.Execute("DELETE FROM Branch WHERE id=@id", new { id }) > 0;
        }

        public int CountTerminals(string branchId)
        {
            using (var connection = DbConnectionFactory.CreateConnection())
                return connection.ExecuteScalar<int>("SELECT COUNT(1) FROM Terminal WHERE branchId=@branchId", new { branchId });
        }

        public bool HasTransactionReferences(string branchId)
        {
            using (var connection = DbConnectionFactory.CreateConnection())
            {
                const string sql = @"
                    SELECT
                        (SELECT COUNT(1) FROM Invoice WHERE branchId=@branchId) +
                        (SELECT COUNT(1) FROM Payment WHERE branchId=@branchId) +
                        (SELECT COUNT(1) FROM Shift WHERE branchId=@branchId) +
                        (SELECT COUNT(1) FROM Expense WHERE branchId=@branchId) +
                        (SELECT COUNT(1) FROM StockMovement WHERE branchId=@branchId) +
                        (SELECT COUNT(1) FROM SaleReturn WHERE branchId=@branchId) +
                        (SELECT COUNT(1) FROM PurchaseInvoice WHERE branchId=@branchId) +
                        (SELECT COUNT(1) FROM PurchaseReturn WHERE branchId=@branchId)";
                return connection.ExecuteScalar<int>(sql, new { branchId }) > 0;
            }
        }

        public List<Terminal> GetTerminals(string branchId)
        {
            using (var connection = DbConnectionFactory.CreateConnection())
                return connection.Query<Terminal>(
                    "SELECT * FROM Terminal WHERE branchId=@branchId ORDER BY name COLLATE NOCASE", new { branchId }).ToList();
        }

        public void SaveRuntimeContext(string branchId, string terminalId, string nodeRole, string centralBaseUrl)
        {
            using (var connection = DbConnectionFactory.CreateConnection())
            {
                var valid = connection.ExecuteScalar<int>("SELECT COUNT(1) FROM Terminal WHERE id=@terminalId AND branchId=@branchId AND isActive=1", new { terminalId, branchId });
                if (valid == 0) throw new NotFoundException("Terminal does not belong to the selected branch");
                connection.Execute(@"UPDATE BranchRuntimeConfig
                    SET branchId=@branchId, terminalId=@terminalId, nodeRole=@nodeRole,
                        centralBaseUrl=@centralBaseUrl, updatedAt=@updatedAt WHERE id=1",
                    new { branchId, terminalId, nodeRole, centralBaseUrl, updatedAt = DateTime.Now });
            }
        }

        public void SaveRuntimeCredentials(string centralToken, string centralBranchKey)
        {
            using (var connection = DbConnectionFactory.CreateConnection())
                connection.Execute(@"UPDATE BranchRuntimeConfig
                    SET centralToken=@centralToken, centralBranchKey=@centralBranchKey, updatedAt=@updatedAt WHERE id=1",
                    new { centralToken, centralBranchKey, updatedAt = DateTime.Now });
        }

        public BranchAgentConfig GetAgentConfig()
        {
            using (var connection = DbConnectionFactory.CreateConnection())
            {
                var config = connection.QueryFirstOrDefault<AgentRow>(
                    "SELECT branchId,nodeRole,centralBaseUrl,centralToken,centralBranchKey FROM BranchRuntimeConfig WHERE id=1");
                if (config == null) return null;
                return new BranchAgentConfig
                {
                    CentralBaseUrl = config.CentralBaseUrl,
                    CentralToken = config.CentralToken,
                    BranchId = config.BranchId,
                    BranchKey = config.CentralBranchKey,
                    NodeRole = config.NodeRole
                };
            }
        }

        public string GetRuntimeStatus()
        {
            using (var connection = DbConnectionFactory.CreateConnection())
                return connection.ExecuteScalar<string>("SELECT status FROM BranchRuntimeConfig WHERE id=1");
        }

        public void SetRuntimeStatus(string status)
        {
            using (var connection = DbConnectionFactory.CreateConnection())
                connection.Execute("UPDATE BranchRuntimeConfig SET status=@status, updatedAt=@updatedAt WHERE id=1",
                    new { status, updatedAt = DateTime.Now });
        }

        public void ApplySwitch(string branchId, string terminalId, string centralBranchKey)
        {
            using (var connection = DbConnectionFactory.CreateConnection())
            using (var tx = connection.BeginTransaction())
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(terminalId))
                        terminalId = connection.ExecuteScalar<string>("SELECT terminalId FROM BranchRuntimeConfig WHERE id=1", transaction: tx);

                    connection.Execute(@"UPDATE BranchRuntimeConfig
                        SET branchId=@branchId, terminalId=@terminalId, centralBranchKey=@centralBranchKey,
                            status='active', updatedAt=@updatedAt WHERE id=1",
                        new { branchId, terminalId, centralBranchKey, updatedAt = DateTime.Now }, transaction: tx);

                    connection.Execute("UPDATE Terminal SET branchId=@branchId, updatedAt=@updatedAt WHERE id=@terminalId",
                        new { branchId, terminalId, updatedAt = DateTime.Now }, transaction: tx);

                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        public string GetSecretHash(string branchId)
        {
            using (var connection = DbConnectionFactory.CreateConnection())
                return connection.ExecuteScalar<string>("SELECT secretHash FROM Branch WHERE id=@id", new { id = branchId });
        }

        public void SetSecretHash(string branchId, string secretHash)
        {
            using (var connection = DbConnectionFactory.CreateConnection())
                connection.Execute("UPDATE Branch SET secretHash=@secretHash, updatedAt=@updatedAt WHERE id=@id",
                    new { id = branchId, secretHash, updatedAt = DateTime.Now });
        }

        private static string EscapeLike(string input)
        {
            return input.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
        }

        private sealed class RuntimeRow
        {
            public string BranchId { get; set; }
            public string TerminalId { get; set; }
            public string NodeRole { get; set; }
            public string CentralBaseUrl { get; set; }
            public string Status { get; set; }
        }

        private sealed class AgentRow
        {
            public string BranchId { get; set; }
            public string NodeRole { get; set; }
            public string CentralBaseUrl { get; set; }
            public string CentralToken { get; set; }
            public string CentralBranchKey { get; set; }
        }
    }
}
