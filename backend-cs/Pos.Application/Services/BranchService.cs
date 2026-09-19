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

        public BranchService(IBranchRepository repository)
        {
            _repository = repository;
        }

        public BranchRuntimeContext GetRuntimeContext()
        {
            var context = _repository.GetRuntimeContext();
            if (context == null || context.Branch == null || context.Terminal == null)
                throw new DomainValidationException("Branch runtime configuration is incomplete");
            return context;
        }

        public List<Branch> GetAll(bool activeOnly)
        {
            return _repository.GetAll(activeOnly);
        }

        public void Configure(string branchId, string terminalId, string nodeRole, string centralBaseUrl)
        {
            if (string.IsNullOrWhiteSpace(branchId)) throw new DomainValidationException("Branch is required");
            if (string.IsNullOrWhiteSpace(terminalId)) throw new DomainValidationException("Terminal is required");
            if (nodeRole != "branch" && nodeRole != "central") throw new DomainValidationException("Invalid node role");
            _repository.SaveRuntimeContext(branchId.Trim(), terminalId.Trim(), nodeRole, centralBaseUrl == null ? null : centralBaseUrl.Trim());
        }
    }
}
