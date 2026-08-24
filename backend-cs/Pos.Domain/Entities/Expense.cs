using System;

namespace PosCs.Domain.Entities
{
    /// <summary>An operating expense (plan Phase 10, spec §26): rent, utilities, salaries...
    /// Strictly separate from purchases — never touches supplier balances.</summary>
    public class Expense
    {
        public string Id { get; set; }
        public string CategoryId { get; set; }
        /// <summary>Resolved name of the category for display (not persisted on Expense).</summary>
        public string CategoryName { get; set; }
        public double Amount { get; set; }
        /// <summary>'cash' | 'card' | 'bank_transfer' — only 'cash' moves the drawer.</summary>
        public string PaymentMethod { get; set; } = "cash";
        public DateTime Date { get; set; }
        public string Description { get; set; }
        public string Reference { get; set; }

        /// <summary>Active shift when the expense was recorded; cash expenses reduce that shift's expected cash.</summary>
        public string ShiftId { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
