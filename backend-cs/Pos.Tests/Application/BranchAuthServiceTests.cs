using System.Collections.Generic;
using PosCs.Application.Models;
using PosCs.Application.Ports;
using PosCs.Application.Services;
using PosCs.Domain.Entities;
using PosCs.Domain.Exceptions;
using PosCs.Infrastructure.Security;
using Xunit;

namespace PosCs.Tests.Application
{
    public class BranchAuthServiceTests
    {
        [Fact]
        public void VerifyKeyReturnsNotConfiguredWhenNoSecretEnrolled()
        {
            var repo = new FakeBranchRepository();
            repo.Branches.Add(new Branch { Id = "b1" });
            var service = new BranchAuthService(repo, new PasswordHasher());

            Assert.Equal(BranchSecretState.NotConfigured, service.VerifyKey("b1", null));
        }

        [Fact]
        public void VerifyKeyAcceptsEnrolledSecretAndRejectsWrongOrUnknownBranch()
        {
            var repo = new FakeBranchRepository();
            repo.Branches.Add(new Branch { Id = "b1" });
            var service = new BranchAuthService(repo, new PasswordHasher());
            service.SetSecret("b1", "correct-horse-battery");

            Assert.Equal(BranchSecretState.Valid, service.VerifyKey("b1", "correct-horse-battery"));
            Assert.Equal(BranchSecretState.Invalid, service.VerifyKey("b1", "wrong-key"));
            Assert.Equal(BranchSecretState.Invalid, service.VerifyKey("missing", "correct-horse-battery"));
        }

        [Fact]
        public void SetSecretRejectsShortKeyAndUnknownBranch()
        {
            var repo = new FakeBranchRepository();
            repo.Branches.Add(new Branch { Id = "b1" });
            var service = new BranchAuthService(repo, new PasswordHasher());

            Assert.Throws<DomainValidationException>(() => service.SetSecret("b1", "short"));
            Assert.Throws<NotFoundException>(() => service.SetSecret("missing", "long-enough-secret-value"));
        }

        [Fact]
        public void EnsureOperationsBelongToBranchRejectsCrossBranchPush()
        {
            var service = new BranchAuthService(new FakeBranchRepository(), new PasswordHasher());
            var operations = new List<SyncOperation>
            {
                new SyncOperation { Id = "op-1", BranchId = "b1" },
                new SyncOperation { Id = "op-2", BranchId = "b2" }
            };

            Assert.Throws<DomainValidationException>(() => service.EnsureOperationsBelongToBranch(operations, "b1"));
        }

        [Fact]
        public void EnsureOperationsBelongToBranchAllowsMatchingOrUnenrolled()
        {
            var service = new BranchAuthService(new FakeBranchRepository(), new PasswordHasher());
            var operations = new List<SyncOperation> { new SyncOperation { Id = "op-1", BranchId = "b2" } };

            service.EnsureOperationsBelongToBranch(operations, null);
            service.EnsureOperationsBelongToBranch(operations, "b2");
        }

        private sealed class FakeBranchRepository : IBranchRepository
        {
            public List<Branch> Branches { get; } = new List<Branch>();
            private string _secretHash;

            public BranchRuntimeContext GetRuntimeContext() { return null; }
            public List<Branch> GetAll(bool activeOnly) { return Branches; }
            public PosCs.Application.Models.PagedResult<Branch> GetPaged(int page, int pageSize, string query, bool activeOnly) =>
                new PosCs.Application.Models.PagedResult<Branch> { Items = Branches, Total = Branches.Count };
            public Branch GetById(string id) { return Branches.Find(b => b.Id == id); }
            public Branch GetByCode(string code) { return Branches.Find(b => b.Code == code); }
            public Branch Create(Branch branch) { Branches.Add(branch); return branch; }
            public Branch Update(Branch branch) { return branch; }
            public void SetActive(string id, bool isActive) { }
            public bool Delete(string id) { return false; }
            public int CountTerminals(string branchId) { return 0; }
            public bool HasTransactionReferences(string branchId) { return false; }
            public List<Terminal> GetTerminals(string branchId) { return new List<Terminal>(); }
            public void SaveRuntimeContext(string branchId, string terminalId, string nodeRole, string centralBaseUrl) { }
            public void SaveRuntimeCredentials(string centralToken, string centralBranchKey) { }
            public BranchAgentConfig GetAgentConfig() { return new BranchAgentConfig(); }
            public string GetRuntimeStatus() { return null; }
            public void SetRuntimeStatus(string status) { }
            public void ApplySwitch(string branchId, string terminalId, string centralBranchKey) { }
            public string GetSecretHash(string branchId) { return _secretHash; }
            public void SetSecretHash(string branchId, string secretHash) { _secretHash = secretHash; }
        }
    }
}
