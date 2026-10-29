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
    [RoutePrefix("api/branch-sync")]
    [RequireBranchKey(Strict = true)]
    public class BranchSyncController : ApiController
    {
        [Route("changes")]
        [HttpGet]
        public HttpResponseMessage Changes(long afterVersion = 0, int limit = 50)
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK,
                    CompositionRoot.SyncService.GetChanges(afterVersion, limit, AuthenticatedBranch()));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to load branch sync changes: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to load sync changes");
            }
        }

        [Route("receive")]
        [HttpPost]
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
                Console.Error.WriteLine($"[API ERR] Failed to receive branch sync operations: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to receive sync operations");
            }
        }

        [Route("switch")]
        [HttpPost]
        public HttpResponseMessage Switch([FromBody] BranchSwitchRequest request)
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK,
                    CompositionRoot.BranchAssignmentService.CentralSwitch(AuthenticatedBranch(), "branch-agent", request));
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to switch branch: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to switch branch");
            }
        }

        [Route("switch/{requestId}")]
        [HttpGet]
        public HttpResponseMessage SwitchStatus(string requestId)
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK,
                    CompositionRoot.BranchAssignmentService.GetCentralAssignmentForBranch(requestId, AuthenticatedBranch()));
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to load branch switch status: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to load branch switch status");
            }
        }

        private string AuthenticatedBranch()
        {
            object value;
            return Request.Properties.TryGetValue(RequireBranchKeyAttribute.BranchIdProperty, out value)
                ? value as string
                : null;
        }
    }
}
