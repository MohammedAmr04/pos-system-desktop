using System.Collections.Generic;
using PosCs.Application.Models;
using PosCs.Application.Ports;
using PosCs.Domain.Entities;
using PosCs.Domain.Exceptions;

namespace PosCs.Application.Services
{
    public class BranchService
    {
        private readonly IBranchRepository _repository;
        private readonly IBranchEnrollmentClient _enrollmentClient;

        public BranchService(IBranchRepository repository, IBranchEnrollmentClient enrollmentClient)
        {
            _repository = repository;
            _enrollmentClient = enrollmentClient;
        }

        public BranchRuntimeContext GetRuntimeContext()
        {
            var context = _repository.GetRuntimeContext();
            if (context == null || context.Branch == null || context.Terminal == null)
                throw new DomainValidationException("Branch runtime configuration is incomplete");
            return context;
        }

        public bool IsSwitching()
        {
            return string.Equals(_repository.GetRuntimeStatus(), "switching", System.StringComparison.OrdinalIgnoreCase);
        }

        public List<Branch> GetAll(bool activeOnly)
        {
            return _repository.GetAll(activeOnly);
        }

        public PagedResult<Branch> GetPaged(int page, int pageSize, string query, bool activeOnly)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 200) pageSize = 200;
            return _repository.GetPaged(page, pageSize, query, activeOnly);
        }

        public Branch GetById(string id)
        {
            var branch = _repository.GetById(id);
            if (branch == null) throw new NotFoundException("Branch not found");
            return branch;
        }

        public List<Terminal> GetTerminals(string branchId)
        {
            if (string.IsNullOrWhiteSpace(branchId)) throw new DomainValidationException("Branch is required");
            return _repository.GetTerminals(branchId);
        }

        public Branch Create(CreateBranchRequest request)
        {
            if (request == null) throw new DomainValidationException("Invalid branch");
            var name = request.Name == null ? null : request.Name.Trim();
            var code = request.Code == null ? null : request.Code.Trim();
            if (string.IsNullOrWhiteSpace(name)) throw new DomainValidationException("Branch name is required");
            if (string.IsNullOrWhiteSpace(code)) throw new DomainValidationException("Branch code is required");
            if (_repository.GetByCode(code) != null) throw new DomainValidationException("A branch with this code already exists");

            var branch = new Branch
            {
                Code = code,
                Name = name,
                Address = request.Address == null ? null : request.Address.Trim(),
                IsActive = true
            };
            return _repository.Create(branch);
        }

        public Branch Update(string id, UpdateBranchRequest request)
        {
            var branch = _repository.GetById(id);
            if (branch == null) throw new NotFoundException("Branch not found");
            if (request == null) throw new DomainValidationException("Invalid branch");

            var name = request.Name == null ? null : request.Name.Trim();
            var code = request.Code == null ? null : request.Code.Trim();
            if (string.IsNullOrWhiteSpace(name)) throw new DomainValidationException("Branch name is required");
            if (string.IsNullOrWhiteSpace(code)) throw new DomainValidationException("Branch code is required");

            var existing = _repository.GetByCode(code);
            if (existing != null && !string.Equals(existing.Id, id, System.StringComparison.Ordinal))
                throw new DomainValidationException("A branch with this code already exists");

            branch.Name = name;
            branch.Code = code;
            branch.Address = request.Address == null ? null : request.Address.Trim();
            return _repository.Update(branch);
        }

        public void SetActive(string id, bool isActive)
        {
            var branch = _repository.GetById(id);
            if (branch == null) throw new NotFoundException("Branch not found");
            if (!isActive && _repository.CountTerminals(id) > 0)
                throw new DomainValidationException("Cannot disable a branch that still has terminals");
            _repository.SetActive(id, isActive);
        }

        public void Delete(string id)
        {
            var branch = _repository.GetById(id);
            if (branch == null) throw new NotFoundException("Branch not found");
            if (_repository.CountTerminals(id) > 0)
                throw new DomainValidationException("Cannot delete a branch that still has terminals");
            if (_repository.HasTransactionReferences(id))
                throw new DomainValidationException("Cannot delete a branch referenced by transactions; disable it instead");
            _repository.Delete(id);
        }

        public void Configure(string branchId, string terminalId, string nodeRole, string centralBaseUrl)
        {
            if (string.IsNullOrWhiteSpace(branchId)) throw new DomainValidationException("Branch is required");
            if (string.IsNullOrWhiteSpace(terminalId)) throw new DomainValidationException("Terminal is required");
            if (nodeRole != "branch" && nodeRole != "central") throw new DomainValidationException("Invalid node role");
            _repository.SaveRuntimeContext(branchId.Trim(), terminalId.Trim(), nodeRole, centralBaseUrl == null ? null : centralBaseUrl.Trim());
        }

        public void SaveCredentials(string centralToken, string centralBranchKey)
        {
            _repository.SaveRuntimeCredentials(centralToken == null ? null : centralToken.Trim(),
                centralBranchKey == null ? null : centralBranchKey.Trim());
        }

        public BranchAgentConfig GetAgentConfig()
        {
            return _repository.GetAgentConfig();
        }

        public SetupStatus GetSetupStatus()
        {
            var context = _repository.GetRuntimeContext();
            return new SetupStatus
            {
                SetupComplete = _repository.IsSetupComplete(),
                NodeRole = context == null || string.IsNullOrWhiteSpace(context.NodeRole) ? "branch" : context.NodeRole
            };
        }

        public void Setup(BranchSetupRequest request)
        {
            if (request == null) throw new DomainValidationException("Invalid setup request");
            var role = (request.NodeRole ?? string.Empty).Trim().ToLowerInvariant();
            if (role != "branch" && role != "central")
                throw new DomainValidationException("Invalid node role");

            if (role == "central")
            {
                var existing = _repository.GetRuntimeContext();
                var branchId = string.IsNullOrWhiteSpace(request.BranchId)
                    ? (existing == null || existing.Branch == null ? null : existing.Branch.Id)
                    : request.BranchId.Trim();
                var terminalId = string.IsNullOrWhiteSpace(request.TerminalId)
                    ? (existing == null || existing.Terminal == null ? null : existing.Terminal.Id)
                    : request.TerminalId.Trim();
                if (string.IsNullOrWhiteSpace(branchId) || string.IsNullOrWhiteSpace(terminalId))
                    throw new DomainValidationException("Branch and terminal are required");

                Configure(branchId, terminalId, "central", request.CentralBaseUrl);
                _repository.MarkSetupComplete();
                return;
            }

            var centralBaseUrl = (request.CentralBaseUrl ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(centralBaseUrl))
                throw new DomainValidationException("Central server URL is required");
            if (string.IsNullOrWhiteSpace(request.BranchId))
                throw new DomainValidationException("Branch id is required");
            if (string.IsNullOrWhiteSpace(request.TerminalId))
                throw new DomainValidationException("Terminal id is required");
            var syncToken = (request.SyncToken ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(syncToken))
                throw new DomainValidationException("Sync token is required");
            var branchKey = (request.BranchKey ?? string.Empty).Trim();

            var branchIdValue = request.BranchId.Trim();
            var terminalIdValue = request.TerminalId.Trim();

            _enrollmentClient.VerifyCentral(centralBaseUrl, branchIdValue, branchKey, syncToken);

            _repository.EnsureBranchAndTerminal(branchIdValue, terminalIdValue);
            Configure(branchIdValue, terminalIdValue, "branch", centralBaseUrl);
            SaveCredentials(syncToken, branchKey);
            _repository.MarkSetupComplete();
        }
    }
}
