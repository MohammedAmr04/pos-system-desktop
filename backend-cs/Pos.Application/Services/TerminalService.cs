using System.Collections.Generic;
using PosCs.Application.Models;
using PosCs.Application.Ports;
using PosCs.Domain.Entities;
using PosCs.Domain.Exceptions;

namespace PosCs.Application.Services
{
    public class TerminalService
    {
        private readonly ITerminalRepository _terminals;
        private readonly IBranchRepository _branches;
        private readonly IBranchAssignmentRepository _history;

        public TerminalService(ITerminalRepository terminals, IBranchRepository branches, IBranchAssignmentRepository history)
        {
            _terminals = terminals;
            _branches = branches;
            _history = history;
        }

        public PagedResult<Terminal> GetPaged(int page, int pageSize, string branchId, bool activeOnly)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 200) pageSize = 200;
            int total;
            var items = _terminals.GetPaged(page, pageSize, branchId, activeOnly, out total);
            return new PagedResult<Terminal> { Items = items, Total = total };
        }

        public Terminal GetById(string id)
        {
            var terminal = _terminals.GetById(id);
            if (terminal == null) throw new NotFoundException("Terminal not found");
            return terminal;
        }

        public List<BranchAssignmentHistory> GetHistory(string id, int limit)
        {
            if (limit < 1) limit = 20;
            if (limit > 200) limit = 200;
            return _history.GetByTerminal(id, limit);
        }

        public Terminal Create(CreateTerminalRequest request)
        {
            if (request == null) throw new DomainValidationException("Invalid terminal");
            var name = request.Name == null ? null : request.Name.Trim();
            if (string.IsNullOrWhiteSpace(name)) throw new DomainValidationException("Terminal name is required");
            var branch = RequireActiveBranch(request.BranchId);

            var terminal = new Terminal
            {
                BranchId = branch.Id,
                Name = name,
                Notes = request.Notes == null ? null : request.Notes.Trim(),
                IsActive = true
            };
            return _terminals.Create(terminal);
        }

        public Terminal Update(string id, UpdateTerminalRequest request)
        {
            var terminal = _terminals.GetById(id);
            if (terminal == null) throw new NotFoundException("Terminal not found");
            if (request == null) throw new DomainValidationException("Invalid terminal");

            var name = request.Name == null ? null : request.Name.Trim();
            if (string.IsNullOrWhiteSpace(name)) throw new DomainValidationException("Terminal name is required");

            if (!string.IsNullOrWhiteSpace(request.BranchId))
            {
                var branch = RequireActiveBranch(request.BranchId);
                terminal.BranchId = branch.Id;
            }

            terminal.Name = name;
            terminal.Notes = request.Notes == null ? null : request.Notes.Trim();
            return _terminals.Update(terminal);
        }

        public void SetActive(string id, bool isActive)
        {
            var terminal = _terminals.GetById(id);
            if (terminal == null) throw new NotFoundException("Terminal not found");
            _terminals.SetActive(id, isActive);
        }

        private Branch RequireActiveBranch(string branchId)
        {
            if (string.IsNullOrWhiteSpace(branchId)) throw new DomainValidationException("Branch is required");
            var branch = _branches.GetById(branchId.Trim());
            if (branch == null) throw new NotFoundException("Branch not found");
            if (!branch.IsActive) throw new DomainValidationException("Target branch is disabled");
            return branch;
        }
    }
}
