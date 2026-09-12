using System.Collections.Generic;

namespace PosCs.Application.Ports
{
    /// <summary>Outcome of a print operation. Message is user-facing and must be returned verbatim.
    /// Status is the HTTP status the endpoint should respond with.</summary>
    public sealed class PrintOutcome
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int Status { get; set; } = 200;
        public string Detail { get; set; }
    }

    /// <summary>Fully resolved receipt ready for rendering.</summary>
    public sealed class ReceiptContent
    {
        public string Id { get; set; }
        public int InvoiceNumber { get; set; }
        public System.DateTime? CreatedAt { get; set; }
        public double Discount { get; set; }
        public double TotalAmount { get; set; }
        public List<ReceiptLineItem> Items { get; set; } = new List<ReceiptLineItem>();
    }

    public sealed class ReceiptLineItem
    {
        public string Name { get; set; }
        public string UnitName { get; set; }
        public double Quantity { get; set; }
        public double SalePrice { get; set; }
        public double? FinalTotal { get; set; }
        public List<ReceiptComponent> Components { get; set; } = new List<ReceiptComponent>();
    }

    public sealed class ReceiptComponent
    {
        public string Name { get; set; }
        public double Quantity { get; set; }
    }

    /// <summary>Renders the Arabic receipt as an image and sends it to the thermal printer.</summary>
    public interface IReceiptPrinter
    {
        PrintOutcome PrintReceipt(ReceiptContent receipt);
    }

    /// <summary>Prints barcode label(s) as ESC/POS barcode commands.</summary>
    public interface IBarcodeLabelPrinter
    {
        PrintOutcome PrintLabels(string barcode, string productName, double price, int count);
    }
}
