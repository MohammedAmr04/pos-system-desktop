using System;
using System.Collections.Generic;

namespace PosCs.Models
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

        [Newtonsoft.Json.JsonProperty("InvoiceDetail")]
        public List<InvoiceDetail> InvoiceDetail { get; set; } = new List<InvoiceDetail>();
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
        public string PriceEditNote { get; set; }
        public Product Product { get; set; }
    }
}
