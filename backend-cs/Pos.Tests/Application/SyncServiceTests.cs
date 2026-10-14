using System;
using System.Collections.Generic;
using PosCs.Application.Models;
using PosCs.Application.Ports;
using PosCs.Application.Services;
using Xunit;

namespace PosCs.Tests.Application
{
    public class SyncServiceTests
    {
        [Fact]
        public void GetPendingClampsBatchSizeToSafeUpperBound()
        {
            var repo = new FakeSyncRepository();
            var service = new SyncService(repo);

            service.GetPending(1000);

            Assert.Equal(100, repo.LastLimit);
        }

        [Fact]
        public void ReceiveForwardsOperationsToRepository()
        {
            var repo = new FakeSyncRepository();
            var service = new SyncService(repo);
            var operation = new SyncOperation { Id = "operation-1" };

            service.Receive(new[] { operation, operation });

            Assert.Equal(2, repo.Received.Count);
        }

        [Fact]
        public void ReceiveReportsFailedAppliesSoCallersDoNotAcknowledge()
        {
            var repo = new FakeSyncRepository();
            repo.FailIds.Add("operation-2");
            var service = new SyncService(repo);

            var result = service.Receive(new[]
            {
                new SyncOperation { Id = "operation-1" },
                new SyncOperation { Id = "operation-2" },
                new SyncOperation { Id = "operation-3" }
            });

            Assert.Equal(2, result.Applied);
            Assert.False(result.Success);
            var failure = Assert.Single(result.Failed);
            Assert.Equal("operation-2", failure.Id);
            Assert.Equal("apply failed", failure.Error);
        }

        [Fact]
        public void ReceiveTreatsUnknownApplyOutcomeAsFailure()
        {
            var repo = new FakeSyncRepository { ReturnNullOutcome = true };
            var service = new SyncService(repo);

            var result = service.Receive(new[] { new SyncOperation { Id = "operation-1" } });

            Assert.False(result.Success);
            Assert.Equal(0, result.Applied);
            Assert.Single(result.Failed);
        }

        [Fact]
        public void BranchServiceRejectsUnknownNodeRole()
        {
            var repo = new FakeBranchRepository();
            var service = new BranchService(repo);

            Assert.Throws<PosCs.Domain.Exceptions.DomainValidationException>(() =>
                service.Configure("branch-1", "terminal-1", "worker", null));
        }

        private sealed class FakeSyncRepository : ISyncRepository
        {
            public int LastLimit { get; private set; }
            public List<SyncOperation> Received { get; } = new List<SyncOperation>();
            public HashSet<string> FailIds { get; } = new HashSet<string>();
            public bool ReturnNullOutcome { get; set; }

            public List<SyncOperation> GetPending(int limit)
            {
                LastLimit = limit;
                return new List<SyncOperation>();
            }

            public SyncStatus GetStatus() { return new SyncStatus(); }
            public void MarkAttempt(string id, string error) { }
            public void MarkSynced(string id) { }
            public void Receive(SyncOperation operation) { Received.Add(operation); }
            public SyncApplyOutcome ApplyReceived(string operationId)
            {
                if (ReturnNullOutcome) return null;
                return FailIds.Contains(operationId)
                    ? SyncApplyOutcome.Failure("apply failed")
                    : SyncApplyOutcome.Success();
            }
            public List<SyncChange> GetChanges(long afterVersion, int limit, string branchId = null) { return new List<SyncChange>(); }
        }

        private sealed class FakeBranchRepository : IBranchRepository
        {
            public BranchRuntimeContext GetRuntimeContext() { return null; }
            public List<PosCs.Domain.Entities.Branch> GetAll(bool activeOnly) { return new List<PosCs.Domain.Entities.Branch>(); }
            public PosCs.Domain.Entities.Branch GetById(string id) { return null; }
            public void SaveRuntimeContext(string branchId, string terminalId, string nodeRole, string centralBaseUrl) { }
        }
    }
}
