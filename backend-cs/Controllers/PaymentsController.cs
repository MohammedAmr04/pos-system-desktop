using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using PosCs.Api;
using PosCs.Application.Ports;
using PosCs.Application.Services;
using PosCs.Attributes;

namespace PosCs.Controllers
{
    [RoutePrefix("api/payments")]
    public class PaymentsController : ApiController
    {
        private readonly PaymentService _service = CompositionRoot.PaymentService;

        [Route("")]
        [HttpGet]
        [RequirePermission("payments.view")]
        public HttpResponseMessage GetPaged([FromUri] int page = 1, [FromUri] int pageSize = 20,
            [FromUri] string clientId = null, [FromUri] string supplierId = null, [FromUri] string invoiceId = null)
        {
            try
            {
                var result = _service.GetPaged(clientId, supplierId, invoiceId, page, pageSize);
                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    items = result.Items,
                    total = result.Total,
                    page,
                    pageSize
                });
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to fetch payments: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to fetch payments");
            }
        }

        [Route("summary")]
        [HttpGet]
        [RequirePermission("payments.view")]
        public HttpResponseMessage GetSummary([FromUri] string ids)
        {
            try
            {
                var idList = (ids ?? "")
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Take(200)
                    .ToList();
                var paid = _service.GetPaidTotals(idList);
                return Request.CreateResponse(HttpStatusCode.OK, new { paid });
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to fetch payment summary: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to fetch payment summary");
            }
        }

        // NOTE: no PUT/DELETE — payments are append-only (spec §19); corrections are new payments.

        [Route("")]
        [HttpPost]
        [RequirePermission("payments.create")]
        public HttpResponseMessage Create([FromBody] CreatePaymentRequest dto)
        {
            try
            {
                var userId = Request.GetOwinContextUserId();
                var payment = _service.Create(dto, userId);
                Console.WriteLine($"[API] Recorded payment: {payment.Id} amount={payment.Amount} client={payment.ClientId} supplier={payment.SupplierId}");
                return Request.CreateResponse(HttpStatusCode.OK, payment);
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to record payment: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to record payment");
            }
        }
    }
}
