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
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
