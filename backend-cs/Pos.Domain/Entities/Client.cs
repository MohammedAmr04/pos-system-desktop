using System;

namespace PosCs.Domain.Entities
{
    public class Client
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string Notes { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        /// <summary>
        /// Computed account balance, populated only by the paged listing (posted
        /// invoices minus payments). Positive means the client owes us; negative
        /// means the client has advance credit. Never persisted.
        /// </summary>
        public double Balance { get; set; }
    }
}
