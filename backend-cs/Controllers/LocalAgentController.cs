using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using PosCs.Application.Models;
using PosCs.Application.Services;
using PosCs.Infrastructure.Config;

namespace PosCs.Controllers
{
    [RoutePrefix("api/local-agent")]
    public class LocalAgentController : ApiController
    {
        [Route("config")]
        [HttpGet]
        public HttpResponseMessage Config()
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK, CompositionRoot.BranchService.GetAgentConfig());
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to load local agent config: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to load agent config");
            }
        }

        [Route("sync/pending")]
        [HttpGet]
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

        [Route("sync/{id}/attempt")]
        [HttpPost]
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

        [Route("sync/{id}/ack")]
        [HttpPost]
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

        [Route("sync/apply")]
        [HttpPost]
        public HttpResponseMessage Apply([FromBody] SyncPushRequest request)
        {
            try
            {
                var result = CompositionRoot.SyncService.Receive(request == null ? null : request.Operations);
                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    success = result.Success,
                    applied = result.Applied,
                    failed = result.Failed
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to apply sync operations: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to apply sync operations");
            }
        }

        [Route("printing/queue/pending")]
        [HttpGet]
        public HttpResponseMessage QueuePending(int limit = 10)
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK, CompositionRoot.PrintQueueService.GetPending(limit));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to load pending print jobs: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to load print queue");
            }
        }

        [Route("printing/queue/{id}/printed")]
        [HttpPost]
        public HttpResponseMessage QueuePrinted(string id)
        {
            try
            {
                CompositionRoot.PrintQueueService.MarkPrinted(id);
                return Request.CreateResponse(HttpStatusCode.OK, new { success = true });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to mark print job printed: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to update print queue");
            }
        }

        [Route("printing/queue/{id}/failed")]
        [HttpPost]
        public HttpResponseMessage QueueFailed(string id, [FromBody] QueueFailureRequest request)
        {
            try
            {
                CompositionRoot.PrintQueueService.MarkAttempt(id, request == null ? "Print failed" : request.Error);
                return Request.CreateResponse(HttpStatusCode.OK, new { success = true });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to mark print job failed: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to update print queue");
            }
        }

        [Route("printing/print")]
        [HttpPost]
        public HttpResponseMessage Print([FromBody] PrintReceiptRequest dto)
        {
            if (!PosConfig.GetBool("POS_SERVER_PRINTING", true))
                return Request.CreateResponse(HttpStatusCode.ServiceUnavailable, new
                {
                    success = false,
                    message = "Server-side printing is disabled on this host."
                });

            try
            {
                var outcome = CompositionRoot.PrintingService.PrintReceipt(dto);
                return Request.CreateResponse((HttpStatusCode)outcome.Status, new
                {
                    success = outcome.Success,
                    message = outcome.Message,
                    detail = outcome.Detail
                });
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

        public sealed class SyncAttemptRequest
        {
            public string Error { get; set; }
        }

        public sealed class QueueFailureRequest
        {
            public string Error { get; set; }
        }
    }
}
