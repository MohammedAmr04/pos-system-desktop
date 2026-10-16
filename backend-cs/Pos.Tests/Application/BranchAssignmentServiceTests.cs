using System;
using PosCs.Application.Models;
using PosCs.Application.Services;
using PosCs.Domain.Entities;
using PosCs.Domain.Exceptions;
using PosCs.Infrastructure.Security;
using Xunit;

namespace PosCs.Tests.Application
{
    public class BranchAssignmentServiceTests
    {
        private const string TargetKey = "target-key-1234567890";

        private static BranchAssignmentService Make(
            FakeBranchRepo branches,
            FakeTerminalRepo terminals,
            FakeAssignmentRepo history,
            FakeSyncRepo sync,
            PosCs.Application.Ports.IBranchSwitchClient client,
            BranchAuthService auth)
        {
            return new BranchAssignmentService(branches, terminals, history, sync, client, auth);
        }

        private static void SeedCentral(FakeBranchRepo branches, FakeTerminalRepo terminals, bool enrollKey)
        {
            branches.Branches.Add(new Branch { Id = "b1", Code = "br-1", Name = "Main", IsActive = true });
            branches.Branches.Add(new Branch { Id = "b2", Code = "br-2", Name = "Second", IsActive = true });
            terminals.Terminals.Add(new Terminal { Id = "t1", BranchId = "b1", Name = "Till 1", IsActive = true });
            if (enrollKey)
                branches.SetSecretHash("b2", new PasswordHasher().Hash(TargetKey));
        }

        private static BranchAuthService Auth(FakeBranchRepo branches) => new BranchAuthService(branches, new PasswordHasher());

        [Fact]
        public void CentralSwitch_AcceptsAndReassignsWhenTargetHasNoSecret()
        {
            var branches = new FakeBranchRepo();
            var terminals = new FakeTerminalRepo();
            var history = new FakeAssignmentRepo();
            SeedCentral(branches, terminals, enrollKey: false);
            var service = Make(branches, terminals, history, new FakeSyncRepo(), new FakeSwitchClient(), Auth(branches));

            var result = service.CentralSwitch("b1", "admin", new BranchSwitchRequest
            {
                RequestId = "req-1",
                TerminalId = "t1",
                TargetBranchId = "b2"
            });

            Assert.Equal("accepted", result.Outcome);
            Assert.Equal("b2", result.BranchId);
            Assert.Equal("br-2", result.BranchCode);
            Assert.Contains("t1:b2", terminals.BranchSets);
            Assert.Single(history.Entries);
        }

        [Fact]
        public void CentralSwitch_RejectsInvalidKeyWhenTargetSecretEnrolled()
        {
            var branches = new FakeBranchRepo();
            var terminals = new FakeTerminalRepo();
            SeedCentral(branches, terminals, enrollKey: true);
            var service = Make(branches, terminals, new FakeAssignmentRepo(), new FakeSyncRepo(), new FakeSwitchClient(), Auth(branches));

            Assert.Throws<DomainValidationException>(() => service.CentralSwitch("b1", "admin", new BranchSwitchRequest
            {
                RequestId = "req-1",
                TerminalId = "t1",
                TargetBranchId = "b2",
                TargetBranchKey = "wrong-key-0000000000"
            }));
            Assert.Empty(terminals.BranchSets);
        }

        [Fact]
        public void CentralSwitch_AcceptsCorrectKeyWhenTargetSecretEnrolled()
        {
            var branches = new FakeBranchRepo();
            var terminals = new FakeTerminalRepo();
            SeedCentral(branches, terminals, enrollKey: true);
            var service = Make(branches, terminals, new FakeAssignmentRepo(), new FakeSyncRepo(), new FakeSwitchClient(), Auth(branches));

            var result = service.CentralSwitch("b1", "admin", new BranchSwitchRequest
            {
                RequestId = "req-1",
                TerminalId = "t1",
                TargetBranchId = "b2",
                TargetBranchKey = TargetKey
            });

            Assert.Equal("b2", result.BranchId);
        }

        [Fact]
        public void CentralSwitch_IsIdempotentByRequestId()
        {
            var branches = new FakeBranchRepo();
            var terminals = new FakeTerminalRepo();
            var history = new FakeAssignmentRepo();
            SeedCentral(branches, terminals, enrollKey: false);
            var service = Make(branches, terminals, history, new FakeSyncRepo(), new FakeSwitchClient(), Auth(branches));

            var request = new BranchSwitchRequest { RequestId = "req-1", TerminalId = "t1", TargetBranchId = "b2" };
            service.CentralSwitch("b1", "admin", request);
            service.CentralSwitch("b1", "admin", request);

            Assert.Single(history.Entries);
            Assert.Single(terminals.BranchSets);
        }

        [Fact]
        public void CentralSwitch_RejectsSameBranch()
        {
            var branches = new FakeBranchRepo();
            var terminals = new FakeTerminalRepo();
            SeedCentral(branches, terminals, enrollKey: false);
            var service = Make(branches, terminals, new FakeAssignmentRepo(), new FakeSyncRepo(), new FakeSwitchClient(), Auth(branches));

            Assert.Throws<DomainValidationException>(() => service.CentralSwitch("b2", "admin", new BranchSwitchRequest
            {
                RequestId = "req-1",
                TerminalId = "t1",
                TargetBranchId = "b2"
            }));
        }

        [Fact]
        public void CentralSwitch_RejectsDisabledTarget()
        {
            var branches = new FakeBranchRepo();
            var terminals = new FakeTerminalRepo();
            SeedCentral(branches, terminals, enrollKey: false);
            branches.GetById("b2").IsActive = false;
            var service = Make(branches, terminals, new FakeAssignmentRepo(), new FakeSyncRepo(), new FakeSwitchClient(), Auth(branches));

            Assert.Throws<DomainValidationException>(() => service.CentralSwitch("b1", "admin", new BranchSwitchRequest
            {
                RequestId = "req-1",
                TerminalId = "t1",
                TargetBranchId = "b2"
            }));
        }

        [Fact]
        public void LocalSwitch_BlocksWhenOutboxNotDrained()
        {
            var branches = new FakeBranchRepo();
            var terminals = new FakeTerminalRepo();
            SeedCentral(branches, terminals, enrollKey: false);
            branches.RuntimeContext = new BranchRuntimeContext { Branch = branches.GetById("b1"), Terminal = terminals.GetById("t1") };
            var sync = new FakeSyncRepo { Status = new SyncStatus { Pending = 1 } };
            var service = Make(branches, terminals, new FakeAssignmentRepo(), sync, new FakeSwitchClient(), Auth(branches));

            Assert.Throws<DomainValidationException>(() => service.LocalSwitch("admin", new BranchSwitchRequest
            {
                RequestId = "req-1",
                TargetBranchId = "b2"
            }));
            Assert.Null(branches.RuntimeStatus);
        }

        [Fact]
        public void LocalSwitch_AppliesWhenCentralAccepts()
        {
            var branches = new FakeBranchRepo();
            var terminals = new FakeTerminalRepo();
            SeedCentral(branches, terminals, enrollKey: false);
            branches.RuntimeContext = new BranchRuntimeContext { Branch = branches.GetById("b1"), Terminal = terminals.GetById("t1") };
            branches.AgentConfig = new BranchAgentConfig { BranchId = "b1" };
            var client = new FakeSwitchClient
            {
                NextResult = new BranchAssignmentResult { RequestId = "req-1", BranchId = "b2", Outcome = "accepted" }
            };
            var service = Make(branches, terminals, new FakeAssignmentRepo(), new FakeSyncRepo(), client, Auth(branches));

            var result = service.LocalSwitch("admin", new BranchSwitchRequest { RequestId = "req-1", TargetBranchId = "b2" });

            Assert.Equal("b2", result.BranchId);
            Assert.Contains("b2", branches.AppliedSwitches);
            Assert.Equal("t1", client.LastRequest.TerminalId);
            Assert.Equal("req-1", client.LastRequestId);
        }

        [Fact]
        public void LocalSwitch_KeepsPreviousBranchWhenCentralRejects()
        {
            var branches = new FakeBranchRepo();
            var terminals = new FakeTerminalRepo();
            SeedCentral(branches, terminals, enrollKey: false);
            branches.RuntimeContext = new BranchRuntimeContext { Branch = branches.GetById("b1"), Terminal = terminals.GetById("t1") };
            var client = new FakeSwitchClient
            {
                NextResult = new BranchAssignmentResult { RequestId = "req-1", BranchId = "b2", Outcome = "rejected" }
            };
            var service = Make(branches, terminals, new FakeAssignmentRepo(), new FakeSyncRepo(), client, Auth(branches));

            Assert.Throws<DomainValidationException>(() =>
                service.LocalSwitch("admin", new BranchSwitchRequest { RequestId = "req-1", TargetBranchId = "b2" }));

            Assert.Empty(branches.AppliedSwitches);
            Assert.Equal("active", branches.RuntimeStatus);
        }

        [Fact]
        public void LocalSwitch_ResetsStatusWhenClientThrows()
        {
            var branches = new FakeBranchRepo();
            var terminals = new FakeTerminalRepo();
            SeedCentral(branches, terminals, enrollKey: false);
            branches.RuntimeContext = new BranchRuntimeContext { Branch = branches.GetById("b1"), Terminal = terminals.GetById("t1") };
            var service = Make(branches, terminals, new FakeAssignmentRepo(), new FakeSyncRepo(), new ThrowingSwitchClient(), Auth(branches));

            Assert.Throws<InvalidOperationException>(() =>
                service.LocalSwitch("admin", new BranchSwitchRequest { RequestId = "req-1", TargetBranchId = "b2" }));
            Assert.Equal("active", branches.RuntimeStatus);
        }

        [Fact]
        public void LocalSwitch_RejectsSameBranchBeforeDrain()
        {
            var branches = new FakeBranchRepo();
            var terminals = new FakeTerminalRepo();
            SeedCentral(branches, terminals, enrollKey: false);
            branches.RuntimeContext = new BranchRuntimeContext { Branch = branches.GetById("b2"), Terminal = terminals.GetById("t1") };
            var sync = new FakeSyncRepo { Status = new SyncStatus { Pending = 5 } };
            var service = Make(branches, terminals, new FakeAssignmentRepo(), sync, new FakeSwitchClient(), Auth(branches));

            Assert.Throws<DomainValidationException>(() =>
                service.LocalSwitch("admin", new BranchSwitchRequest { RequestId = "req-1", TargetBranchId = "b2" }));
        }

        [Fact]
        public void Reconcile_ThrowsWhenRequestUnknown()
        {
            var branches = new FakeBranchRepo();
            var service = Make(branches, new FakeTerminalRepo(), new FakeAssignmentRepo(), new FakeSyncRepo(), new FakeSwitchClient(), Auth(branches));

            Assert.Throws<NotFoundException>(() => service.Reconcile("missing"));
        }

        [Fact]
        public void Reconcile_ReturnsCentralAssignmentAndClearsSwitching()
        {
            var branches = new FakeBranchRepo { RuntimeStatus = "switching" };
            var client = new FakeSwitchClient
            {
                NextResult = new BranchAssignmentResult { RequestId = "req-1", BranchId = "b2", Outcome = "accepted" }
            };
            var service = Make(branches, new FakeTerminalRepo(), new FakeAssignmentRepo(), new FakeSyncRepo(), client, Auth(branches));

            var result = service.Reconcile("req-1");
            Assert.Equal("b2", result.BranchId);
            Assert.Equal("active", branches.RuntimeStatus);
        }

        [Fact]
        public void GetCentralAssignment_ReturnsRecordedEntry()
        {
            var branches = new FakeBranchRepo();
            var terminals = new FakeTerminalRepo();
            var history = new FakeAssignmentRepo();
            SeedCentral(branches, terminals, enrollKey: false);
            var service = Make(branches, terminals, history, new FakeSyncRepo(), new FakeSwitchClient(), Auth(branches));
            service.CentralSwitch("b1", "admin", new BranchSwitchRequest { RequestId = "req-1", TerminalId = "t1", TargetBranchId = "b2" });

            var result = service.GetCentralAssignment("req-1");
            Assert.Equal("b2", result.BranchId);
            Assert.Equal("br-2", result.BranchCode);
        }

        private sealed class ThrowingSwitchClient : PosCs.Application.Ports.IBranchSwitchClient
        {
            public BranchAssignmentResult RequestCentralSwitch(BranchAgentConfig config, string currentBranchId, BranchSwitchRequest request) =>
                throw new InvalidOperationException("network down");

            public BranchAssignmentResult GetCentralAssignment(BranchAgentConfig config, string requestId) =>
                throw new InvalidOperationException("network down");
        }
    }
}
