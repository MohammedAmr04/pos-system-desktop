using PosCs.Application.Models;
using PosCs.Application.Services;
using PosCs.Domain.Entities;
using PosCs.Domain.Exceptions;
using Xunit;

namespace PosCs.Tests.Application
{
    public class TerminalServiceTests
    {
        private static TerminalService Make(FakeTerminalRepo terminals, FakeBranchRepo branches) =>
            new TerminalService(terminals, branches, new FakeAssignmentRepo());

        [Fact]
        public void Create_RequiresActiveBranch()
        {
            var terminals = new FakeTerminalRepo();
            var branches = new FakeBranchRepo();
            branches.Branches.Add(new Branch { Id = "b1", Code = "br-1", Name = "Main", IsActive = false });

            var ex = Assert.Throws<DomainValidationException>(() =>
                Make(terminals, branches).Create(new CreateTerminalRequest { BranchId = "b1", Name = "Till 1" }));
            Assert.Contains("disabled", ex.Message);
        }

        [Fact]
        public void Create_TrimsAndAssignsBranch()
        {
            var terminals = new FakeTerminalRepo();
            var branches = new FakeBranchRepo();
            branches.Branches.Add(new Branch { Id = "b1", Code = "br-1", Name = "Main", IsActive = true });

            var terminal = Make(terminals, branches).Create(
                new CreateTerminalRequest { BranchId = " b1 ", Name = " Till 1 ", Notes = " front " });

            Assert.Equal("b1", terminal.BranchId);
            Assert.Equal("Till 1", terminal.Name);
            Assert.Equal("front", terminal.Notes);
            Assert.True(terminal.IsActive);
        }

        [Fact]
        public void Create_RejectsMissingName()
        {
            var terminals = new FakeTerminalRepo();
            var branches = new FakeBranchRepo();
            branches.Branches.Add(new Branch { Id = "b1", Code = "br-1", Name = "Main", IsActive = true });

            Assert.Throws<DomainValidationException>(() =>
                Make(terminals, branches).Create(new CreateTerminalRequest { BranchId = "b1", Name = " " }));
        }

        [Fact]
        public void Update_ReassignsToActiveBranch()
        {
            var terminals = new FakeTerminalRepo();
            var branches = new FakeBranchRepo();
            branches.Branches.Add(new Branch { Id = "b1", Code = "br-1", Name = "Main", IsActive = true });
            branches.Branches.Add(new Branch { Id = "b2", Code = "br-2", Name = "Second", IsActive = true });
            terminals.Terminals.Add(new Terminal { Id = "t1", BranchId = "b1", Name = "Till 1", IsActive = true });

            var updated = Make(terminals, branches).Update("t1",
                new UpdateTerminalRequest { Name = "Till 1", BranchId = "b2" });

            Assert.Equal("b2", updated.BranchId);
        }

        [Fact]
        public void Update_ThrowsWhenTerminalMissing()
        {
            var terminals = new FakeTerminalRepo();
            var branches = new FakeBranchRepo();

            Assert.Throws<NotFoundException>(() => Make(terminals, branches).Update("nope",
                new UpdateTerminalRequest { Name = "x" }));
        }

        [Fact]
        public void GetPaged_FiltersByBranch()
        {
            var terminals = new FakeTerminalRepo();
            var branches = new FakeBranchRepo();
            terminals.Terminals.Add(new Terminal { Id = "t1", BranchId = "b1", Name = "A", IsActive = true });
            terminals.Terminals.Add(new Terminal { Id = "t2", BranchId = "b2", Name = "B", IsActive = true });

            var page = Make(terminals, branches).GetPaged(1, 20, "b1", false);
            Assert.Single(page.Items);
            Assert.Equal("t1", page.Items[0].Id);
        }

        [Fact]
        public void SetActive_TogglesTerminal()
        {
            var terminals = new FakeTerminalRepo();
            var branches = new FakeBranchRepo();
            terminals.Terminals.Add(new Terminal { Id = "t1", BranchId = "b1", Name = "A", IsActive = true });

            Make(terminals, branches).SetActive("t1", false);
            Assert.False(terminals.GetById("t1").IsActive);
        }
    }
}
