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

                var subtotal = dto.Items.Sum(i => i.SalePrice * i.Quantity);
                var totalAmount = Math.Max(0, subtotal - dto.Discount);

                using (var conn = DbConnectionFactory.CreateConnection())
                {
                    var invoice = _repo.Create(conn, new Invoice
                    {
                        TotalAmount = totalAmount,
                        Discount = dto.Discount
                    }, dto.Items.Select(i => (i.ProductId, i.Quantity, i.BuyPrice, i.SalePrice)).ToList());

                    Console.WriteLine($"[API] Created invoice: {invoice.Id} (total: {invoice.TotalAmount})");
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
    }

    public class InvoiceItemDto
    {
        public string ProductId { get; set; }
        public string Name { get; set; }
        public double BuyPrice { get; set; }
        public double SalePrice { get; set; }
        public int Quantity { get; set; }
        public int MaxStock { get; set; }
    }
}
