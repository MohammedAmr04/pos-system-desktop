using System.Collections.Generic;
using PosCs.Application.Models;
using PosCs.Application.Ports;

namespace PosCs.Application.Services
{
    public class SyncService
    {
        private readonly ISyncRepository _repository;

        public SyncService(ISyncRepository repository)
        {
            _repository = repository;
        }

        public List<SyncOperation> GetPending(int limit)
        {
            if (limit < 1) limit = 1;
            if (limit > 100) limit = 100;
            return _repository.GetPending(limit);
        }

        public SyncStatus GetStatus()
        {
            return _repository.GetStatus();
        }

        public void MarkAttempt(string id, string error)
        {
            _repository.MarkAttempt(id, error);
        }

        public void MarkSynced(string id)
        {
            _repository.MarkSynced(id);
        }

        public SyncReceiveResult Receive(IEnumerable<SyncOperation> operations)
        {
            var result = new SyncReceiveResult();
            if (operations == null) return result;
            foreach (var operation in operations)
            {
                if (operation == null || string.IsNullOrWhiteSpace(operation.Id)) continue;
                _repository.Receive(operation);
                var outcome = _repository.ApplyReceived(operation.Id);
                if (outcome != null && outcome.Applied)
                {
                    result.Applied++;
                }
                else
                {
                    result.Failed.Add(new SyncOperationFailure
                    {
                        Id = operation.Id,
                        Error = outcome == null ? "Unknown apply failure" : outcome.Error
                    });
                }
            }
            return result;
        }

        public List<SyncChange> GetChanges(long afterVersion, int limit, string branchId = null)
        {
            if (afterVersion < 0) afterVersion = 0;
            if (limit < 1) limit = 1;
            if (limit > 100) limit = 100;
            return _repository.GetChanges(afterVersion, limit, branchId);
        }
    }
}
