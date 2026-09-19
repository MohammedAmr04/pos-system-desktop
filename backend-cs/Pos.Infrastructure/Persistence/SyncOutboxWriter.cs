using System;
using Dapper;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using PosCs.Application.Models;

namespace PosCs.Infrastructure.Persistence
{
    public static class SyncOutboxWriter
    {
        public static void Enqueue(SqliteConnection connection, SqliteTransaction transaction,
            string operationType, string entityType, string entityId, object payload)
        {
            var context = connection.QueryFirstOrDefault<RuntimeRow>(
                "SELECT branchId,terminalId,nodeRole FROM BranchRuntimeConfig WHERE id=1", transaction: transaction);
            if (context == null || !string.Equals(context.NodeRole, "branch", StringComparison.OrdinalIgnoreCase)) return;

            connection.Execute(@"INSERT OR IGNORE INTO SyncOutbox
                (id,branchId,terminalId,operationType,entityType,entityId,payload,occurredAt,status)
                VALUES (@id,@branchId,@terminalId,@operationType,@entityType,@entityId,@payload,@occurredAt,'pending')", new
            {
                id = Guid.NewGuid().ToString("N"),
                branchId = context.BranchId,
                terminalId = context.TerminalId,
                operationType,
                entityType,
                entityId,
                payload = payload == null ? null : JsonConvert.SerializeObject(payload),
                occurredAt = DateTime.Now
            }, transaction);
        }

        private sealed class RuntimeRow
        {
            public string BranchId { get; set; }
            public string TerminalId { get; set; }
            public string NodeRole { get; set; }
        }
    }
}
