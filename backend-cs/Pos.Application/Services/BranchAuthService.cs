using System;
using System.Collections.Generic;
using PosCs.Application.Models;
using PosCs.Application.Ports;
using PosCs.Domain.Exceptions;

namespace PosCs.Application.Services
{
    public enum BranchSecretState
    {
        /// <summary>The branch has no key enrolled; central falls back to authenticated-user trust.</summary>
        NotConfigured,
        Valid,
        Invalid
    }

    /// <summary>
    /// Branch identity for central sync. A branch proves itself with a long-lived key whose hash is
    /// stored on the central <c>Branch</c> row; central also rejects operations that claim a branch
    /// other than the one that authenticated.
    /// </summary>
    public class BranchAuthService
    {
        public const int MinimumKeyLength = 16;

        private readonly IBranchRepository _branches;
        private readonly IPasswordHasher _hasher;

        public BranchAuthService(IBranchRepository branches, IPasswordHasher hasher)
        {
            _branches = branches;
            _hasher = hasher;
        }

        public BranchSecretState VerifyKey(string branchId, string key)
        {
            if (string.IsNullOrWhiteSpace(branchId))
                return BranchSecretState.Invalid;
            if (_branches.GetById(branchId) == null)
                return BranchSecretState.Invalid;

            var secretHash = _branches.GetSecretHash(branchId);
            if (string.IsNullOrWhiteSpace(secretHash))
                return BranchSecretState.NotConfigured;
            if (string.IsNullOrWhiteSpace(key))
                return BranchSecretState.Invalid;
            return _hasher.Verify(secretHash, key) ? BranchSecretState.Valid : BranchSecretState.Invalid;
        }

        public void SetSecret(string branchId, string key)
        {
            if (string.IsNullOrWhiteSpace(branchId))
                throw new DomainValidationException("Branch is required");
            if (string.IsNullOrWhiteSpace(key) || key.Trim().Length < MinimumKeyLength)
                throw new DomainValidationException($"Branch key must be at least {MinimumKeyLength} characters");
            if (_branches.GetById(branchId) == null)
                throw new NotFoundException("Branch not found");

            _branches.SetSecretHash(branchId, _hasher.Hash(key.Trim()));
        }

        /// <summary>Rejects a push that contains an operation belonging to a different branch than
        /// the authenticated one. A blank authenticated branch (unenrolled) skips the check.</summary>
        public void EnsureOperationsBelongToBranch(IEnumerable<SyncOperation> operations, string authenticatedBranchId)
        {
            if (string.IsNullOrWhiteSpace(authenticatedBranchId) || operations == null)
                return;

            foreach (var operation in operations)
            {
                if (operation == null) continue;
                if (!string.Equals(operation.BranchId, authenticatedBranchId, StringComparison.Ordinal))
                    throw new DomainValidationException($"Operation '{operation.Id}' does not belong to branch '{authenticatedBranchId}'");
            }
        }
    }
}
