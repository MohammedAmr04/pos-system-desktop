using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using PosCs.Application.Models;
using PosCs.Application.Ports;
using PosCs.Attributes;
using PosCs.Application.Services;
using PosCs.Infrastructure.Config;

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
            if (!ServerPrintingEnabled())
                return PrintingDisabledResponse();

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
            if (!ServerPrintingEnabled())
                return PrintingDisabledResponse();

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

        private static bool ServerPrintingEnabled()
        {
            return PosConfig.GetBool("POS_SERVER_PRINTING", true);
        }

        private HttpResponseMessage PrintingDisabledResponse()
        {
            return Request.CreateResponse(HttpStatusCode.ServiceUnavailable, new
            {
                success = false,
                message = "Server-side printing is disabled on this host. Print from the browser or a local print agent."
            });
        }

        private static object ToBody(PrintOutcome outcome)
        {
            if (outcome.Detail == null)
                return new { success = outcome.Success, message = outcome.Message };
            return new { success = outcome.Success, message = outcome.Message, detail = outcome.Detail };
        }
    }
}
