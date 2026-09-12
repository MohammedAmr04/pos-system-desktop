using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PosCs.Application.Models;
using PosCs.Application.Ports;
using PosCs.Infrastructure.Persistence;

namespace PosCs.Infrastructure.Printing
{
    /// <summary>
    /// Renders the receipt as a monochrome image (ReceiptBuilder + GDI+ handles Arabic shaping),
    /// converts it to ESC/POS raster bytes and sends them to the raw printer.
    /// Printer/receipt behaviour comes from stored settings (plan Phase 12) with code defaults.
    /// </summary>
    public class ReceiptPrinter : IReceiptPrinter
    {
        private readonly IPrinterSettingsRepository _settings;

        public ReceiptPrinter(IPrinterSettingsRepository settings)
        {
            _settings = settings;
        }

        public PrintOutcome PrintReceipt(ReceiptContent receipt)
        {
            try
            {
                var config = _settings.Get();
                var printerName = Environment.GetEnvironmentVariable("PRINTER_NAME") ?? config.ReceiptPrinterName;
                var logoFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "logo.jpeg");

                using (var builder = new ReceiptBuilder(config.RasterWidth))
                {
                    if (config.ShowLogo)
                        builder.AddLogo(logoFile);

                    builder.AddStoreInfo(config.StoreName, config.StorePhone, config.StoreAddress, config.ReceiptHeader);
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
                        FinalTotal = i.FinalTotal,
                        Components = i.Components.Select(c => new ReceiptComponentModel { Name = c.Name, Quantity = c.Quantity }).ToList()
                    }));
                    builder.AddTotals(new ReceiptInvoiceModel
                    {
                        Discount = receipt.Discount,
                        TotalAmount = receipt.TotalAmount
                    });
                    builder.AddFooter(config.ReceiptFooter);

                    using (var finalReceipt = builder.GetFinishedReceipt())
                    {
                        byte[] imageBytes = ImagePrinter.GetImageBytes(finalReceipt);

                        var bytes = new List<byte>();
                        bytes.AddRange(new byte[] { 0x1B, 0x40 }); // ESC @ reset
                        for (var copy = 0; copy < config.Copies; copy++)
                            bytes.AddRange(imageBytes);             // raster image (one per configured copy)
                        if (config.AutoCut)
                            bytes.AddRange(new byte[] { 0x1D, 0x56, 0x42, 0x00 }); // full cut
                        if (config.OpenCashDrawer)
                            bytes.AddRange(new byte[] { 0x1B, 0x70, 0x00, 0x19, 0xFA }); // drawer kick pin 2

                        bool success = new PrinterService().PrintBytes(printerName, bytes.ToArray());
                        if (success)
                        {
                            Console.WriteLine($"[API] Custom Arabic Receipt printed for invoice: {receipt.Id} copies={config.Copies}");
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
