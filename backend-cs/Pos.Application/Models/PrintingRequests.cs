using System.Collections.Generic;

namespace PosCs.Application.Models
{
    /// <summary>Wire payload of POST /api/printing/print. Items and InvoiceDetail are
    /// alternative sources for the receipt lines (Items wins when non-empty).</summary>
    public sealed class PrintReceiptRequest
    {
        public PrintInvoicePayload Invoice { get; set; }
    }

    public sealed class PrintInvoicePayload
    {
        public string Id { get; set; }
        public int InvoiceNumber { get; set; }
        public double TotalAmount { get; set; }
        public double Discount { get; set; }
        public System.DateTime? CreatedAt { get; set; }
        public List<PrintItemPayload> Items { get; set; }
        public List<PrintDetailPayload> InvoiceDetail { get; set; }
    }

    public sealed class PrintItemPayload
    {
        public string Name { get; set; }
        public string UnitName { get; set; }
        public double Quantity { get; set; }
        public double SalePrice { get; set; }
        public double? FinalTotal { get; set; }
    }

    public sealed class PrintDetailPayload
    {
        public double Quantity { get; set; }
        public double SalePrice { get; set; }
        public double? FinalTotal { get; set; }
        public string UnitName { get; set; }
        public PrintProductPayload Product { get; set; }
        public string Name { get; set; }
    }

    public sealed class PrintProductPayload
    {
        public string Name { get; set; }
    }

    public sealed class PrintBarcodeLabelRequest
    {
        public string Barcode { get; set; }
        public string Name { get; set; }
        public double Price { get; set; }
        public int Count { get; set; }
    }

    /// <summary>Legacy stub payload of POST /api/printing/receipt.</summary>
    public sealed class ReceiptTextRequest
    {
        public List<ReceiptTextItemRequest> Items { get; set; }
        public double Total { get; set; }
        public double Discount { get; set; }
        public bool Arabic { get; set; }
    }

    public sealed class ReceiptTextItemRequest
    {
        public string Name { get; set; }
        public int Quantity { get; set; }
        public double SalePrice { get; set; }
    }

    /// <summary>Legacy stub payload of POST /api/printing/barcode.</summary>
    public sealed class BarcodeTextRequest
    {
        public string Barcode { get; set; }
        public string ProductName { get; set; }
    }
}
