using System;
using System.Net;
using System.Web.Http;
using PosCs.Api;
using PosCs.Application.Services;
using PosCs.Attributes;

namespace PosCs.Controllers
{
    [RoutePrefix("api/purchasereturns")]
    public class PurchaseReturnsController : ApiController
    {
        private readonly PurchaseReturnService _service = CompositionRoot.PurchaseReturnService;

        [HttpGet]
        [Route("")]
        [RequirePermission("purchases.view")]
        public IHttpActionResult GetPaged([FromUri] int page = 1, [FromUri] int pageSize = 20,
            [FromUri] string purchaseId = null)
        {
            try
            {
                var result = _service.GetPaged(page, pageSize, purchaseId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return Content(HttpStatusCode.BadRequest, new { message = ex.Message });
                Console.WriteLine($"[API ERR] Failed to list purchase returns: {ex}");
                return InternalServerError(new Exception("Failed to list purchase returns"));
            }
        }

        [HttpPost]
        [Route("")]
        [RequirePermission("purchases.return")]
        public IHttpActionResult Create([FromUri] string purchaseId, [FromBody] Application.Ports.CreatePurchaseReturnRequest dto)
        {
            try
            {
                var userId = Request.GetOwinContextUserId();
                var result = _service.Create(purchaseId, dto, userId);
                Console.WriteLine($"[API] Purchase return #{result.Number} created for purchase {purchaseId} refund={result.TotalAmount}");
                return Ok(result);
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return Content(HttpStatusCode.BadRequest, new { message = ex.Message });
                Console.WriteLine($"[API ERR] Failed to create purchase return: {ex}");
                return InternalServerError(new Exception("Failed to create purchase return"));
            }
        }
    }
}
