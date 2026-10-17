using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using PosCs.Api;
using PosCs.Application.Models;
using PosCs.Attributes;
using PosCs.Domain.Exceptions;

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
        [RequireBranchKey]
        public HttpResponseMessage Changes(long afterVersion = 0, int limit = 50, string branchId = null)
        {
            try
            {
                var authenticatedBranch = AuthenticatedBranch();
                if (!string.IsNullOrEmpty(authenticatedBranch))
                    branchId = authenticatedBranch;
                return Request.CreateResponse(HttpStatusCode.OK, CompositionRoot.SyncService.GetChanges(afterVersion, limit, branchId));
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
        [RequireBranchKey]
        public HttpResponseMessage Receive([FromBody] SyncPushRequest request)
        {
            try
            {
                var operations = request == null ? null : request.Operations;
                CompositionRoot.BranchAuthService.EnsureOperationsBelongToBranch(operations, AuthenticatedBranch());
                var result = CompositionRoot.SyncService.Receive(operations);
                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    success = result.Success,
                    applied = result.Applied,
                    failed = result.Failed
                });
            }
            catch (DomainValidationException v)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, v.Message);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to receive sync operations: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to receive sync operations");
            }
        }

        private string AuthenticatedBranch()
        {
            object value;
            return Request.Properties.TryGetValue(RequireBranchKeyAttribute.BranchIdProperty, out value)
                ? value as string
                : null;
        }

        public sealed class SyncAttemptRequest
        {
            public string Error { get; set; }
        }
    }
}
