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

            public List<SyncOperation> GetPending(int limit)
            {
                LastLimit = limit;
                return new List<SyncOperation>();
            }

            public SyncStatus GetStatus() { return new SyncStatus(); }
            public void MarkAttempt(string id, string error) { }
            public void MarkSynced(string id) { }
            public void Receive(SyncOperation operation) { Received.Add(operation); }
            public void ApplyReceived(string operationId) { }
            public List<SyncChange> GetChanges(long afterVersion, int limit) { return new List<SyncChange>(); }
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
