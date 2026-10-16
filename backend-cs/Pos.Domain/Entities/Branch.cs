using System;

namespace PosCs.Domain.Entities
{
    public class Branch
    {
        public string Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class Terminal
    {
        public string Id { get; set; }
        public string BranchId { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public string Notes { get; set; }
        public DateTime? LastSeenAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class BranchAssignmentHistory
    {
        public string Id { get; set; }
        public string TerminalId { get; set; }
        public string FromBranchId { get; set; }
        public string ToBranchId { get; set; }
        public string ChangedBy { get; set; }
        public string NodeRole { get; set; }
        public string RequestId { get; set; }
        public string Outcome { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
