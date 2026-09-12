using System;

namespace PosCs.Domain.Entities
{
    /// <summary>A user-managed expense category (plan Phase 10, spec §26): rent, electricity,
    /// water, internet, maintenance, transportation, salaries, other — seeded by migration.</summary>
    public class ExpenseCategory
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
    }
}
