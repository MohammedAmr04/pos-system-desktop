using System;
using System.Collections.Generic;

namespace PosCs.Domain.Entities
{
    public class PurchaseInvoice
    {
        public string Id { get; set; }
        public long InvoiceNumber { get; set; }
        /// <summary>The supplier's own paper invoice number, if provided.</summary>
        public string SupplierInvoiceNumber { get; set; }
        public string SupplierId { get; set; }
        public Supplier Supplier { get; set; }
        public DateTime Date { get; set; }
        /// <summary>'cash' | 'credit' — credit purchases require a supplier (spec §11.1).</summary>
        public string PaymentMethod { get; set; }
        /// <summary>'draft' | 'posted' | 'cancelled' (spec §12).</summary>
        public string Status { get; set; }
        public double Subtotal { get; set; }
        public double Discount { get; set; }
        public double Tax { get; set; }
        public double Total { get; set; }
        public string Notes { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<PurchaseInvoiceItem> Items { get; set; }
    }

    public class PurchaseInvoiceItem
    {
        public string Id { get; set; }
        public string PurchaseInvoiceId { get; set; }
        public string ProductId { get; set; }
        public Product Product { get; set; }
        public string ProductUnitId { get; set; }
        public string UnitName { get; set; }
        public double QuantityFactor { get; set; }
        public double Quantity { get; set; }
        public double UnitCost { get; set; }
        public double LineTotal { get; set; }
        /// <summary>Optional new selling price applied to the product unit when posted (spec §12.2).</summary>
        public double? NewRetailPrice { get; set; }
        public double? NewWholesalePrice { get; set; }
    }
}
