using System.Collections.Generic;

namespace PosCs.Application.Models
{
    public sealed class CreateProductRequest
    {
        public string Name { get; set; }
        public string ProductType { get; set; }
        public double ServiceCost { get; set; }
        public List<BundleComponentRequest> BundleComponents { get; set; }
        public string Barcode { get; set; }
        public double BuyPrice { get; set; }
        public double SalePrice { get; set; }
        public double RetailPrice { get; set; }
        public double? WholesalePrice { get; set; }
        public string UnitName { get; set; }
        /// <summary>Optional reference into the shared Unit master (resolves/validates UnitName).</summary>
        public string UnitId { get; set; }
        public double StockQuantity { get; set; }
        public string Notes { get; set; }
        public bool AllowDiscount { get; set; } = true;
        public int LowStockThreshold { get; set; }
        /// <summary>When true, hide the product from POS search while keeping it visible everywhere else.</summary>
        public bool IsHiddenFromPOS { get; set; }
        /// <summary>Nullable master-data references: null/empty means unassigned.</summary>
        public string CategoryId { get; set; }
        public string BrandId { get; set; }
    }

    public sealed class BundleComponentRequest
    {
        public string ProductId { get; set; }
        public double Quantity { get; set; }
    }

    public sealed class UpdateProductRequest
    {
        public string Name { get; set; }
        public string ProductType { get; set; }
        public double? ServiceCost { get; set; }
        public List<BundleComponentRequest> BundleComponents { get; set; }
        public string Barcode { get; set; }
        public double BuyPrice { get; set; }
        public double StockQuantity { get; set; }
        public string Notes { get; set; }
        public bool? AllowDiscount { get; set; }
        public int? LowStockThreshold { get; set; }
        public double? RetailPrice { get; set; }
        public double? WholesalePrice { get; set; }
        /// <summary>When true, hide the product from POS search while keeping it visible everywhere else.</summary>
        public bool? IsHiddenFromPOS { get; set; }
        public string UnitName { get; set; }
        /// <summary>Null keeps the current unit link; a value re-links the base unit to that master unit.</summary>
        public string UnitId { get; set; }
        /// <summary>Master-data reference update semantics: null keeps the current value,
        /// an empty string clears the reference, a value assigns it (must be active).</summary>
        public string CategoryId { get; set; }
        public string BrandId { get; set; }
    }

    public sealed class AddUnitRequest
    {
        public string UnitName { get; set; }
        /// <summary>Optional reference into the shared Unit master (resolves/validates UnitName).</summary>
        public string UnitId { get; set; }
        public double QuantityFactor { get; set; }
        public double RetailPrice { get; set; }
        public double? WholesalePrice { get; set; }
    }

    public sealed class UpdateUnitRequest
    {
        public string UnitName { get; set; }
        /// <summary>Null keeps the current unit link; a value re-links to that master unit.</summary>
        public string UnitId { get; set; }
        public double? QuantityFactor { get; set; }
        public double? RetailPrice { get; set; }
        public double? WholesalePrice { get; set; }
    }

    public sealed class AddBarcodeRequest
    {
        public string Barcode { get; set; }
    }

    public sealed class CreateInvoiceRequest
    {
        public List<InvoiceItemRequest> Items { get; set; }
        public double Discount { get; set; }
        public string DiscountType { get; set; }
        public double DiscountValue { get; set; }
        public string PriceMode { get; set; }
        /// <summary>'cash' | 'credit' (default cash); credit requires a client (spec §21).</summary>
        public string PaymentMethod { get; set; }
        public string ClientId { get; set; }
        /// <summary>Optional salesperson (Employee directory). Immutable once posted.</summary>
        public string EmployeeId { get; set; }
        /// <summary>'draft' | 'posted' (default posted). Drafts have zero side effects.</summary>
        public string Status { get; set; }
    }

    public sealed class InvoiceItemRequest
    {
        public string ProductId { get; set; }
        public string ProductUnitId { get; set; }
        public string UnitName { get; set; }
        public string Name { get; set; }
        public double BuyPrice { get; set; }
        public double SalePrice { get; set; }
        public double OriginalUnitPrice { get; set; }
        public double UnitPrice { get; set; }
        public double Quantity { get; set; }
        public double MaxStock { get; set; }
        public bool AllowDiscount { get; set; } = true;
        public string DiscountType { get; set; }
        public double DiscountValue { get; set; }
        public double QuantityFactor { get; set; }
        public string PriceEditNote { get; set; }
    }

    public sealed class CreateSaleReturnRequest
    {
        public List<SaleReturnItemRequest> Items { get; set; }
        public string Notes { get; set; }
    }

    public sealed class SaleReturnItemRequest
    {
        public string InvoiceDetailId { get; set; }
        public double Quantity { get; set; }
    }
}
