using System;

namespace PosCs.Models
{
    public class Product
    {
        public string Id { get; set; }
        public string Barcode { get; set; }
        public string Name { get; set; }
        public double BuyPrice { get; set; }
        public double SalePrice { get; set; }
        public int StockQuantity { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
