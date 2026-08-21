using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PosCs.Application.Models;
using PosCs.Application.Ports;

namespace PosCs.Infrastructure.Printing
{
    /// <summary>
    /// Renders the receipt as a monochrome image (ReceiptBuilder + GDI+ handles Arabic shaping),
    /// converts it to ESC/POS raster bytes and sends them to the raw printer.
    /// </summary>
    public class ReceiptPrinter : IReceiptPrinter
    {
        private static string ResolvePrinterName()
        {
            return Environment.GetEnvironmentVariable("PRINTER_NAME") ?? "Xprinter";
        }

        public PrintOutcome PrintReceipt(ReceiptContent receipt)
        {
            try
            {
                var printerName = ResolvePrinterName();
                var logoFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "logo.jpeg");

                using (var builder = new ReceiptBuilder())
                {
                    builder.AddLogo(logoFile);
                    builder.AddHeader(new ReceiptInvoiceModel
                    {
                        Id = receipt.Id,
                        CreatedAt = receipt.CreatedAt,
                        InvoiceNumber = receipt.InvoiceNumber
                    });
                    builder.AddItems(receipt.Items.Select(i => new ReceiptItemModel
                    {
                        Name = i.Name,
                        UnitName = i.UnitName,
                        Quantity = i.Quantity,
                        SalePrice = i.SalePrice,
                        FinalTotal = i.FinalTotal
                    }));
                    builder.AddTotals(new ReceiptInvoiceModel
                    {
                        Discount = receipt.Discount,
                        TotalAmount = receipt.TotalAmount
                    });
                    builder.AddFooter();

                    using (var finalReceipt = builder.GetFinishedReceipt())
                    {
                        byte[] imageBytes = ImagePrinter.GetImageBytes(finalReceipt);

                        var bytes = new List<byte>();
                        bytes.AddRange(new byte[] { 0x1B, 0x40 }); // ESC @ reset
                        bytes.AddRange(imageBytes);                 // raster image
                        bytes.AddRange(new byte[] { 0x1D, 0x56, 0x42, 0x00 }); // cut

                        bool success = new PrinterService().PrintBytes(printerName, bytes.ToArray());
                        if (success)
                        {
                            Console.WriteLine($"[API] Custom Arabic Receipt printed for invoice: {receipt.Id}");
                            return new PrintOutcome
                            {
                                Status = 200,
                                Success = true,
                                Message = "Printed successfully via Image Mode."
                            };
                        }
                        return new PrintOutcome
                        {
                            Status = 500,
                            Success = false,
                            Message = "Failed to send data to the printer."
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Print receipt failed: {ex}");
                return new PrintOutcome
                {
                    Status = 500,
                    Success = false,
                    Message = "Print failed",
                    Detail = ex.Message
                };
            }
        }
    }
}
