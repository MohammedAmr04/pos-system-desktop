using System.Threading;

namespace PosCs.Application.Ports
{
    /// <summary>
    /// Per-request operational branch scope, derived server-side from the local
    /// <c>BranchRuntimeConfig</c> (the terminal's branch). Cashier-facing operations are forced
    /// to this branch; only users with <c>branches.view</c> may cross branches.
    /// </summary>
    public interface IBranchContext
    {
        string BranchId { get; }
        string TerminalId { get; }
        bool CanCrossBranch { get; }

        /// <summary>Resolves the effective branch filter for a read/list/report call. Cross-branch
        /// users keep the requested value; everyone else is silently clamped to their own branch.</summary>
        string ApplyScope(string requestedBranchId);
    }

    /// <summary>Ambient holder for the current request's branch scope. Flows through async
    /// continuations via <see cref="AsyncLocal{T}"/>; null outside a user request (agent/sync/tests).</summary>
    public static class BranchScope
    {
        private static readonly AsyncLocal<IBranchContext> _current = new AsyncLocal<IBranchContext>();

        public static IBranchContext Current
        {
            get { return _current.Value; }
        }

        public static void Set(string branchId, string terminalId, bool canCrossBranch)
        {
            _current.Value = new ScopedBranchContext(branchId, terminalId, canCrossBranch);
        }

        public static void Clear()
        {
            _current.Value = null;
        }

        private sealed class ScopedBranchContext : IBranchContext
        {
            public ScopedBranchContext(string branchId, string terminalId, bool canCrossBranch)
            {
                BranchId = branchId;
                TerminalId = terminalId;
                CanCrossBranch = canCrossBranch;
            }

            public string BranchId { get; private set; }
            public string TerminalId { get; private set; }
            public bool CanCrossBranch { get; private set; }

            public string ApplyScope(string requestedBranchId)
            {
                if (CanCrossBranch)
                    return string.IsNullOrWhiteSpace(requestedBranchId) ? null : requestedBranchId.Trim();
                return BranchId;
            }
        }
    }

    /// <summary>No request scope: behaves as unrestricted (used by the sync agent and tests).</summary>
    public sealed class NullBranchContext : IBranchContext
    {
        public static readonly NullBranchContext Instance = new NullBranchContext();

        private NullBranchContext() { }

        public string BranchId { get { return null; } }
        public string TerminalId { get { return null; } }
        public bool CanCrossBranch { get { return true; } }

        public string ApplyScope(string requestedBranchId)
        {
            return string.IsNullOrWhiteSpace(requestedBranchId) ? null : requestedBranchId.Trim();
        }
    }

    /// <summary>Singleton that always resolves the ambient scope for the current request.</summary>
    public sealed class AmbientBranchContext : IBranchContext
    {
        public static readonly AmbientBranchContext Instance = new AmbientBranchContext();

        private AmbientBranchContext() { }

        private IBranchContext Current
        {
            get { return BranchScope.Current ?? NullBranchContext.Instance; }
        }

        public string BranchId { get { return Current.BranchId; } }
        public string TerminalId { get { return Current.TerminalId; } }
        public bool CanCrossBranch { get { return Current.CanCrossBranch; } }

        public string ApplyScope(string requestedBranchId)
        {
            return Current.ApplyScope(requestedBranchId);
        }
    }
}
