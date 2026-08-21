using System.Collections.Generic;
using System.Linq;
using PosCs.Application.Models;
using PosCs.Application.Ports;

namespace PosCs.Application.Services
{
    /// <summary>Printing use cases: receipt image printing and barcode label printing.</summary>
    public class PrintingService
    {
        private readonly IReceiptPrinter _receiptPrinter;
        private readonly IBarcodeLabelPrinter _labelPrinter;

        public PrintingService(IReceiptPrinter receiptPrinter, IBarcodeLabelPrinter labelPrinter)
        {
            _receiptPrinter = receiptPrinter;
            _labelPrinter = labelPrinter;
        }

        public PrintOutcome PrintReceipt(PrintReceiptRequest request)
        {
            if (request?.Invoice == null)
                return new PrintOutcome { Success = false, Message = "Missing invoice data" };

            var invoice = request.Invoice;
            var content = new ReceiptContent
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                CreatedAt = invoice.CreatedAt,
                Discount = invoice.Discount,
                TotalAmount = invoice.TotalAmount,
                Items = ExtractItems(invoice)
            };

            return _receiptPrinter.PrintReceipt(content);
        }

        public PrintOutcome PrintBarcodeLabel(PrintBarcodeLabelRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Barcode))
                return new PrintOutcome { Success = false, Message = "Missing barcode" };

            var count = request.Count > 0 ? request.Count : 1;
            return _labelPrinter.PrintLabels(request.Barcode, request.Name, request.Price, count);
        }

        private static List<ReceiptLineItem> ExtractItems(PrintInvoicePayload invoice)
        {
            var items = invoice.Items ?? new List<PrintItemPayload>();
            if (items.Count == 0 && invoice.InvoiceDetail != null)
            {
                items = invoice.InvoiceDetail.Select(d => new PrintItemPayload
                {
                    Name = d.Product?.Name ?? d.Name ?? "Item",
                    Quantity = d.Quantity,
                    SalePrice = d.SalePrice,
                    UnitName = d.UnitName,
                    FinalTotal = d.FinalTotal
                }).ToList();
            }
            return items.Select(i => new ReceiptLineItem
            {
                Name = i.Name ?? "Item",
                UnitName = i.UnitName,
                Quantity = i.Quantity,
                SalePrice = i.SalePrice,
                FinalTotal = i.FinalTotal
            }).ToList();
        }
    }
}
