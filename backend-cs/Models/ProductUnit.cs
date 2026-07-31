using System;
using System.Collections.Generic;

namespace PosCs.Models
{
    public class ProductUnit
    {
        public string Id { get; set; }
        public string ProductId { get; set; }
        public string UnitName { get; set; }
        public double QuantityFactor { get; set; }
        public double RetailPrice { get; set; }
        public double? WholesalePrice { get; set; }
        public bool IsBaseUnit { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<ProductBarcode> Barcodes { get; set; }
    }
}
