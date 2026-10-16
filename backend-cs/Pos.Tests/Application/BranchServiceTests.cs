using System;
using PosCs.Application.Models;
using PosCs.Application.Services;
using PosCs.Domain.Entities;
using PosCs.Domain.Exceptions;
using Xunit;

namespace PosCs.Tests.Application
{
    public class BranchServiceTests
    {
        private static BranchService Make(FakeBranchRepo repo) => new BranchService(repo);

        [Fact]
        public void Create_TrimsAndPersistsActiveBranch()
        {
            var repo = new FakeBranchRepo();
            var branch = Make(repo).Create(new CreateBranchRequest { Code = " br-1 ", Name = " Main ", Address = " here " });

            Assert.Equal("br-1", branch.Code);
            Assert.Equal("Main", branch.Name);
            Assert.Equal("here", branch.Address);
            Assert.True(branch.IsActive);
            Assert.Single(repo.Branches);
        }

        [Fact]
        public void Create_RejectsDuplicateCode()
        {
            var repo = new FakeBranchRepo();
            repo.Branches.Add(new Branch { Id = "b1", Code = "br-1", Name = "Main" });

            Assert.Throws<DomainValidationException>(() =>
                Make(repo).Create(new CreateBranchRequest { Code = "br-1", Name = "Dup" }));
        }

        [Theory]
        [InlineData("", "Main")]
        [InlineData("br-1", "")]
        public void Create_RequiresCodeAndName(string code, string name)
        {
            var repo = new FakeBranchRepo();
            Assert.Throws<DomainValidationException>(() =>
                Make(repo).Create(new CreateBranchRequest { Code = code, Name = name }));
        }

        [Fact]
        public void SetActive_BlocksDisablingBranchWithTerminals()
        {
            var repo = new FakeBranchRepo();
            repo.Branches.Add(new Branch { Id = "b1", Code = "br-1", Name = "Main", IsActive = true });
            repo.TerminalCount = 2;

            Assert.Throws<DomainValidationException>(() => Make(repo).SetActive("b1", false));
            Assert.True(repo.GetById("b1").IsActive);
        }

        [Fact]
        public void SetActive_AllowsEnablingBranchWithTerminals()
        {
            var repo = new FakeBranchRepo();
            repo.Branches.Add(new Branch { Id = "b1", Code = "br-1", Name = "Main", IsActive = false });
            repo.TerminalCount = 2;

            Make(repo).SetActive("b1", true);
            Assert.True(repo.GetById("b1").IsActive);
        }

        [Fact]
        public void Delete_BlocksWhenReferencedByTransactions()
        {
            var repo = new FakeBranchRepo();
            repo.Branches.Add(new Branch { Id = "b1", Code = "br-1", Name = "Main" });
            repo.HasReferences = true;

            Assert.Throws<DomainValidationException>(() => Make(repo).Delete("b1"));
            Assert.Single(repo.Branches);
        }

        [Fact]
        public void Delete_RemovesUnusedBranch()
        {
            var repo = new FakeBranchRepo();
            repo.Branches.Add(new Branch { Id = "b1", Code = "br-1", Name = "Main" });

            Make(repo).Delete("b1");
            Assert.Empty(repo.Branches);
        }

        [Fact]
        public void GetRuntimeContext_ThrowsWhenIncomplete()
        {
            var repo = new FakeBranchRepo { RuntimeContext = new BranchRuntimeContext() };
            Assert.Throws<DomainValidationException>(() => Make(repo).GetRuntimeContext());
        }

        [Theory]
        [InlineData("branch")]
        [InlineData("central")]
        public void Configure_AcceptsKnownRoles(string role)
        {
            var repo = new FakeBranchRepo();
            repo.Branches.Add(new Branch { Id = "b1", Code = "br-1", Name = "Main" });
            Make(repo).Configure("b1", "t1", role, "https://central");
            Assert.NotNull(repo.RuntimeContext);
            Assert.Equal(role, repo.RuntimeContext.NodeRole);
        }

        [Fact]
        public void Configure_RejectsUnknownRole()
        {
            var repo = new FakeBranchRepo();
            Assert.Throws<DomainValidationException>(() => Make(repo).Configure("b1", "t1", "peer", null));
        }

        [Fact]
        public void IsSwitching_ReflectsRuntimeStatus()
        {
            var repo = new FakeBranchRepo { RuntimeStatus = "SWITCHING" };
            Assert.True(Make(repo).IsSwitching());
        }
    }
}
