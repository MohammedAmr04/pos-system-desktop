using System;
using System.Net;
using System.Web.Http;
using PosCs.Api;
using PosCs.Application.Services;
using PosCs.Attributes;
using PosCs.Domain.Entities;

namespace PosCs.Controllers
{
    [RoutePrefix("api/settings/printing")]
    public class PrinterSettingsController : ApiController
    {
        private readonly PrinterSettingsService _service = CompositionRoot.PrinterSettingsService;

        [Route("")]
        [HttpGet]
        [RequirePermission("settings.view")]
        public IHttpActionResult Get()
        {
            try
            {
                return Ok(_service.Get());
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to load printer settings: {ex}");
                return InternalServerError(new Exception("Failed to load printer settings"));
            }
        }

        [Route("")]
        [HttpPut]
        [RequirePermission("settings.update")]
        public IHttpActionResult Save([FromBody] PrinterSettings dto)
        {
            try
            {
                var saved = _service.Save(dto);
                Console.WriteLine($"[API] Printer settings saved receipt='{saved.ReceiptPrinterName}' label='{saved.LabelPrinterName}' width={saved.PaperWidthMm}mm copies={saved.Copies}");
                return Ok(saved);
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return Content(HttpStatusCode.BadRequest, new { message = ex.Message });
                Console.Error.WriteLine($"[API ERR] Failed to save printer settings: {ex}");
                return InternalServerError(new Exception("Failed to save printer settings"));
            }
        }
    }
}
