using System;
using System.Text;
using PosCs.Application.Models;
using PosCs.Application.Ports;

namespace PosCs.Infrastructure.Printing
{
    /// <summary>Prints barcode labels via native ESC/POS Code128 commands.</summary>
    public class BarcodeLabelPrinter : IBarcodeLabelPrinter
    {
        private readonly IPrinterSettingsRepository _settings;

        public BarcodeLabelPrinter(IPrinterSettingsRepository settings)
        {
            _settings = settings;
        }

        public PrintOutcome PrintLabels(string barcode, string productName, double price, int count)
        {
            try
            {
                var config = _settings.Get();
                var printerName = Environment.GetEnvironmentVariable("PRINTER_NAME") ?? config.LabelPrinterName;
                var escPos = new ReceiptService().BuildEscPosBarcode(barcode, productName, price, count);
                var success = new PrinterService().PrintBytes(printerName, Encoding.ASCII.GetBytes(escPos));

                if (success)
                {
                    return new PrintOutcome
                    {
                        Status = 200,
                        Success = true,
                        Message = "Barcode label(s) printed successfully"
                    };
                }
                return new PrintOutcome
                {
                    Status = 200,
                    Success = false,
                    Message = $"Barcode print failed: could not open printer '{printerName}'"
                };
            }
            catch (Exception ex)
            {
                return new PrintOutcome
                {
                    Status = 500,
                    Success = false,
                    Message = "Barcode print failed",
                    Detail = ex.Message
                };
            }
        }
    }
}
