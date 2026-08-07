using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using PosCs.Helpers;
using PosCs.Models;
using PosCs.Repositories;

namespace PosCs.Controllers
{
    [RoutePrefix("api/invoices")]
    public class InvoicesController : ApiController
    {
        private readonly InvoiceRepository _repo = new InvoiceRepository();
        private readonly ProductRepository _productRepo = new ProductRepository();
        private readonly ProductUnitRepository _unitRepo = new ProductUnitRepository();

        [Route("")]
        [HttpGet]
        public HttpResponseMessage GetAll()
        {
            try
            {
                using (var conn = DbConnectionFactory.CreateConnection())
                {
                    var today = DateTime.Today;
                    var invoices = _repo.GetAll(conn, today, today.AddDays(1).AddSeconds(-1));
                    return Request.CreateResponse(HttpStatusCode.OK, invoices);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to fetch invoices: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to fetch invoices");
            }
        }

        [Route("filter")]
        [HttpGet]
        public HttpResponseMessage GetFiltered(string from = null, string to = null)
        {
            try
            {
                using (var conn = DbConnectionFactory.CreateConnection())
                {
                    DateTime? fromDate = null;
                    DateTime? toDate = null;

                    if (!string.IsNullOrEmpty(from))
                        fromDate = DateTime.Parse(from);

                    if (!string.IsNullOrEmpty(to))
                    {
                        toDate = DateTime.Parse(to).Date.AddDays(1).AddSeconds(-1);
                    }

                    if (!fromDate.HasValue && !toDate.HasValue)
                    {
                        fromDate = DateTime.Today;
                        toDate = DateTime.Today.AddDays(1).AddSeconds(-1);
                    }

                    var invoices = _repo.GetAll(conn, fromDate, toDate);
                    return Request.CreateResponse(HttpStatusCode.OK, invoices);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to fetch filtered invoices: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to fetch filtered invoices");
            }
        }

        [Route("paged")]
        [HttpGet]
        public HttpResponseMessage GetPaged(int page = 1, int pageSize = 20, string from = null, string to = null, string q = null)
        {
            try
            {
                page = Math.Max(1, page);
                pageSize = Math.Max(1, Math.Min(pageSize, 100));

                DateTime? fromDate = null;
                DateTime? toDate = null;

                if (!string.IsNullOrEmpty(from))
                    fromDate = DateTime.Parse(from);

                if (!string.IsNullOrEmpty(to))
                    toDate = DateTime.Parse(to).Date.AddDays(1).AddSeconds(-1);

                if (!fromDate.HasValue && !toDate.HasValue)
                {
                    fromDate = DateTime.Today;
                    toDate = DateTime.Today.AddDays(1).AddSeconds(-1);
                }

                using (var conn = DbConnectionFactory.CreateConnection())
                {
                    var result = _repo.GetPaged(conn, fromDate, toDate, q, page, pageSize);
                    return Request.CreateResponse(HttpStatusCode.OK, new
                    {
                        items = result.Items,
                        total = result.Total,
                        page,
                        pageSize,
                        totals = new { revenue = result.Revenue, discounts = result.Discounts }
                    });
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to fetch paged invoices: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to fetch paged invoices");
            }
        }

        [Route("{id}")]
        [HttpGet]
        public HttpResponseMessage GetById(string id)
        {
            try
            {
                using (var conn = DbConnectionFactory.CreateConnection())
                {
                    var invoice = _repo.GetById(conn, id);
                    if (invoice == null)
                        return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Invoice not found");
                    return Request.CreateResponse(HttpStatusCode.OK, invoice);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to fetch invoice {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to fetch invoice");
            }
        }

        [Route("")]
        [HttpPost]
        public HttpResponseMessage Create([FromBody] CreateInvoiceDto dto)
        {
            try
            {
                if (dto?.Items == null || dto.Items.Count == 0)
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "No items provided");

                var priceMode = string.Equals(dto.PriceMode, "wholesale", StringComparison.OrdinalIgnoreCase)
                    ? "wholesale"
                    : "retail";

                using (var conn = DbConnectionFactory.CreateConnection())
                {
                    var lineDetails = new List<InvoiceDetail>();
                    var lineFinals = new List<double>();

                    for (int i = 0; i < dto.Items.Count; i++)
                    {
                        var item = dto.Items[i];
                        if (item.Quantity <= 0)
                            return Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                                $"Quantity must be greater than zero for '{item.Name}'");

                        var product = _productRepo.GetById(conn, item.ProductId);
                        if (product == null)
                            return Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                                $"Product not found: '{item.Name}'");

                        var unit = _unitRepo.GetById(conn, item.ProductUnitId);
                        if (unit == null || unit.ProductId != product.Id)
                            unit = _unitRepo.GetBaseUnit(conn, product.Id);
                        if (unit == null)
                            return Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                                $"No selling unit found for product '{product.Name}'");

                        var originalPrice = unit.RetailPrice;
                        if (priceMode == "wholesale" && unit.WholesalePrice.HasValue && unit.WholesalePrice.Value > 0)
                            originalPrice = unit.WholesalePrice.Value;

                        var unitPrice = item.UnitPrice > 0 ? item.UnitPrice : originalPrice;
                        if (unitPrice < 0)
                            return Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                                $"Unit price cannot be negative for '{product.Name}'");

                        string lineDiscountType = null;
                        double lineDiscountValue = 0;
                        double lineDiscountAmount = 0;

                        if (!string.IsNullOrEmpty(item.DiscountType) && item.DiscountValue > 0)
                        {
                            var lineSubtotalBefore = unitPrice * item.Quantity;
                            if (item.DiscountType == "percentage")
                            {
                                if (item.DiscountValue < 0 || item.DiscountValue > 100)
                                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                                        "Line discount percentage must be between 0 and 100");
                                lineDiscountType = "percentage";
                                lineDiscountValue = item.DiscountValue;
                                lineDiscountAmount = Math.Round(lineSubtotalBefore * (item.DiscountValue / 100.0), 2);
                            }
                            else if (item.DiscountType == "fixed")
                            {
                                if (item.DiscountValue < 0)
                                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                                        "Line discount cannot be negative");
                                if (item.DiscountValue > lineSubtotalBefore)
                                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                                        $"Line discount cannot exceed line total for '{product.Name}'");
                                lineDiscountType = "fixed";
                                lineDiscountValue = item.DiscountValue;
                                lineDiscountAmount = Math.Round(item.DiscountValue, 2);
                            }
                            else
                            {
                                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid line discount type");
                            }
                        }

                        var lineSubtotal = Math.Round(unitPrice * item.Quantity, 2);
                        var lineFinal = Math.Round(lineSubtotal - lineDiscountAmount, 2);

                        string priceEditNote = null;
                        if (unitPrice != originalPrice && !string.IsNullOrWhiteSpace(item.PriceEditNote))
                            priceEditNote = item.PriceEditNote.Trim();

                        lineDetails.Add(new InvoiceDetail
                        {
                            ProductId = product.Id,
                            ProductUnitId = unit.Id,
                            UnitName = unit.UnitName,
                            Quantity = item.Quantity,
                            BuyPrice = product.BuyPrice,
                            OriginalUnitPrice = Math.Round(originalPrice, 2),
                            UnitPrice = Math.Round(unitPrice, 2),
                            DiscountType = lineDiscountType,
                            DiscountValue = lineDiscountValue,
                            DiscountAmount = lineDiscountAmount,
                            LineSubtotal = lineSubtotal,
                            FinalTotal = lineFinal,
                            QuantityFactor = unit.QuantityFactor,
                            PriceEditNote = priceEditNote,
                            Product = product
                        });
                        lineFinals.Add(item.AllowDiscount ? lineFinal : 0);
                    }

                    var eligibleTotal = lineFinals.Sum();

                    double invoiceDiscountAmount = 0;
                    var discountType = dto.DiscountType;
                    var discountValue = dto.DiscountValue;

                    if (!string.IsNullOrEmpty(discountType) && discountValue > 0)
                    {
                        if (discountType == "percentage")
                        {
                            if (discountValue < 0 || discountValue > 100)
                                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Percentage must be between 0 and 100");
                            invoiceDiscountAmount = Math.Round(eligibleTotal * (discountValue / 100.0), 2);
                        }
                        else if (discountType == "fixed")
                        {
                            if (discountValue < 0)
                                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Discount cannot be negative");
                            if (discountValue > eligibleTotal)
                                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Discount exceeds eligible amount");
                            invoiceDiscountAmount = Math.Round(discountValue, 2);
                        }
                        else
                        {
                            return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid discount type");
                        }
                    }
                    else
                    {
                        invoiceDiscountAmount = Math.Round(Math.Max(0, dto.Discount), 2);
                        discountType = null;
                        discountValue = 0;
                    }

                    var invoiceShares = new double[lineDetails.Count];
                    if (invoiceDiscountAmount > 0 && eligibleTotal > 0)
                    {
                        double distributed = 0;
                        for (int i = 0; i < lineDetails.Count; i++)
                        {
                            if (!dto.Items[i].AllowDiscount)
                            {
                                invoiceShares[i] = 0;
                                continue;
                            }
                            var share = Math.Round(invoiceDiscountAmount * (lineDetails[i].FinalTotal / eligibleTotal), 2);
                            distributed += share;
                            invoiceShares[i] = share;
                        }
                        if (distributed != invoiceDiscountAmount)
                        {
                            var lastEligibleIdx = -1;
                            for (int i = lineDetails.Count - 1; i >= 0; i--)
                            {
                                if (dto.Items[i].AllowDiscount) { lastEligibleIdx = i; break; }
                            }
                            if (lastEligibleIdx >= 0)
                                invoiceShares[lastEligibleIdx] += invoiceDiscountAmount - distributed;
                        }
                    }

                    for (int i = 0; i < lineDetails.Count; i++)
                    {
                        var detail = lineDetails[i];
                        var totalLineDiscount = detail.DiscountAmount + invoiceShares[i];
                        var effectivePrice = detail.Quantity > 0
                            ? Math.Round((detail.LineSubtotal - totalLineDiscount) / detail.Quantity, 2)
                            : detail.UnitPrice;
                        if (effectivePrice < detail.BuyPrice)
                        {
                            return Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                                $"Profit protection: '{detail.Product.Name}' would sell below cost price");
                        }
                    }

                    double totalAmount = 0;
                    for (int i = 0; i < lineDetails.Count; i++)
                    {
                        lineDetails[i].DiscountAmount = Math.Round(lineDetails[i].DiscountAmount + invoiceShares[i], 2);
                        lineDetails[i].FinalTotal = Math.Round(lineDetails[i].LineSubtotal - lineDetails[i].DiscountAmount, 2);
                        totalAmount += lineDetails[i].FinalTotal;
                    }
                    totalAmount = Math.Round(totalAmount, 2);

                    var invoice = _repo.Create(conn, new Invoice
                    {
                        TotalAmount = totalAmount,
                        Discount = invoiceDiscountAmount,
                        DiscountType = discountType,
                        DiscountValue = discountValue,
                        DiscountAmount = invoiceDiscountAmount,
                        PriceMode = priceMode
                    }, lineDetails);

                    Console.WriteLine($"[API] Created invoice: {invoice.Id} (total: {invoice.TotalAmount}, discount: {invoice.DiscountAmount} {invoice.DiscountType}, mode: {priceMode})");
                    return Request.CreateResponse(HttpStatusCode.OK, invoice);
                }
            }
            catch (InsufficientStockException ex)
            {
                Console.Error.WriteLine($"[API ERR] Invoice creation failed: {ex.Message}");
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to create invoice: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to create invoice");
            }
        }
    }

    public class CreateInvoiceDto
    {
        public List<InvoiceItemDto> Items { get; set; }
        public double Discount { get; set; }
        public string DiscountType { get; set; }
        public double DiscountValue { get; set; }
        public string PriceMode { get; set; }
    }

    public class InvoiceItemDto
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
