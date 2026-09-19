using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using PosCs.Api;
using PosCs.Application.Models;
using PosCs.Attributes;

namespace PosCs.Controllers
{
    [RoutePrefix("api/sync")]
    public class SyncController : ApiController
    {
        [Route("status")]
        [HttpGet]
        [RequirePermission]
        public HttpResponseMessage Status()
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK, CompositionRoot.SyncService.GetStatus());
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to load sync status: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to load sync status");
            }
        }

        [Route("pending")]
        [HttpGet]
        [RequirePermission]
        public HttpResponseMessage Pending(int limit = 50)
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK, CompositionRoot.SyncService.GetPending(limit));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to load pending sync operations: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to load pending sync operations");
            }
        }

        [Route("changes")]
        [HttpGet]
        [RequirePermission]
        public HttpResponseMessage Changes(long afterVersion = 0, int limit = 50)
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK, CompositionRoot.SyncService.GetChanges(afterVersion, limit));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to load sync changes: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to load sync changes");
            }
        }

        [Route("{id}/attempt")]
        [HttpPost]
        [RequirePermission]
        public HttpResponseMessage Attempt(string id, [FromBody] SyncAttemptRequest request)
        {
            try
            {
                CompositionRoot.SyncService.MarkAttempt(id, request == null ? "Sync failed" : request.Error);
                return Request.CreateResponse(HttpStatusCode.OK, new { success = true });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to mark sync attempt: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to mark sync attempt");
            }
        }

        [Route("{id}/ack")]
        [HttpPost]
        [RequirePermission]
        public HttpResponseMessage Acknowledge(string id)
        {
            try
            {
                CompositionRoot.SyncService.MarkSynced(id);
                return Request.CreateResponse(HttpStatusCode.OK, new { success = true });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to acknowledge sync operation: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to acknowledge sync operation");
            }
        }

        [Route("receive")]
        [HttpPost]
        [RequirePermission]
        public HttpResponseMessage Receive([FromBody] SyncPushRequest request)
        {
            try
            {
                CompositionRoot.SyncService.Receive(request == null ? null : request.Operations);
                return Request.CreateResponse(HttpStatusCode.OK, new { success = true });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to receive sync operations: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to receive sync operations");
            }
        }

        public sealed class SyncAttemptRequest
        {
            public string Error { get; set; }
        }
    }
}
