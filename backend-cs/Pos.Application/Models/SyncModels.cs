using System;
using System.Collections.Generic;
using PosCs.Domain.Entities;

namespace PosCs.Application.Models
{
    public sealed class BranchRuntimeContext
    {
        public Branch Branch { get; set; }
        public Terminal Terminal { get; set; }
        public string NodeRole { get; set; }
        public string CentralBaseUrl { get; set; }
    }

    public sealed class SyncOperation
    {
        public string Id { get; set; }
        public string BranchId { get; set; }
        public string TerminalId { get; set; }
        public string OperationType { get; set; }
        public string EntityType { get; set; }
        public string EntityId { get; set; }
        public string Payload { get; set; }
        public DateTime OccurredAt { get; set; }
        public string Status { get; set; }
        public int Attempts { get; set; }
        public string LastError { get; set; }
        public DateTime? SyncedAt { get; set; }
    }

    public sealed class SyncStatus
    {
        public int Pending { get; set; }
        public int Failed { get; set; }
        public DateTime? LastSyncedAt { get; set; }
        public DateTime? LastOccurredAt { get; set; }
    }

    public sealed class SyncChange
    {
        public long Version { get; set; }
        public string BranchId { get; set; }
        public string EntityType { get; set; }
        public string EntityId { get; set; }
        public string OperationType { get; set; }
        public string Payload { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public sealed class SyncPushRequest
    {
        public List<SyncOperation> Operations { get; set; } = new List<SyncOperation>();
    }

    public sealed class BranchRuntimeConfigurationRequest
    {
        public string BranchId { get; set; }
        public string TerminalId { get; set; }
        public string NodeRole { get; set; }
        public string CentralBaseUrl { get; set; }
    }
}
