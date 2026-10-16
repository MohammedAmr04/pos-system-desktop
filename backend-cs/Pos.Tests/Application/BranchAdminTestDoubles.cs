using System;
using System.Collections.Generic;
using System.Linq;
using PosCs.Application.Models;
using PosCs.Application.Ports;
using PosCs.Domain.Entities;

namespace PosCs.Tests.Application
{
    internal sealed class FakeBranchRepo : IBranchRepository
    {
        public List<Branch> Branches = new List<Branch>();
        public List<Terminal> Terminals = new List<Terminal>();
        public Dictionary<string, string> SecretHashes = new Dictionary<string, string>();
        public BranchRuntimeContext RuntimeContext;
        public BranchAgentConfig AgentConfig = new BranchAgentConfig();
        public string RuntimeStatus;
        public int TerminalCount;
        public bool HasReferences;
        public List<string> AppliedSwitches = new List<string>();

        public BranchRuntimeContext GetRuntimeContext() { return RuntimeContext; }

        public List<Branch> GetAll(bool activeOnly) =>
            Branches.Where(b => !activeOnly || b.IsActive).ToList();

        public PagedResult<Branch> GetPaged(int page, int pageSize, string query, bool activeOnly) =>
            new PagedResult<Branch> { Items = GetAll(activeOnly), Total = GetAll(activeOnly).Count };

        public Branch GetById(string id) => Branches.Find(b => b.Id == id);
        public Branch GetByCode(string code) => Branches.Find(b => b.Code == code);

        public Branch Create(Branch branch)
        {
            if (string.IsNullOrEmpty(branch.Id)) branch.Id = Guid.NewGuid().ToString("N");
            Branches.Add(branch);
            return branch;
        }

        public Branch Update(Branch branch) { return branch; }
        public void SetActive(string id, bool isActive)
        {
            var b = GetById(id);
            if (b != null) b.IsActive = isActive;
        }
        public bool Delete(string id) => Branches.RemoveAll(b => b.Id == id) > 0;
        public int CountTerminals(string branchId) => TerminalCount;
        public bool HasTransactionReferences(string branchId) => HasReferences;
        public List<Terminal> GetTerminals(string branchId) =>
            Terminals.Where(t => t.BranchId == branchId).ToList();

        public void SaveRuntimeContext(string branchId, string terminalId, string nodeRole, string centralBaseUrl)
        {
            RuntimeContext = new BranchRuntimeContext
            {
                Branch = GetById(branchId),
                Terminal = new Terminal { Id = terminalId },
                NodeRole = nodeRole,
                CentralBaseUrl = centralBaseUrl
            };
        }

        public void SaveRuntimeCredentials(string centralToken, string centralBranchKey)
        {
            AgentConfig.CentralToken = centralToken;
            AgentConfig.BranchKey = centralBranchKey;
        }

        public BranchAgentConfig GetAgentConfig() { return AgentConfig; }
        public string GetRuntimeStatus() { return RuntimeStatus; }
        public void SetRuntimeStatus(string status) { RuntimeStatus = status; }
        public void ApplySwitch(string branchId, string terminalId, string centralBranchKey)
        {
            AppliedSwitches.Add(branchId);
            if (RuntimeContext != null)
            {
                RuntimeContext.Branch = GetById(branchId);
                RuntimeContext.Terminal = new Terminal { Id = terminalId };
            }
            AgentConfig.BranchId = branchId;
            AgentConfig.BranchKey = centralBranchKey;
        }

        public string GetSecretHash(string branchId)
        {
            string hash;
            return SecretHashes.TryGetValue(branchId, out hash) ? hash : null;
        }

        public void SetSecretHash(string branchId, string secretHash) { SecretHashes[branchId] = secretHash; }
    }

    internal sealed class FakeTerminalRepo : ITerminalRepository
    {
        public List<Terminal> Terminals = new List<Terminal>();
        public List<string> BranchSets = new List<string>();
        public List<string> ActiveSets = new List<string>();

        public List<Terminal> GetPaged(int page, int pageSize, string branchId, bool activeOnly, out int total)
        {
            var all = Filter(branchId, activeOnly);
            total = all.Count;
            return all.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        }

        public List<Terminal> GetAll(string branchId, bool activeOnly) => Filter(branchId, activeOnly);

        private List<Terminal> Filter(string branchId, bool activeOnly) =>
            Terminals.Where(t => (string.IsNullOrWhiteSpace(branchId) || t.BranchId == branchId) && (!activeOnly || t.IsActive)).ToList();

        public Terminal GetById(string id) => Terminals.Find(t => t.Id == id);

        public Terminal Create(Terminal terminal)
        {
            if (string.IsNullOrEmpty(terminal.Id)) terminal.Id = Guid.NewGuid().ToString("N");
            Terminals.Add(terminal);
            return terminal;
        }

        public Terminal Update(Terminal terminal) { return terminal; }
        public void SetActive(string id, bool isActive)
        {
            ActiveSets.Add(id + ":" + isActive);
            var t = GetById(id);
            if (t != null) t.IsActive = isActive;
        }
        public void SetBranch(string id, string branchId)
        {
            BranchSets.Add(id + ":" + branchId);
            var t = GetById(id);
            if (t != null) t.BranchId = branchId;
        }
        public void TouchLastSeen(string id) { }
        public int CountTransactionReferences(string terminalId) => 0;
    }

    internal sealed class FakeAssignmentRepo : IBranchAssignmentRepository
    {
        public List<BranchAssignmentHistory> Entries = new List<BranchAssignmentHistory>();

        public BranchAssignmentHistory GetByRequestId(string requestId) =>
            Entries.Find(e => e.RequestId == requestId);

        public BranchAssignmentHistory Create(BranchAssignmentHistory entry)
        {
            if (string.IsNullOrEmpty(entry.Id)) entry.Id = Guid.NewGuid().ToString("N");
            Entries.Add(entry);
            return entry;
        }

        public List<BranchAssignmentHistory> GetByTerminal(string terminalId, int limit) =>
            Entries.Where(e => e.TerminalId == terminalId).Take(limit).ToList();
    }

    internal sealed class FakeSyncRepo : ISyncRepository
    {
        public SyncStatus Status = new SyncStatus();
        public List<string> ReceivedOps = new List<string>();

        public List<SyncOperation> GetPending(int limit) => new List<SyncOperation>();
        public SyncStatus GetStatus() => Status;
        public void MarkAttempt(string id, string error) { }
        public void MarkSynced(string id) { }
        public void Receive(SyncOperation operation) { ReceivedOps.Add(operation.Id); }
        public SyncApplyOutcome ApplyReceived(string operationId) => SyncApplyOutcome.Success();
        public List<SyncChange> GetChanges(long afterVersion, int limit, string branchId = null) => new List<SyncChange>();
    }

    internal sealed class FakeSwitchClient : IBranchSwitchClient
    {
        public BranchAssignmentResult NextResult;
        public string LastRequestId;
        public BranchSwitchRequest LastRequest;

        public BranchAssignmentResult RequestCentralSwitch(BranchAgentConfig config, string currentBranchId, BranchSwitchRequest request)
        {
            LastRequest = request;
            LastRequestId = request.RequestId;
            return NextResult;
        }

        public BranchAssignmentResult GetCentralAssignment(BranchAgentConfig config, string requestId)
        {
            LastRequestId = requestId;
            return NextResult;
        }
    }
}
