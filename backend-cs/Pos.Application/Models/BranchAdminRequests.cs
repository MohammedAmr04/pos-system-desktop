namespace PosCs.Application.Models
{
    public sealed class CreateBranchRequest
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
    }

    public sealed class UpdateBranchRequest
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
    }

    public sealed class CreateTerminalRequest
    {
        public string BranchId { get; set; }
        public string Name { get; set; }
        public string Notes { get; set; }
    }

    public sealed class UpdateTerminalRequest
    {
        public string Name { get; set; }
        public string Notes { get; set; }
        public string BranchId { get; set; }
    }

    /// <summary>Central-side branch switch request. The terminal echoes its current branch so
    /// central can verify the move and detect lost-response retries via <c>requestId</c>.</summary>
    public sealed class BranchSwitchRequest
    {
        public string RequestId { get; set; }
        public string TerminalId { get; set; }
        public string TargetBranchId { get; set; }
        /// <summary>Optional plaintext key for the target branch. Required (and verified) when the
        /// target branch has an enrolled sync secret; never stored or returned by central.</summary>
        public string TargetBranchKey { get; set; }
    }

    public sealed class BranchCredentialsRequest
    {
        public string CentralToken { get; set; }
        public string CentralBranchKey { get; set; }
    }

    public sealed class BranchAssignmentResult
    {
        public string RequestId { get; set; }
        public string TerminalId { get; set; }
        public string BranchId { get; set; }
        public string BranchCode { get; set; }
        public string BranchName { get; set; }
        public string FromBranchId { get; set; }
        public string Outcome { get; set; }
    }
}
