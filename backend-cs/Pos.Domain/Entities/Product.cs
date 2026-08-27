using System;
using System.Collections.Generic;

namespace PosCs.Domain.Entities
{
    // Read-model enrichment fields (Barcode, Barcodes, Units, SalePrice) are not
    // persisted columns; they are populated by the application layer when serving
    // products over the API. Kept here so the API contract stays byte-identical.
    public class Product
    {
        public string Id { get; set; }
        public string Barcode { get; set; }
        public List<ProductBarcode> Barcodes { get; set; }
        public List<ProductUnit> Units { get; set; }
        public string Name { get; set; }
        public double BuyPrice { get; set; }
        public double SalePrice { get; set; }
        public double StockQuantity { get; set; }
        public string Notes { get; set; }
        public bool AllowDiscount { get; set; } = true;
        public int LowStockThreshold { get; set; }
        /// <summary>When true, the product is excluded from POS search results but still
        /// shown everywhere else (e.g. the products page and historical documents).</summary>
        public bool IsHiddenFromPOS { get; set; }
        /// <summary>Nullable: NULL means genuinely unassigned (UI shows "Other").</summary>
        public string CategoryId { get; set; }
        /// <summary>Nullable: NULL means genuinely unassigned (UI shows "Other").</summary>
        public string BrandId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
