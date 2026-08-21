using System.Collections.Generic;

namespace PosCs.Application.Models
{
    public sealed class CreateProductRequest
    {
        public string Name { get; set; }
        public string Barcode { get; set; }
        public double BuyPrice { get; set; }
        public double SalePrice { get; set; }
        public double RetailPrice { get; set; }
        public double? WholesalePrice { get; set; }
        public string UnitName { get; set; }
        public double StockQuantity { get; set; }
        public string Notes { get; set; }
        public bool AllowDiscount { get; set; } = true;
        public int LowStockThreshold { get; set; }
    }

    public sealed class UpdateProductRequest
    {
        public string Name { get; set; }
        public string Barcode { get; set; }
        public double BuyPrice { get; set; }
        public double StockQuantity { get; set; }
        public string Notes { get; set; }
        public bool? AllowDiscount { get; set; }
        public int? LowStockThreshold { get; set; }
        public double? RetailPrice { get; set; }
        public double? WholesalePrice { get; set; }
        public string UnitName { get; set; }
    }

    public sealed class AddUnitRequest
    {
        public string UnitName { get; set; }
        public double QuantityFactor { get; set; }
        public double RetailPrice { get; set; }
        public double? WholesalePrice { get; set; }
    }

    public sealed class UpdateUnitRequest
    {
        public string UnitName { get; set; }
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
}
