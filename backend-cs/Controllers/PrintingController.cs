using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using PosCs.Application.Models;
using PosCs.Application.Ports;
using PosCs.Attributes;
using PosCs.Application.Services;
using Newtonsoft.Json;

namespace PosCs.Controllers
{
    [RoutePrefix("api/printing")]
    public class PrintingController : ApiController
    {
        private readonly PrintingService _service = CompositionRoot.PrintingService;
        private readonly PrintQueueService _queue = CompositionRoot.PrintQueueService;

        [Route("print")]
        [HttpPost]
        [RequirePermission("printing.receipt", "receipt_printing")]
        public HttpResponseMessage Print([FromBody] PrintReceiptRequest dto)
        {
            try
            {
                var outcome = _service.PrintReceipt(dto);
                if (!outcome.Success && dto != null)
                    _queue.Enqueue("receipt", JsonConvert.SerializeObject(dto));
                return Request.CreateResponse((HttpStatusCode)outcome.Status, ToBody(outcome));
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

        [Route("queue/status")]
        [HttpGet]
        [RequirePermission]
        public HttpResponseMessage QueueStatus()
        {
            return Request.CreateResponse(HttpStatusCode.OK, _queue.GetStatus());
        }

        [Route("queue/pending")]
        [HttpGet]
        [RequirePermission]
        public HttpResponseMessage QueuePending(int limit = 20)
        {
            return Request.CreateResponse(HttpStatusCode.OK, _queue.GetPending(limit));
        }

        [Route("queue/{id}/printed")]
        [HttpPost]
        [RequirePermission]
        public HttpResponseMessage QueuePrinted(string id)
        {
            _queue.MarkPrinted(id);
            return Request.CreateResponse(HttpStatusCode.OK, new { success = true });
        }

        [Route("queue/{id}/failed")]
        [HttpPost]
        [RequirePermission]
        public HttpResponseMessage QueueFailed(string id, [FromBody] QueueFailureRequest request)
        {
            _queue.MarkAttempt(id, request == null ? "Print failed" : request.Error);
            return Request.CreateResponse(HttpStatusCode.OK, new { success = true });
        }

        [Route("print-barcode")]
        [HttpPost]
        [RequirePermission("printing.barcode", "barcode_printing")]
        public HttpResponseMessage PrintBarcode([FromBody] PrintBarcodeLabelRequest dto)
        {
            try
            {
                var outcome = _service.PrintBarcodeLabel(dto);
                return Request.CreateResponse((HttpStatusCode)outcome.Status, ToBody(outcome));
            }
            catch (Exception ex)
            {
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
        [RequirePermission("printing.receipt", "receipt_printing")]
        public HttpResponseMessage ReceiptText([FromBody] ReceiptTextRequest dto)
        {
            return Request.CreateResponse(HttpStatusCode.Gone, new { success = false, message = "This endpoint has been retired. Use /api/printing/print." });
        }

        [Route("barcode")]
        [HttpPost]
        [RequirePermission("printing.barcode", "barcode_printing")]
        public HttpResponseMessage BarcodeText([FromBody] BarcodeTextRequest dto)
        {
            return Request.CreateResponse(HttpStatusCode.Gone, new { success = false, message = "This endpoint has been retired. Use /api/printing/print-barcode." });
        }

        private static object ToBody(PrintOutcome outcome)
        {
            if (outcome.Detail == null)
                return new { success = outcome.Success, message = outcome.Message };
            return new { success = outcome.Success, message = outcome.Message, detail = outcome.Detail };
        }

        public sealed class QueueFailureRequest
        {
            public string Error { get; set; }
        }
    }
}
