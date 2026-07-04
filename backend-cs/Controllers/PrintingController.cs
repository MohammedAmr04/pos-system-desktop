using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
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

                var escPos = _receiptService.BuildEscPosReceipt(
                    items,
                    invoice.TotalAmount,
                    invoice.Discount,
                    invoice.Id,
                    invoice.CreatedAt
                );

                var success = RawPrinterHelper.SendStringToPrinter(PrinterName, escPos);

                if (success)
                {
                    Console.WriteLine($"[API] Receipt printed for invoice: {invoice.Id}");
                    return Request.CreateResponse(HttpStatusCode.OK, new
                    {
                        success = true,
                        message = "Receipt printed successfully"
                    });
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.OK, new
                    {
                        success = false,
                        message = $"Print failed: could not open printer '{PrinterName}'"
                    });
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
            try
            {
                if (string.IsNullOrWhiteSpace(dto?.Barcode))
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new
                    {
                        success = false,
                        message = "Missing barcode"
                    });

                var escPos = _receiptService.BuildEscPosBarcode(
                    dto.Barcode,
                    dto.Name,
                    dto.Price,
                    dto.Count > 0 ? dto.Count : 1
                );

                var success = RawPrinterHelper.SendStringToPrinter(PrinterName, escPos);

                if (success)
                {
                    Console.WriteLine($"[API] Barcode label(s) printed for: {dto.Barcode}");
                    return Request.CreateResponse(HttpStatusCode.OK, new
                    {
                        success = true,
                        message = "Barcode label(s) printed successfully"
                    });
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.OK, new
                    {
                        success = false,
                        message = $"Barcode print failed: could not open printer '{PrinterName}'"
                    });
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Barcode print failed: {ex}");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new
                {
                    success = false,
                    message = "Barcode print failed",
                    detail = ex.Message
                });
            }
        }

        [Route("receipt")]
        [HttpPost]
        public HttpResponseMessage ReceiptText([FromBody] ReceiptTextDto dto)
        {
            try
            {
                if (dto?.Items == null || dto.Items.Count == 0)
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "No items provided");

                var receipt = _receiptService.BuildReceiptText(
                    dto.Items.Select(i => new ReceiptItem
                    {
                        Name = i.Name,
                        Quantity = i.Quantity,
                        SalePrice = i.SalePrice
                    }).ToList(),
                    dto.Total,
                    dto.Discount,
                    dto.Arabic
                );

                Console.WriteLine($"[API] Receipt text generated: {dto.Items.Count} items, total: {dto.Total}");
                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    success = true,
                    receipt
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Generate receipt text failed: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to generate receipt");
            }
        }

        [Route("barcode")]
        [HttpPost]
        public HttpResponseMessage BarcodeText([FromBody] BarcodeTextDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto?.Barcode))
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Barcode is required");

                var label = $"{dto.ProductName ?? ""}\n{dto.Barcode}".Trim();
                Console.WriteLine($"[API] Barcode text generated: {dto.Barcode}");
                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    success = true,
                    label
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Generate barcode text failed: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to generate barcode label");
            }
        }

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
        public string InvoiceNumber { get; set; }
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
