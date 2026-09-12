using System;
using System.Collections.Generic;

namespace PosCs.Domain.Entities
{
    public class ProductUnit
    {
        public string Id { get; set; }
        public string ProductId { get; set; }
        /// <summary>Historical snapshot of the master unit name at assignment time.</summary>
        public string UnitName { get; set; }
        /// <summary>Reference into the shared Unit master (nullable for legacy rows).</summary>
        public string UnitId { get; set; }
        public double QuantityFactor { get; set; }
        public double RetailPrice { get; set; }
        public double? WholesalePrice { get; set; }
        public bool IsBaseUnit { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<ProductBarcode> Barcodes { get; set; }
    }
}
