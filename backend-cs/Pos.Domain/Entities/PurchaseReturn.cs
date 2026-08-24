using System;
using System.Collections.Generic;

namespace PosCs.Domain.Entities
{
    /// <summary>A purchase return transaction (plan Phase 8): references the original posted
    /// purchase, removes stock and reduces its FIFO cost layers at the historical unit cost.</summary>
    public class PurchaseReturn
    {
        public string Id { get; set; }
        public int Number { get; set; }
        public string PurchaseInvoiceId { get; set; }
        public DateTime Date { get; set; }
        /// <summary>Total refund amount (historical line costs).</summary>
        public double TotalAmount { get; set; }
        /// <summary>Mirrors the original purchase's payment method (spec §25).</summary>
        public string PaymentMethod { get; set; } = "cash";
        public string Notes { get; set; }
        public string Status { get; set; } = "posted";
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }

        /// <summary>Convenience join value for list views (not persisted on PurchaseReturn).</summary>
        public long PurchaseInvoiceNumber { get; set; }

        public List<PurchaseReturnDetail> Details { get; set; } = new List<PurchaseReturnDetail>();
        public PurchaseInvoice PurchaseInvoice { get; set; }
    }

    public class PurchaseReturnDetail
    {
        public string Id { get; set; }
        public string ReturnId { get; set; }
        public string PurchaseItemId { get; set; }
        public string ProductId { get; set; }
        public string UnitName { get; set; }
        /// <summary>Returned quantity in purchase units (base units = quantity × QuantityFactor).</summary>
        public double Quantity { get; set; }
        public double QuantityFactor { get; set; } = 1;
        /// <summary>Historical unit cost snapshot from the original purchase line.</summary>
        public double UnitCost { get; set; }
        public double LineTotal { get; set; }
        public Product Product { get; set; }
    }
}
