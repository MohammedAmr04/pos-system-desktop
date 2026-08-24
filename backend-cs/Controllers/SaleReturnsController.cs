using System;
using System.Net;
using System.Web.Http;
using PosCs.Api;
using PosCs.Application.Models;
using PosCs.Application.Services;
using PosCs.Attributes;

namespace PosCs.Controllers
{
    [RoutePrefix("api/salereturns")]
    public class SaleReturnsController : ApiController
    {
        private readonly SaleReturnService _service = CompositionRoot.SaleReturnService;

        [HttpGet]
        [Route("")]
        [RequirePermission("invoices.view")]
        public IHttpActionResult GetPaged([FromUri] int page = 1, [FromUri] int pageSize = 20,
            [FromUri] string invoiceId = null)
        {
            try
            {
                var result = _service.GetPaged(page, pageSize, invoiceId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return Content(HttpStatusCode.BadRequest, new { message = ex.Message });
                Console.WriteLine($"[API ERR] Failed to list sale returns: {ex}");
                return InternalServerError(new Exception("Failed to list sale returns"));
            }
        }

        [HttpPost]
        [Route("")]
        [RequirePermission("invoices.return")]
        public IHttpActionResult Create([FromUri] string invoiceId, [FromBody] CreateSaleReturnRequest dto)
        {
            try
            {
                var userId = Request.GetOwinContextUserId();
                var result = _service.Create(invoiceId, dto, userId);
                Console.WriteLine($"[API] Sale return #{result.Number} created for invoice {invoiceId} refund={result.TotalAmount}");
                return Ok(result);
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return Content(HttpStatusCode.BadRequest, new { message = ex.Message });
                Console.WriteLine($"[API ERR] Failed to create sale return: {ex}");
                return InternalServerError(new Exception("Failed to create sale return"));
            }
        }
    }
}
