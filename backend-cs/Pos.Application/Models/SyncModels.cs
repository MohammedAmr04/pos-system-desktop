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
        public string Status { get; set; }
    }

    public sealed class BranchAgentConfig
    {
        public string CentralBaseUrl { get; set; }
        public string CentralToken { get; set; }
        public string BranchId { get; set; }
        public string BranchKey { get; set; }
        public string NodeRole { get; set; }
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
        public int Dead { get; set; }
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

    public sealed class SyncApplyOutcome
    {
        public bool Applied { get; set; }
        public string Error { get; set; }

        public static SyncApplyOutcome Success()
        {
            return new SyncApplyOutcome { Applied = true };
        }

        public static SyncApplyOutcome Failure(string error)
        {
            return new SyncApplyOutcome { Applied = false, Error = error };
        }
    }

    public sealed class SyncOperationFailure
    {
        public string Id { get; set; }
        public string Error { get; set; }
    }

    public sealed class SyncReceiveResult
    {
        public int Applied { get; set; }
        public List<SyncOperationFailure> Failed { get; set; } = new List<SyncOperationFailure>();
        public bool Success { get { return Failed.Count == 0; } }
    }

    public sealed class BranchRuntimeConfigurationRequest
    {
        public string BranchId { get; set; }
        public string TerminalId { get; set; }
        public string NodeRole { get; set; }
        public string CentralBaseUrl { get; set; }
    }

    public sealed class BranchSecretRequest
    {
        public string Key { get; set; }
    }
}
