using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using PosCs.Api;
using PosCs.Application.Models;
using PosCs.Attributes;

namespace PosCs.Controllers
{
    [RoutePrefix("api/branch")]
    public class BranchController : ApiController
    {
        [Route("context")]
        [HttpGet]
        [RequirePermission]
        public HttpResponseMessage Context()
        {
            try
            {
                var context = CompositionRoot.BranchService.GetRuntimeContext();
                return Request.CreateResponse(HttpStatusCode.OK, context);
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to load branch context: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to load branch context");
            }
        }

        [Route("agent-config")]
        [HttpGet]
        [RequirePermission]
        public HttpResponseMessage AgentConfig()
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK, CompositionRoot.BranchService.GetAgentConfig());
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to load agent config: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to load agent config");
            }
        }

        [Route("list")]
        [HttpGet]
        [RequirePermission("branches.view")]
        public HttpResponseMessage List(bool activeOnly = true)
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK, CompositionRoot.BranchService.GetAll(activeOnly));
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to list branches: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to list branches");
            }
        }

        [Route("paged")]
        [HttpGet]
        [RequirePermission("branches.view")]
        public HttpResponseMessage Paged(int page = 1, int pageSize = 20, string q = null, bool activeOnly = true)
        {
            try
            {
                var result = CompositionRoot.BranchService.GetPaged(page, pageSize, q, activeOnly);
                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    items = result.Items,
                    total = result.Total,
                    page,
                    pageSize
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to page branches: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to page branches");
            }
        }

        [Route("{id}")]
        [HttpGet]
        [RequirePermission("branches.view")]
        public HttpResponseMessage GetById(string id)
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK, CompositionRoot.BranchService.GetById(id));
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to load branch {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to load branch");
            }
        }

        [Route("{id}/terminals")]
        [HttpGet]
        [RequirePermission("branches.view")]
        public HttpResponseMessage Terminals(string id)
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK, CompositionRoot.BranchService.GetTerminals(id));
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to load branch terminals {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to load branch terminals");
            }
        }

        [Route("")]
        [HttpPost]
        [RequirePermission("branches.manage")]
        public HttpResponseMessage Create([FromBody] CreateBranchRequest request)
        {
            try
            {
                var branch = CompositionRoot.BranchService.Create(request);
                Console.WriteLine($"[API] Created branch: {branch.Id} ({branch.Code})");
                return Request.CreateResponse(HttpStatusCode.OK, branch);
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to create branch: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to create branch");
            }
        }

        [Route("{id}")]
        [HttpPut]
        [RequirePermission("branches.manage")]
        public HttpResponseMessage Update(string id, [FromBody] UpdateBranchRequest request)
        {
            try
            {
                var branch = CompositionRoot.BranchService.Update(id, request);
                Console.WriteLine($"[API] Updated branch: {branch.Id} ({branch.Code})");
                return Request.CreateResponse(HttpStatusCode.OK, branch);
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to update branch {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to update branch");
            }
        }

        [Route("{id}/activate")]
        [HttpPost]
        [RequirePermission("branches.manage")]
        public HttpResponseMessage Activate(string id)
        {
            return SetActive(id, true);
        }

        [Route("{id}/deactivate")]
        [HttpPost]
        [RequirePermission("branches.manage")]
        public HttpResponseMessage Deactivate(string id)
        {
            return SetActive(id, false);
        }

        [Route("{id}")]
        [HttpDelete]
        [RequirePermission("branches.manage")]
        public HttpResponseMessage Delete(string id)
        {
            try
            {
                CompositionRoot.BranchService.Delete(id);
                Console.WriteLine($"[API] Deleted branch: {id}");
                return Request.CreateResponse(HttpStatusCode.OK, new { success = true });
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to delete branch {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to delete branch");
            }
        }

        [Route("{id}/secret")]
        [HttpPost]
        [RequirePermission("branches.manage")]
        public HttpResponseMessage SetSecret(string id, [FromBody] BranchSecretRequest request)
        {
            try
            {
                if (request == null) return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid branch secret");
                CompositionRoot.BranchAuthService.SetSecret(id, request.Key);
                return Request.CreateResponse(HttpStatusCode.OK, new { success = true });
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to set branch secret: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to set branch secret");
            }
        }

        [Route("credentials")]
        [HttpPost]
        [RequirePermission("branches.manage")]
        public HttpResponseMessage Credentials([FromBody] BranchCredentialsRequest request)
        {
            try
            {
                if (request == null) return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid branch credentials");
                CompositionRoot.BranchService.SaveCredentials(request.CentralToken, request.CentralBranchKey);
                return Request.CreateResponse(HttpStatusCode.OK, new { success = true });
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to save branch credentials: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to save branch credentials");
            }
        }

        [Route("runtime")]
        [HttpPost]
        [RequirePermission("branches.manage")]
        public HttpResponseMessage Configure([FromBody] BranchRuntimeConfigurationRequest request)
        {
            try
            {
                if (request == null) return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid branch configuration");
                CompositionRoot.BranchService.Configure(request.BranchId, request.TerminalId, request.NodeRole, request.CentralBaseUrl);
                return Request.CreateResponse(HttpStatusCode.OK, CompositionRoot.BranchService.GetRuntimeContext());
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to configure branch runtime: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to configure branch runtime");
            }
        }

        [Route("switch")]
        [HttpPost]
        [RequirePermission("branches.manage")]
        public HttpResponseMessage Switch([FromBody] BranchSwitchRequest request)
        {
            try
            {
                var userId = Request.GetOwinContextUserId();
                var context = CompositionRoot.BranchService.GetRuntimeContext();
                var isCentral = context != null && string.Equals(context.NodeRole, "central", StringComparison.OrdinalIgnoreCase);
                var result = isCentral
                    ? CompositionRoot.BranchAssignmentService.CentralSwitch(AuthenticatedBranch(), userId, request)
                    : CompositionRoot.BranchAssignmentService.LocalSwitch(userId, request);
                Console.WriteLine($"[API] Branch switch {(isCentral ? "accepted (central)" : "requested")}: terminal {result.TerminalId} -> {result.BranchId}");
                return Request.CreateResponse(HttpStatusCode.OK, result);
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
        [RequirePermission("branches.manage")]
        public HttpResponseMessage SwitchStatus(string requestId)
        {
            try
            {
                var context = CompositionRoot.BranchService.GetRuntimeContext();
                var isCentral = context != null && string.Equals(context.NodeRole, "central", StringComparison.OrdinalIgnoreCase);
                var result = isCentral
                    ? CompositionRoot.BranchAssignmentService.GetCentralAssignment(requestId)
                    : CompositionRoot.BranchAssignmentService.Reconcile(requestId);
                return Request.CreateResponse(HttpStatusCode.OK, result);
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to load branch switch status: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to load branch switch status");
            }
        }

        private HttpResponseMessage SetActive(string id, bool isActive)
        {
            try
            {
                CompositionRoot.BranchService.SetActive(id, isActive);
                return Request.CreateResponse(HttpStatusCode.OK, new { success = true });
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to update branch state: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to update branch state");
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
