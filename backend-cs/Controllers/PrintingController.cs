using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using PosCs.Application.Models;
using PosCs.Application.Ports;
using PosCs.Attributes;
using PosCs.Application.Services;

namespace PosCs.Controllers
{
    [RoutePrefix("api/printing")]
    public class PrintingController : ApiController
    {
        private readonly PrintingService _service = CompositionRoot.PrintingService;

        [Route("print")]
        [HttpPost]
        [RequirePermission("printing.receipt", "receipt_printing")]
        public HttpResponseMessage Print([FromBody] PrintReceiptRequest dto)
        {
            try
            {
                var outcome = _service.PrintReceipt(dto);
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
        public HttpResponseMessage ReceiptText([FromBody] ReceiptTextRequest dto) { /* legacy stub */ return Request.CreateResponse(HttpStatusCode.OK); }

        [Route("barcode")]
        [HttpPost]
        [RequirePermission("printing.barcode", "barcode_printing")]
        public HttpResponseMessage BarcodeText([FromBody] BarcodeTextRequest dto) { /* legacy stub */ return Request.CreateResponse(HttpStatusCode.OK); }

        private static object ToBody(PrintOutcome outcome)
        {
            if (outcome.Detail == null)
                return new { success = outcome.Success, message = outcome.Message };
            return new { success = outcome.Success, message = outcome.Message, detail = outcome.Detail };
        }
    }
}
