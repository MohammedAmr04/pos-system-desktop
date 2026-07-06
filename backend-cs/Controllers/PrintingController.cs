using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using PosCs.Helpers;
using PosCs.Services;

namespace PosCs.Controllers
{
    [RoutePrefix("api/printing")]
    public class PrintingController : ApiController
    {
        private readonly ReceiptService _receiptService = new ReceiptService();
        private string PrinterName => Environment.GetEnvironmentVariable("PRINTER_NAME") ?? "Xprinter";
        
        // المسار الخاص بالصورة لديك
        private string logoPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "logo.jpeg");

        [Route("print")]
        [HttpPost]
        public HttpResponseMessage Print([FromBody] PrintDto dto)
        {
            try
            {
                if (dto?.Invoice == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new
                    {
                        success = false,
                        message = "Missing invoice data"
                    });

                var invoice = dto.Invoice;
                var items = ExtractItems(invoice);

                string printerName = PrinterName;

                // We no longer use IBM864 or manual shaping because they are unreliable.
                // We render the entire receipt as an image using System.Drawing (via ReceiptBuilder)
                // which flawlessly supports Arabic shaping and BiDi out of the box via Windows GDI+.

                using (var builder = new Builders.ReceiptBuilder())
                {
                    // 1. Logo
                    string logoFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "logo.jpeg");
                    builder.AddLogo(logoFile);

                    // 2. Header
                    builder.AddHeader(new Builders.ReceiptInvoiceModel { 
                        Id = invoice.Id, 
                        CreatedAt = invoice.CreatedAt,
                        InvoiceNumber = invoice.InvoiceNumber
                    });

                    // 3. Items
                    var productItems = items.Select(i => new Builders.ReceiptItemModel { Name = i.Name, Quantity = i.Quantity, SalePrice = i.SalePrice }).ToList();
                    builder.AddItems(productItems);

                    // 4. Totals
                    builder.AddTotals(new Builders.ReceiptInvoiceModel { Discount = invoice.Discount, TotalAmount = invoice.TotalAmount });

                    // 5. Footer
                    builder.AddFooter();

                    // Generate final bitmap
                    using (var finalReceipt = builder.GetFinishedReceipt())
                    {
                        // Convert to ESC/POS Raster Bytes
                        byte[] imageBytes = Printers.ImagePrinter.GetImageBytes(finalReceipt);

                        // Initialize printer reset (ESC @)
                        List<byte> bytes = new List<byte>();
                        bytes.AddRange(new byte[] { 0x1B, 0x40 });

                        // Add image bytes
                        bytes.AddRange(imageBytes);

                        // Cut paper
                        bytes.AddRange(new byte[] { 0x1D, 0x56, 0x42, 0x00 });

                        // Print
                        var printerService = new Services.PrinterService();
                        bool success = printerService.PrintBytes(printerName, bytes.ToArray());

                        if (success)
                        {
                            Console.WriteLine($"[API] Custom Arabic Receipt printed for invoice: {invoice.Id}");
                            return Request.CreateResponse(HttpStatusCode.OK, new { success = true, message = "Printed successfully via Image Mode." });
                        }
                        else
                        {
                            return Request.CreateResponse(HttpStatusCode.InternalServerError, new { success = false, message = "Failed to send data to the printer." });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Print receipt failed: {ex}");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new
                {
                    success = false,
                    message = "Print failed",
                    detail = ex.Message
                });
            }
        }

        [Route("print-barcode")]
        [HttpPost]
        public HttpResponseMessage PrintBarcode([FromBody] PrintBarcodeDto dto)
        {
            // اترك كود الباركود كما هو بدون تغيير لاعتماده على تصميم مختلف
            try
            {
                if (string.IsNullOrWhiteSpace(dto?.Barcode))
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new { success = false, message = "Missing barcode" });

                var escPos = _receiptService.BuildEscPosBarcode(dto.Barcode, dto.Name, dto.Price, dto.Count > 0 ? dto.Count : 1);
                var success = new Services.PrinterService().PrintBytes(PrinterName, System.Text.Encoding.ASCII.GetBytes(escPos));

                if (success) return Request.CreateResponse(HttpStatusCode.OK, new { success = true, message = "Barcode label(s) printed successfully" });
                else return Request.CreateResponse(HttpStatusCode.OK, new { success = false, message = $"Barcode print failed: could not open printer '{PrinterName}'" });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new { success = false, message = "Barcode print failed", detail = ex.Message });
            }
        }

        [Route("receipt")]
        [HttpPost]
        public HttpResponseMessage ReceiptText([FromBody] ReceiptTextDto dto) { /* يبقى كما هو */ return Request.CreateResponse(HttpStatusCode.OK); }

        [Route("barcode")]
        [HttpPost]
        public HttpResponseMessage BarcodeText([FromBody] BarcodeTextDto dto) { /* يبقى كما هو */ return Request.CreateResponse(HttpStatusCode.OK); }

        private List<ReceiptItem> ExtractItems(InvoiceData invoice)
        {
            var items = invoice.Items ?? new List<ReceiptItemData>();
            if (items.Count == 0 && invoice.InvoiceDetail != null)
            {
                items = invoice.InvoiceDetail.Select(d => new ReceiptItemData
                {
                    Name = d.Product?.Name ?? d.Name ?? "Item",
                    Quantity = d.Quantity,
                    SalePrice = d.SalePrice
                }).ToList();
            }
            return items.Select(i => new ReceiptItem
            {
                Name = i.Name ?? "Item",
                Quantity = i.Quantity,
                SalePrice = i.SalePrice
            }).ToList();
        }
    }

    public class PrintDto
    {
        public InvoiceData Invoice { get; set; }
    }

    public class PrintBarcodeDto
    {
        public string Barcode { get; set; }
        public string Name { get; set; }
        public double Price { get; set; }
        public int Count { get; set; }
    }

    public class ReceiptTextDto
    {
        public List<ReceiptTextItemDto> Items { get; set; }
        public double Total { get; set; }
        public double Discount { get; set; }
        public bool Arabic { get; set; }
    }

    public class ReceiptTextItemDto
    {
        public string Name { get; set; }
        public int Quantity { get; set; }
        public double SalePrice { get; set; }
    }

    public class BarcodeTextDto
    {
        public string Barcode { get; set; }
        public string ProductName { get; set; }
    }

    public class InvoiceData
    {
        public string Id { get; set; }
        public int InvoiceNumber { get; set; }
        public double TotalAmount { get; set; }
        public double Discount { get; set; }
        public DateTime? CreatedAt { get; set; }
        public List<ReceiptItemData> Items { get; set; }
        public List<InvoiceDetailData> InvoiceDetail { get; set; }
    }

    public class ReceiptItemData
    {
        public string Name { get; set; }
        public int Quantity { get; set; }
        public double SalePrice { get; set; }
    }

    public class InvoiceDetailData
    {
        public int Quantity { get; set; }
        public double SalePrice { get; set; }
        public ProductData Product { get; set; }
        public string Name { get; set; }
    }

    public class ProductData
    {
        public string Name { get; set; }
    }
}
