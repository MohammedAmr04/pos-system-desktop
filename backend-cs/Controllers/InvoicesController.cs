using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using PosCs.Api;
using PosCs.Application.Models;
using PosCs.Application.Services;
using PosCs.Attributes;
using PosCs.Domain.Exceptions;

namespace PosCs.Controllers
{
    [RoutePrefix("api/invoices")]
    public class InvoicesController : ApiController
    {
        private readonly InvoiceService _service = CompositionRoot.InvoiceService;

        [Route("")]
        [HttpGet]
        [RequirePermission("invoices.view")]
        public HttpResponseMessage GetAll()
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK, _service.GetToday());
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to fetch invoices: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to fetch invoices");
            }
        }

        [Route("filter")]
        [HttpGet]
        [RequirePermission("invoices.view")]
        public HttpResponseMessage GetFiltered(string from = null, string to = null)
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK, _service.GetFiltered(from, to));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to fetch filtered invoices: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to fetch filtered invoices");
            }
        }

        [Route("paged")]
        [HttpGet]
        [RequirePermission("invoices.view")]
        public HttpResponseMessage GetPaged(int page = 1, int pageSize = 20, string from = null, string to = null, string q = null)
        {
            try
            {
                var result = _service.GetPaged(page, pageSize, from, to, q);
                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    items = result.Items,
                    total = result.Total,
                    page,
                    pageSize,
                    totals = new { revenue = result.Revenue, discounts = result.Discounts }
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to fetch paged invoices: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to fetch paged invoices");
            }
        }

        [Route("{id}")]
        [HttpGet]
        [RequirePermission("invoices.view")]
        public HttpResponseMessage GetById(string id)
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK, _service.GetById(id));
            }
            catch (NotFoundException)
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Invoice not found");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to fetch invoice {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to fetch invoice");
            }
        }

        [Route("")]
        [HttpPost]
        [RequirePermission("invoices.create")]
        public HttpResponseMessage Create([FromBody] CreateInvoiceRequest dto)
        {
            try
            {
                var userId = Request.GetOwinContextUserId();
                var invoice = _service.Create(dto, userId);
                Console.WriteLine($"[API] Created invoice: {invoice.Id} (total: {invoice.TotalAmount}, discount: {invoice.DiscountAmount} {invoice.DiscountType}, mode: {invoice.PriceMode})");
                return Request.CreateResponse(HttpStatusCode.OK, invoice);
            }
            catch (InsufficientStockException ex)
            {
                Console.Error.WriteLine($"[API ERR] Invoice creation failed: {ex.Message}");
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message);
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to create invoice: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to create invoice");
            }
        }
    }
}
