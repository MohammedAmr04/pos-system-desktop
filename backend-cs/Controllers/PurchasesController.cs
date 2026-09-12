using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using PosCs.Api;
using PosCs.Application.Ports;
using PosCs.Application.Services;
using PosCs.Attributes;

namespace PosCs.Controllers
{
    [RoutePrefix("api/purchases")]
    public class PurchasesController : ApiController
    {
        private readonly PurchaseService _service = CompositionRoot.PurchaseService;

        [Route("")]
        [HttpGet]
        [RequirePermission("purchases.view")]
        public HttpResponseMessage GetPaged([FromUri] string status = null, [FromUri] int page = 1,
            [FromUri] int pageSize = 20, [FromUri] string q = null)
        {
            try
            {
                var result = _service.GetPaged(status, q, page, pageSize);
                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    items = result.Items,
                    total = result.Total,
                    postedTotal = result.PostedTotal,
                    page,
                    pageSize
                });
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to fetch purchases (paged): {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to fetch purchases");
            }
        }

        [Route("{id}")]
        [HttpGet]
        [RequirePermission("purchases.view")]
        public HttpResponseMessage GetById(string id)
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK, _service.GetById(id));
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to fetch purchase {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to fetch purchase");
            }
        }

        /// <summary>Creates a draft, or saves+posts in one call when status = 'posted'.</summary>
        [Route("")]
        [HttpPost]
        [RequirePermission("purchases.create")]
        public HttpResponseMessage Create([FromBody] SavePurchaseRequest dto)
        {
            try
            {
                var invoice = _service.Save(dto, Request.GetOwinContextUserId());
                Console.WriteLine($"[API] Saved purchase {invoice.InvoiceNumber} ({invoice.Status})");
                return Request.CreateResponse(HttpStatusCode.OK, invoice);
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to save purchase: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to save purchase");
            }
        }

        /// <summary>Edits a draft, or edits a posted invoice via transactional reversal + re-application.</summary>
        [Route("{id}")]
        [HttpPut]
        [RequirePermission("purchases.update")]
        public HttpResponseMessage Update(string id, [FromBody] SavePurchaseRequest dto)
        {
            try
            {
                var invoice = _service.Update(id, dto);
                Console.WriteLine($"[API] Updated purchase {invoice.InvoiceNumber} ({invoice.Status})");
                return Request.CreateResponse(HttpStatusCode.OK, invoice);
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to update purchase {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to update purchase");
            }
        }

        [Route("{id}/post")]
        [HttpPost]
        [RequirePermission("purchases.update")]
        public HttpResponseMessage Post(string id)
        {
            try
            {
                var invoice = _service.Post(id);
                Console.WriteLine($"[API] Posted purchase {invoice.InvoiceNumber}");
                return Request.CreateResponse(HttpStatusCode.OK, invoice);
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to post purchase {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to post purchase");
            }
        }

        // NOTE: no DELETE — cancellation appends reversal rows instead of deleting history (spec §12.3).

        [Route("{id}/cancel")]
        [HttpPost]
        [RequirePermission("purchases.update")]
        public HttpResponseMessage Cancel(string id)
        {
            try
            {
                var invoice = _service.Cancel(id);
                Console.WriteLine($"[API] Cancelled purchase {invoice.InvoiceNumber}");
                return Request.CreateResponse(HttpStatusCode.OK, invoice);
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to cancel purchase {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to cancel purchase");
            }
        }
    }
}
