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
                var config = connection.QueryFirstOrDefault<RuntimeRow>("SELECT branchId,terminalId,nodeRole,centralBaseUrl FROM BranchRuntimeConfig WHERE id=1");
                if (config == null) return null;
                var branch = connection.QueryFirstOrDefault<Branch>("SELECT * FROM Branch WHERE id=@id", new { id = config.BranchId });
                var terminal = connection.QueryFirstOrDefault<Terminal>("SELECT * FROM Terminal WHERE id=@id AND branchId=@branchId", new { id = config.TerminalId, branchId = config.BranchId });
                return new BranchRuntimeContext { Branch = branch, Terminal = terminal, NodeRole = config.NodeRole, CentralBaseUrl = config.CentralBaseUrl };
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

        public Branch GetById(string id)
        {
            using (var connection = DbConnectionFactory.CreateConnection())
                return connection.QueryFirstOrDefault<Branch>("SELECT * FROM Branch WHERE id=@id", new { id });
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

        private sealed class RuntimeRow
        {
            public string BranchId { get; set; }
            public string TerminalId { get; set; }
            public string NodeRole { get; set; }
            public string CentralBaseUrl { get; set; }
        }
    }
}
