using System;
using System.Net;
using System.Web.Http;
using PosCs.Api;
using PosCs.Application.Ports;
using PosCs.Application.Services;
using PosCs.Attributes;

namespace PosCs.Controllers
{
    [RoutePrefix("api/shifts")]
    public class ShiftsController : ApiController
    {
        private readonly ShiftService _service = CompositionRoot.ShiftService;

        [Route("")]
        [HttpGet]
        [RequirePermission("shifts.view")]
        public IHttpActionResult GetPaged([FromUri] string status = null, [FromUri] int page = 1,
            [FromUri] int pageSize = 20)
        {
            try
            {
                return Ok(_service.GetPaged(status, page, pageSize));
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return Content(HttpStatusCode.BadRequest, new { message = ex.Message });
                Console.WriteLine($"[API ERR] Failed to list shifts: {ex}");
                return InternalServerError(new Exception("Failed to list shifts"));
            }
        }

        /// <summary>The currently open shift, or 204 when none is active.</summary>
        [Route("active")]
        [HttpGet]
        [RequirePermission("shifts.view")]
        public IHttpActionResult GetActive()
        {
            try
            {
                var shift = _service.GetActive();
                if (shift == null) return StatusCode(HttpStatusCode.NoContent);
                return Ok(shift);
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return Content(HttpStatusCode.BadRequest, new { message = ex.Message });
                Console.WriteLine($"[API ERR] Failed to fetch active shift: {ex}");
                return InternalServerError(new Exception("Failed to fetch active shift"));
            }
        }

        [Route("{id}")]
        [HttpGet]
        [RequirePermission("shifts.view")]
        public IHttpActionResult GetById(string id)
        {
            try
            {
                return Ok(_service.GetById(id));
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return Content(HttpStatusCode.BadRequest, new { message = ex.Message });
                Console.WriteLine($"[API ERR] Failed to fetch shift {id}: {ex}");
                return InternalServerError(new Exception("Failed to fetch shift"));
            }
        }

        [Route("")]
        [HttpPost]
        [RequirePermission("shifts.open")]
        public IHttpActionResult Open([FromBody] OpenShiftRequest dto)
        {
            try
            {
                var userId = Request.GetOwinContextUserId();
                var shift = _service.Open(dto, userId);
                Console.WriteLine($"[API] Shift #{shift.Number} opened by {userId} float={shift.OpeningCash}");
                return Ok(shift);
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return Content(HttpStatusCode.BadRequest, new { message = ex.Message });
                Console.WriteLine($"[API ERR] Failed to open shift: {ex}");
                return InternalServerError(new Exception("Failed to open shift"));
            }
        }

        [Route("{id}/close")]
        [HttpPost]
        [RequirePermission("shifts.close")]
        public IHttpActionResult Close(string id, [FromBody] CloseShiftRequest dto)
        {
            try
            {
                var shift = _service.Close(id, dto);
                Console.WriteLine($"[API] Shift #{shift.Number} closed expected={shift.ExpectedCash} counted={shift.CountedCash} diff={shift.Difference}");
                return Ok(shift);
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return Content(HttpStatusCode.BadRequest, new { message = ex.Message });
                Console.WriteLine($"[API ERR] Failed to close shift {id}: {ex}");
                return InternalServerError(new Exception("Failed to close shift"));
            }
        }

        [Route("{id}/report")]
        [HttpGet]
        [RequirePermission("shifts.view")]
        public IHttpActionResult GetReport(string id)
        {
            try
            {
                return Ok(_service.GetReport(id));
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return Content(HttpStatusCode.BadRequest, new { message = ex.Message });
                Console.WriteLine($"[API ERR] Failed to build shift report {id}: {ex}");
                return InternalServerError(new Exception("Failed to build shift report"));
            }
        }

        [Route("{id}/invoices")]
        [HttpGet]
        [RequirePermission("shifts.view")]
        public IHttpActionResult GetInvoices(string id, [FromUri] int page = 1, [FromUri] int pageSize = 20)
        {
            try
            {
                return Ok(_service.GetShiftInvoices(id, page, pageSize));
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return Content(HttpStatusCode.BadRequest, new { message = ex.Message });
                Console.WriteLine($"[API ERR] Failed to list shift invoices {id}: {ex}");
                return InternalServerError(new Exception("Failed to list shift invoices"));
            }
        }
    }
}
