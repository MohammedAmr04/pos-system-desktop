using System;
using System.Collections.Generic;

namespace PosCs.Domain.Entities
{
    /// <summary>A sales return transaction (plan Phase 7): references the original posted
    /// sale, restores stock and FIFO layers, and carries its own identity/number.</summary>
    public class SaleReturn
    {
        public string Id { get; set; }
        public int Number { get; set; }
        public string InvoiceId { get; set; }
        public DateTime Date { get; set; }
        /// <summary>Total refund amount (prorated line totals).</summary>
        public double TotalAmount { get; set; }
        /// <summary>FIFO cost reversed back into layers (for profit reporting).</summary>
        public double RestoredCost { get; set; }
        /// <summary>Mirrors the original sale's payment method (refund uses it, spec §23.7).</summary>
        public string PaymentMethod { get; set; } = "cash";
        public string Notes { get; set; }
        public string Status { get; set; } = "posted";
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }

        /// <summary>Convenience join value for list views (not persisted on SaleReturn).</summary>
        public int InvoiceNumber { get; set; }

        public List<SaleReturnDetail> Details { get; set; } = new List<SaleReturnDetail>();
        public Invoice Invoice { get; set; }
    }

    public class SaleReturnDetail
    {
        public string Id { get; set; }
        public string ReturnId { get; set; }
        public string InvoiceDetailId { get; set; }
        public string ProductId { get; set; }
        public string UnitName { get; set; }
        /// <summary>Returned quantity in sale units (base units = quantity × QuantityFactor).</summary>
        public double Quantity { get; set; }
        public double QuantityFactor { get; set; } = 1;
        /// <summary>Historical unit price snapshot from the original sale line.</summary>
        public double UnitPrice { get; set; }
        public double LineTotal { get; set; }
        public double RestoredCost { get; set; }
        public Product Product { get; set; }
    }
}
