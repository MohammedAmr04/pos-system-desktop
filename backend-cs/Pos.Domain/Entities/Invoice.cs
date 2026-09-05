using System;
using System.Collections.Generic;

namespace PosCs.Domain.Entities
{
    public class Invoice
    {
        public string Id { get; set; }
        public double TotalAmount { get; set; }
        public double Discount { get; set; }
        public string DiscountType { get; set; }
        public double DiscountValue { get; set; }
        public double DiscountAmount { get; set; }
        public string PriceMode { get; set; }
        public DateTime CreatedAt { get; set; }
        public int InvoiceNumber { get; set; }

        /// <summary>'draft' | 'posted' | 'cancelled' (plan Phase 6). Drafts have zero side effects.</summary>
        public string Status { get; set; } = "posted";
        /// <summary>NULL | 'partial' | 'full' — accumulates as returns are created (plan Phase 7).</summary>
        public string ReturnStatus { get; set; }
        /// <summary>'cash' | 'credit'; credit requires a client.</summary>
        public string PaymentMethod { get; set; } = "cash";
        public string ClientId { get; set; }
        public string CreatedBy { get; set; }
        /// <summary>Optional salesperson attribution (Employee directory, not a login).</summary>
        public string EmployeeId { get; set; }
        /// <summary>The shift that was open when the sale was posted (plan Phase 9).</summary>
        public string ShiftId { get; set; }

        // Serialized under its exact legacy key by the API contract resolver.
        public List<InvoiceDetail> InvoiceDetail { get; set; } = new List<InvoiceDetail>();
        public Client Client { get; set; }
        public Employee Employee { get; set; }
    }

    public class InvoiceDetail
    {
        public string Id { get; set; }
        public string InvoiceId { get; set; }
        public string ProductId { get; set; }
        public string ProductUnitId { get; set; }
        public string UnitName { get; set; }
        public double Quantity { get; set; }
        public double BuyPrice { get; set; }
        public double SalePrice { get; set; }
        public double OriginalUnitPrice { get; set; }
        public double UnitPrice { get; set; }
        public string DiscountType { get; set; }
        public double DiscountValue { get; set; }
        public double DiscountAmount { get; set; }
        public double LineSubtotal { get; set; }
        public double FinalTotal { get; set; }
        public double QuantityFactor { get; set; }
        /// <summary>Historical COGS captured at sale time via FIFO allocation (null for legacy rows).</summary>
        public double? TotalCost { get; set; }
        public string PriceEditNote { get; set; }
        public Product Product { get; set; }
    }
}
