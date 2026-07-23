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

        [Route("")]
        [HttpPost]
        public HttpResponseMessage Create([FromBody] CreateInvoiceDto dto)
        {
            try
            {
                if (dto?.Items == null || dto.Items.Count == 0)
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "No items provided");

                var subtotal = dto.Items.Sum(i => i.SalePrice * i.Quantity);

                var discountType = dto.DiscountType;
                var discountValue = dto.DiscountValue;

                var eligibleItems = dto.Items.Where(i => i.AllowDiscount).ToList();
                var eligibleTotal = eligibleItems.Sum(i => i.SalePrice * i.Quantity);

                double discountAmount = 0;

                if (!string.IsNullOrEmpty(discountType) && discountValue > 0)
                {
                    if (discountType == "percentage")
                    {
                        if (discountValue < 0 || discountValue > 100)
                            return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Percentage must be between 0 and 100");
                        discountAmount = Math.Round(eligibleTotal * (discountValue / 100.0), 2);
                    }
                    else if (discountType == "fixed")
                    {
                        if (discountValue < 0)
                            return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Discount cannot be negative");
                        if (discountValue > eligibleTotal)
                            return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Discount exceeds eligible amount");
                        discountAmount = Math.Round(discountValue, 2);
                    }
                    else
                    {
                        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid discount type");
                    }
                }
                else
                {
                    discountAmount = Math.Round(Math.Max(0, dto.Discount), 2);
                    discountType = null;
                    discountValue = 0;
                }

                var lineDiscounts = new Dictionary<int, double>();
                if (discountAmount > 0 && eligibleTotal > 0)
                {
                    double distributed = 0;
                    for (int i = 0; i < dto.Items.Count; i++)
                    {
                        if (!dto.Items[i].AllowDiscount)
                        {
                            lineDiscounts[i] = 0;
                            continue;
                        }
                        var lineTotal = dto.Items[i].SalePrice * dto.Items[i].Quantity;
                        var share = Math.Round(discountAmount * (lineTotal / eligibleTotal), 2);
                        distributed += share;
                        lineDiscounts[i] = share;
                    }
                    if (distributed != discountAmount && eligibleItems.Count > 0)
                    {
                        var lastEligibleIdx = -1;
                        for (int i = dto.Items.Count - 1; i >= 0; i--)
                        {
                            if (dto.Items[i].AllowDiscount) { lastEligibleIdx = i; break; }
                        }
                        if (lastEligibleIdx >= 0)
                            lineDiscounts[lastEligibleIdx] += discountAmount - distributed;
                    }
                }
                else
                {
                    for (int i = 0; i < dto.Items.Count; i++)
                        lineDiscounts[i] = 0;
                }

                for (int i = 0; i < dto.Items.Count; i++)
                {
                    var item = dto.Items[i];
                    var lineDiscount = lineDiscounts[i];
                    var effectiveSalePrice = item.Quantity > 0
                        ? Math.Round((item.SalePrice * item.Quantity - lineDiscount) / item.Quantity, 2)
                        : item.SalePrice;
                    if (effectiveSalePrice < item.BuyPrice)
                    {
                        return Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                            $"Profit protection: '{item.Name}' would sell below cost price");
                    }
                }

                var totalAmount = Math.Max(0, subtotal - discountAmount);

                using (var conn = DbConnectionFactory.CreateConnection())
                {
                    var invoice = _repo.Create(conn, new Invoice
                    {
                        TotalAmount = totalAmount,
                        Discount = discountAmount,
                        DiscountType = discountType,
                        DiscountValue = discountValue,
                        DiscountAmount = discountAmount
                    }, dto.Items.Select((item, idx) => (item.ProductId, item.Quantity, item.BuyPrice, item.SalePrice, lineDiscounts[idx])).ToList());

                    Console.WriteLine($"[API] Created invoice: {invoice.Id} (total: {invoice.TotalAmount}, discount: {invoice.DiscountAmount} {invoice.DiscountType})");
                    return Request.CreateResponse(HttpStatusCode.OK, invoice);
                }
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
    }

    public class InvoiceItemDto
    {
        public string ProductId { get; set; }
        public string Name { get; set; }
        public double BuyPrice { get; set; }
        public double SalePrice { get; set; }
        public int Quantity { get; set; }
        public int MaxStock { get; set; }
        public bool AllowDiscount { get; set; } = true;
    }
}
