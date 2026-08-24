using System;

namespace PosCs.Domain.Entities
{
    /// <summary>A batch of stock received from one purchase line, consumed by sales in FIFO order.</summary>
    public class CostLayer
    {
        public string Id { get; set; }
        public string ProductId { get; set; }
        public string SourcePurchaseId { get; set; }
        public double QuantityReceived { get; set; }
        public double QuantityRemaining { get; set; }
        public double UnitCost { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>Immutable audit row: which cost layer a sales line consumed, at which quantity/cost.</summary>
    public class SaleCostAllocation
    {
        public string Id { get; set; }
        public string InvoiceId { get; set; }
        public string InvoiceDetailId { get; set; }
        public string ProductId { get; set; }
        public string CostLayerId { get; set; }
        public double Quantity { get; set; }
        public double UnitCost { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
