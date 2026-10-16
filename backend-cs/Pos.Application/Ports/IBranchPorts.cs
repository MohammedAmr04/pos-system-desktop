using System.Collections.Generic;
using PosCs.Application.Models;
using PosCs.Domain.Entities;

namespace PosCs.Application.Ports
{
    public interface IBranchRepository
    {
        BranchRuntimeContext GetRuntimeContext();
        List<Branch> GetAll(bool activeOnly);
        PagedResult<Branch> GetPaged(int page, int pageSize, string query, bool activeOnly);
        Branch GetById(string id);
        Branch GetByCode(string code);
        Branch Create(Branch branch);
        Branch Update(Branch branch);
        void SetActive(string id, bool isActive);
        bool Delete(string id);
        int CountTerminals(string branchId);
        bool HasTransactionReferences(string branchId);
        List<Terminal> GetTerminals(string branchId);
        void SaveRuntimeContext(string branchId, string terminalId, string nodeRole, string centralBaseUrl);
        void SaveRuntimeCredentials(string centralToken, string centralBranchKey);
        BranchAgentConfig GetAgentConfig();
        string GetRuntimeStatus();
        void SetRuntimeStatus(string status);
        void ApplySwitch(string branchId, string terminalId, string centralBranchKey);
        string GetSecretHash(string branchId);
        void SetSecretHash(string branchId, string secretHash);
    }

    public interface ITerminalRepository
    {
        List<Terminal> GetPaged(int page, int pageSize, string branchId, bool activeOnly, out int total);
        List<Terminal> GetAll(string branchId, bool activeOnly);
        Terminal GetById(string id);
        Terminal Create(Terminal terminal);
        Terminal Update(Terminal terminal);
        void SetActive(string id, bool isActive);
        void SetBranch(string id, string branchId);
        void TouchLastSeen(string id);
        int CountTransactionReferences(string terminalId);
    }

    public interface IBranchAssignmentRepository
    {
        BranchAssignmentHistory GetByRequestId(string requestId);
        BranchAssignmentHistory Create(BranchAssignmentHistory entry);
        List<BranchAssignmentHistory> GetByTerminal(string terminalId, int limit);
    }

    /// <summary>Outbound call from a branch node to the central node to request a terminal branch
    /// switch. Implemented over HTTPS in Infrastructure.</summary>
    public interface IBranchSwitchClient
    {
        BranchAssignmentResult RequestCentralSwitch(BranchAgentConfig config, string currentBranchId, BranchSwitchRequest request);
        BranchAssignmentResult GetCentralAssignment(BranchAgentConfig config, string requestId);
    }

    public interface ISyncRepository
    {
        List<SyncOperation> GetPending(int limit);
        SyncStatus GetStatus();
        void MarkAttempt(string id, string error);
        void MarkSynced(string id);
        void Receive(SyncOperation operation);
        SyncApplyOutcome ApplyReceived(string operationId);
        List<SyncChange> GetChanges(long afterVersion, int limit, string branchId = null);
    }
}
