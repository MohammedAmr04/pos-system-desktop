using System;
using System.Collections.Generic;
using PosCs.Application.Models;
using PosCs.Application.Ports;
using PosCs.Domain.Entities;
using PosCs.Domain.Exceptions;

namespace PosCs.Application.Services
{
    /// <summary>
    /// Safe terminal branch switching. On the central node it validates and records an authorized
    /// assignment; on a branch node it drains the outbox, asks central to accept the switch, and
    /// only then persists the new local runtime context. Historical transactions are never touched.
    /// </summary>
    public class BranchAssignmentService
    {
        private readonly IBranchRepository _branches;
        private readonly ITerminalRepository _terminals;
        private readonly IBranchAssignmentRepository _history;
        private readonly ISyncRepository _sync;
        private readonly IBranchSwitchClient _client;
        private readonly BranchAuthService _branchAuth;

        public BranchAssignmentService(
            IBranchRepository branches,
            ITerminalRepository terminals,
            IBranchAssignmentRepository history,
            ISyncRepository sync,
            IBranchSwitchClient client,
            BranchAuthService branchAuth)
        {
            _branches = branches;
            _terminals = terminals;
            _history = history;
            _sync = sync;
            _client = client;
            _branchAuth = branchAuth;
        }

        /// <summary>Central-side: validate an admin-authorized switch, record it idempotently by
        /// requestId, and update the terminal's authoritative branch assignment.</summary>
        public BranchAssignmentResult CentralSwitch(string authenticatedBranchId, string changedBy, BranchSwitchRequest request)
        {
            Validate(request);
            var requestId = string.IsNullOrWhiteSpace(request.RequestId) ? Guid.NewGuid().ToString("N") : request.RequestId.Trim();

            var existing = _history.GetByRequestId(requestId);
            if (existing != null)
                return BuildResult(existing);

            var target = _branches.GetById(request.TargetBranchId.Trim());
            if (target == null) throw new NotFoundException("Target branch not found");
            if (!target.IsActive) throw new DomainValidationException("Target branch is disabled");

            var terminal = _terminals.GetById(request.TerminalId.Trim());
            if (terminal == null) throw new NotFoundException("Terminal not found");
            if (!terminal.IsActive) throw new DomainValidationException("Terminal is disabled");

            var keyState = _branchAuth.VerifyKey(target.Id, request.TargetBranchKey);
            if (keyState == BranchSecretState.Invalid)
                throw new DomainValidationException("Invalid key for the target branch");

            var fromBranchId = string.IsNullOrWhiteSpace(authenticatedBranchId) ? terminal.BranchId : authenticatedBranchId;
            if (string.Equals(fromBranchId, target.Id, StringComparison.Ordinal))
                throw new DomainValidationException("Terminal is already assigned to this branch");

            var entry = _history.Create(new BranchAssignmentHistory
            {
                TerminalId = terminal.Id,
                FromBranchId = fromBranchId,
                ToBranchId = target.Id,
                ChangedBy = changedBy,
                NodeRole = "central",
                RequestId = requestId,
                Outcome = "accepted"
            });
            _terminals.SetBranch(terminal.Id, target.Id);
            return BuildResult(entry);
        }

        /// <summary>Branch-side orchestration: drain, then switch. Fails closed and keeps the
        /// previous branch if anything goes wrong.</summary>
        public BranchAssignmentResult LocalSwitch(string changedBy, BranchSwitchRequest request)
        {
            Validate(request);
            var requestId = string.IsNullOrWhiteSpace(request.RequestId) ? Guid.NewGuid().ToString("N") : request.RequestId.Trim();

            var context = _branches.GetRuntimeContext();
            if (context == null || context.Branch == null)
                throw new DomainValidationException("Branch runtime is not configured");
            var targetId = request.TargetBranchId.Trim();
            if (string.Equals(context.Branch.Id, targetId, StringComparison.Ordinal))
                throw new DomainValidationException("Terminal is already assigned to this branch");

            EnsureOutboxDrained();

            var terminalId = string.IsNullOrWhiteSpace(request.TerminalId)
                ? (context.Terminal == null ? null : context.Terminal.Id)
                : request.TerminalId.Trim();

            var agent = _branches.GetAgentConfig();
            _branches.SetRuntimeStatus("switching");
            try
            {
                request.RequestId = requestId;
                request.TerminalId = terminalId;
                var result = _client.RequestCentralSwitch(agent, context.Branch.Id, request);
                if (result == null || !string.Equals(result.Outcome, "accepted", StringComparison.OrdinalIgnoreCase))
                    throw new DomainValidationException("Central did not confirm the branch switch");

                _branches.ApplySwitch(result.BranchId, terminalId, request.TargetBranchKey);
                return result;
            }
            catch
            {
                _branches.SetRuntimeStatus("active");
                throw;
            }
        }

        /// <summary>Recovery for a lost switch response: fetch the authoritative assignment from
        /// central and reconcile the local runtime config if central already accepted it.</summary>
        public BranchAssignmentResult Reconcile(string requestId)
        {
            if (string.IsNullOrWhiteSpace(requestId))
                throw new DomainValidationException("Switch request id is required");

            var agent = _branches.GetAgentConfig();
            var result = _client.GetCentralAssignment(agent, requestId.Trim());
            if (result == null) throw new NotFoundException("Switch request not found");

            _branches.SetRuntimeStatus("active");
            return result;
        }

        public BranchAssignmentResult GetCentralAssignment(string requestId)
        {
            if (string.IsNullOrWhiteSpace(requestId))
                throw new DomainValidationException("Switch request id is required");
            var entry = _history.GetByRequestId(requestId.Trim());
            if (entry == null) throw new NotFoundException("Switch request not found");
            return BuildResult(entry);
        }

        public List<BranchAssignmentHistory> History(string terminalId, int limit)
        {
            if (string.IsNullOrWhiteSpace(terminalId)) throw new DomainValidationException("Terminal is required");
            if (limit < 1) limit = 20;
            if (limit > 200) limit = 200;
            return _history.GetByTerminal(terminalId.Trim(), limit);
        }

        private void EnsureOutboxDrained()
        {
            var status = _sync.GetStatus();
            var unresolved = status.Pending + status.Failed + status.Dead;
            if (unresolved > 0)
                throw new DomainValidationException(
                    $"Cannot switch branches while {unresolved} sync operation(s) are still pending");
        }

        private void Validate(BranchSwitchRequest request)
        {
            if (request == null) throw new DomainValidationException("Invalid branch switch request");
            if (string.IsNullOrWhiteSpace(request.TargetBranchId))
                throw new DomainValidationException("Target branch is required");
        }

        private BranchAssignmentResult BuildResult(BranchAssignmentHistory entry)
        {
            var toBranch = _branches.GetById(entry.ToBranchId);
            return new BranchAssignmentResult
            {
                RequestId = entry.RequestId,
                TerminalId = entry.TerminalId,
                BranchId = entry.ToBranchId,
                BranchCode = toBranch == null ? null : toBranch.Code,
                BranchName = toBranch == null ? null : toBranch.Name,
                FromBranchId = entry.FromBranchId,
                Outcome = entry.Outcome
            };
        }
    }
}
