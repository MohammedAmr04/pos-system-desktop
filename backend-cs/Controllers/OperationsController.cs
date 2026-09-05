using System;
using System.Net;
using System.Web.Http;
using PosCs.Api;
using PosCs.Application.Models;
using PosCs.Application.Services;
using PosCs.Attributes;

namespace PosCs.Controllers
{
    [RoutePrefix("api")]
    public class OperationsController : ApiController
    {
        private readonly OperationsService _service = CompositionRoot.OperationsService;
        [Route("audit-logs")][HttpGet][RequirePermission("audit.view")] public IHttpActionResult Audit(string action = null, string entityType = null, int page = 1, int pageSize = 20) { return Ok(_service.Audit(action, entityType, page, pageSize)); }
        [Route("alerts")][HttpGet][RequirePermission("alerts.view")] public IHttpActionResult Alerts(string status = "open", int page = 1, int pageSize = 20) { return Ok(_service.Alerts(status, page, pageSize)); }
        [Route("alerts/{id}/acknowledge")][HttpPost][RequirePermission("alerts.acknowledge")] public IHttpActionResult Acknowledge(string id) { return Ok(_service.Acknowledge(id, Request.GetOwinContextUserId())); }
        [Route("inventory-adjustments")][HttpGet][RequirePermission("inventory.adjustments.view")] public IHttpActionResult ListAdjustments(int page = 1, int pageSize = 20) { return Ok(_service.GetAdjustments(page, pageSize)); }
        [Route("inventory-adjustments")][HttpPost][RequirePermission("inventory.adjustments.create")] public IHttpActionResult CreateAdjustment([FromBody] CreateInventoryAdjustmentRequest dto) { return Execute(() => _service.CreateAdjustment(dto, Request.GetOwinContextUserId())); }
        [Route("inventory-adjustments/{id}")][HttpGet][RequirePermission("inventory.adjustments.view")] public IHttpActionResult GetAdjustment(string id) { return Execute(() => _service.GetAdjustment(id)); }
        [Route("inventory-adjustments/{id}/notes")][HttpPut][RequirePermission("inventory.adjustments.create")] public IHttpActionResult UpdateAdjustmentNotes(string id, [FromBody] UpdateInventoryAdjustmentNotesRequest dto) { return Execute(() => _service.UpdateAdjustmentNotes(id, dto, Request.GetOwinContextUserId())); }
        [Route("inventory-adjustments/{id}/lines")][HttpPut][RequirePermission("inventory.adjustments.create")] public IHttpActionResult SaveLine(string id, [FromBody] InventoryAdjustmentLineRequest dto) { return Execute(() => _service.SaveLine(id, dto, Request.GetOwinContextUserId())); }
        [Route("inventory-adjustments/{id}/lines/{lineId}")][HttpDelete][RequirePermission("inventory.adjustments.create")] public IHttpActionResult DeleteLine(string id, string lineId) { return Execute(() => { _service.DeleteLine(id, lineId, Request.GetOwinContextUserId()); return new { success = true }; }); }
        [Route("inventory-adjustments/{id}/post")][HttpPost][RequirePermission("inventory.adjustments.create")] public IHttpActionResult Post(string id) { return Execute(() => _service.Post(id, Request.GetOwinContextUserId())); }
        [Route("inventory-adjustments/{id}/cancel")][HttpPost][RequirePermission("inventory.adjustments.create")] public IHttpActionResult Cancel(string id) { return Execute(() => _service.Cancel(id, Request.GetOwinContextUserId())); }
        [Route("inventory-adjustments/{id}")][HttpDelete][RequirePermission("inventory.adjustments.create")] public IHttpActionResult DeleteAdjustment(string id) { return Execute(() => { _service.DeleteAdjustment(id, Request.GetOwinContextUserId()); return new { success = true }; }); }
        private IHttpActionResult Execute(Func<object> operation) { try { return Ok(operation()); } catch (Exception ex) { Console.Error.WriteLine("[API ERR] Operations request failed: " + ex); if (ApiErrors.IsHandled(ex)) return Content(HttpStatusCode.BadRequest, new { message = ex.Message }); return InternalServerError(); } }
    }
}
