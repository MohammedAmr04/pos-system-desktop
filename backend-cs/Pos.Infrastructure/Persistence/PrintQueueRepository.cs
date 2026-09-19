using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using PosCs.Application.Models;
using PosCs.Application.Ports;

namespace PosCs.Infrastructure.Persistence
{
    public class PrintQueueRepository : IPrintQueueRepository
    {
        public void Enqueue(string jobType, string payload)
        {
            using (var connection = DbConnectionFactory.CreateConnection())
            {
                var context = connection.QueryFirstOrDefault<RuntimeRow>("SELECT branchId,terminalId FROM BranchRuntimeConfig WHERE id=1");
                if (context == null) throw new InvalidOperationException("Branch runtime configuration is missing");
                connection.Execute(@"INSERT INTO PrintQueue (id,branchId,terminalId,jobType,payload,createdAt,status)
                    VALUES (@id,@branchId,@terminalId,@jobType,@payload,@createdAt,'pending')", new
                {
                    id = Guid.NewGuid().ToString("N"), context.BranchId, context.TerminalId, jobType, payload, createdAt = DateTime.Now
                });
            }
        }

        public List<PrintJob> GetPending(int limit)
        {
            using (var connection = DbConnectionFactory.CreateConnection())
                return connection.Query<PrintJob>("SELECT * FROM PrintQueue WHERE status IN ('pending','failed') ORDER BY createdAt LIMIT @limit", new { limit }).ToList();
        }

        public void MarkAttempt(string id, string error)
        {
            using (var connection = DbConnectionFactory.CreateConnection())
                connection.Execute("UPDATE PrintQueue SET status='failed',attempts=attempts+1,lastError=@error WHERE id=@id", new { id, error });
        }

        public void MarkPrinted(string id)
        {
            using (var connection = DbConnectionFactory.CreateConnection())
                connection.Execute("UPDATE PrintQueue SET status='printed',printedAt=@at,lastError=NULL WHERE id=@id", new { id, at = DateTime.Now });
        }

        public PrintQueueStatus GetStatus()
        {
            using (var connection = DbConnectionFactory.CreateConnection())
                return new PrintQueueStatus
                {
                    Pending = connection.ExecuteScalar<int>("SELECT COUNT(1) FROM PrintQueue WHERE status='pending'"),
                    Failed = connection.ExecuteScalar<int>("SELECT COUNT(1) FROM PrintQueue WHERE status='failed'")
                };
        }

        private sealed class RuntimeRow
        {
            public string BranchId { get; set; }
            public string TerminalId { get; set; }
        }
    }
}
