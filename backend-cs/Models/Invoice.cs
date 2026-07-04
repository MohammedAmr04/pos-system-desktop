using System;
using System.Collections.Generic;

namespace PosCs.Models
{
    public class Invoice
    {
        public string Id { get; set; }
        public double TotalAmount { get; set; }
        public double Discount { get; set; }
        public DateTime CreatedAt { get; set; }

        [Newtonsoft.Json.JsonProperty("InvoiceDetail")]
        public List<InvoiceDetail> InvoiceDetail { get; set; } = new List<InvoiceDetail>();
    }

    public class InvoiceDetail
    {
        public string Id { get; set; }
        public string InvoiceId { get; set; }
        public string ProductId { get; set; }
        public int Quantity { get; set; }
        public double BuyPrice { get; set; }
        public double SalePrice { get; set; }
        public Product Product { get; set; }
    }
}
