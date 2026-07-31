using System;
using System.Collections.Generic;

namespace PosCs.Models
{
    public class Product
    {
        public string Id { get; set; }
        public string Barcode { get; set; }
        public List<ProductBarcode> Barcodes { get; set; }
        public string Name { get; set; }
        public double BuyPrice { get; set; }
        public double SalePrice { get; set; }
        public int StockQuantity { get; set; }
        public string Notes { get; set; }
        public bool AllowDiscount { get; set; } = true;
        public int LowStockThreshold { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
