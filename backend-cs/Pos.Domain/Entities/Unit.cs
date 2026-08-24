using System;

namespace PosCs.Domain.Entities
{
    /// <summary>Shared unit master. The per-product conversion factor lives on ProductUnit.</summary>
    public class Unit
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
