using System;

namespace PosCs.Domain.Entities
{
    /// <summary>Single ledger row of an inventory mutation. Quantity is signed, in base units.</summary>
    public class StockMovement
    {
        public string Id { get; set; }
        public string ProductId { get; set; }
        public double Quantity { get; set; }
        public string Type { get; set; }
        public string ReferenceId { get; set; }
        public string ReferenceNumber { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
