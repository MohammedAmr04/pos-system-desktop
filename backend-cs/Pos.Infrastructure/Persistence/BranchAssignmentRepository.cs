using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using PosCs.Application.Ports;
using PosCs.Domain.Entities;

namespace PosCs.Infrastructure.Persistence
{
    public class BranchAssignmentRepository : IBranchAssignmentRepository
    {
        public BranchAssignmentHistory GetByRequestId(string requestId)
        {
            using (var connection = DbConnectionFactory.CreateConnection())
                return connection.QueryFirstOrDefault<BranchAssignmentHistory>(
                    "SELECT * FROM BranchAssignmentHistory WHERE requestId=@requestId", new { requestId });
        }

        public BranchAssignmentHistory Create(BranchAssignmentHistory entry)
        {
            using (var connection = DbConnectionFactory.CreateConnection())
            {
                if (string.IsNullOrWhiteSpace(entry.Id)) entry.Id = Guid.NewGuid().ToString("N");
                entry.CreatedAt = DateTime.Now;
                connection.Execute(@"INSERT INTO BranchAssignmentHistory
                    (id,terminalId,fromBranchId,toBranchId,changedBy,nodeRole,requestId,outcome,createdAt)
                    VALUES (@id,@terminalId,@fromBranchId,@toBranchId,@changedBy,@nodeRole,@requestId,@outcome,@createdAt)",
                    new
                    {
                        id = entry.Id,
                        terminalId = entry.TerminalId,
                        fromBranchId = entry.FromBranchId,
                        toBranchId = entry.ToBranchId,
                        changedBy = entry.ChangedBy,
                        nodeRole = entry.NodeRole,
                        requestId = entry.RequestId,
                        outcome = entry.Outcome,
                        createdAt = entry.CreatedAt
                    });
                return entry;
            }
        }

        public List<BranchAssignmentHistory> GetByTerminal(string terminalId, int limit)
        {
            using (var connection = DbConnectionFactory.CreateConnection())
                return connection.Query<BranchAssignmentHistory>(
                    "SELECT * FROM BranchAssignmentHistory WHERE terminalId=@terminalId ORDER BY createdAt DESC LIMIT @limit",
                    new { terminalId, limit }).ToList();
        }
    }
}
