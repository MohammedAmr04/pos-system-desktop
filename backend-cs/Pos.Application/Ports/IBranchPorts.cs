using System.Collections.Generic;
using PosCs.Application.Models;
using PosCs.Domain.Entities;

namespace PosCs.Application.Ports
{
    public interface IBranchRepository
    {
        BranchRuntimeContext GetRuntimeContext();
        List<Branch> GetAll(bool activeOnly);
        Branch GetById(string id);
        void SaveRuntimeContext(string branchId, string terminalId, string nodeRole, string centralBaseUrl);
    }

    public interface ISyncRepository
    {
        List<SyncOperation> GetPending(int limit);
        SyncStatus GetStatus();
        void MarkAttempt(string id, string error);
        void MarkSynced(string id);
        void Receive(SyncOperation operation);
        void ApplyReceived(string operationId);
        List<SyncChange> GetChanges(long afterVersion, int limit);
    }
}
