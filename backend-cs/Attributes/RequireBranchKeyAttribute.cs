using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using PosCs.Application.Services;

namespace PosCs.Attributes
{
    /// <summary>
    /// Enforces branch identity on central-facing sync endpoints. On the central node a valid
    /// <c>X-Branch-Key</c> for the <c>X-Branch-Id</c> branch is required; the authenticated branch
    /// id is exposed to the controller via the request property so it can reject cross-branch
    /// submissions. On a branch node (or until a branch is enrolled with a key) this is a no-op so
    /// existing single-branch and local traffic keeps working.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class RequireBranchKeyAttribute : Attribute, IAuthorizationFilter
    {
        public const string BranchIdProperty = "PosCs.SyncBranchId";

        public bool AllowMultiple => false;

        public Task<HttpResponseMessage> ExecuteAuthorizationFilterAsync(
            HttpActionContext actionContext,
            CancellationToken cancellationToken,
            Func<Task<HttpResponseMessage>> continuation)
        {
            if (!IsCentralNode())
                return continuation();

            var request = actionContext.Request;
            var branchId = Header(request, "X-Branch-Id");
            var key = Header(request, "X-Branch-Key");

            var state = CompositionRoot.BranchAuthService.VerifyKey(branchId, key);
            if (state == BranchSecretState.Invalid)
            {
                var denied = request.CreateResponse(HttpStatusCode.Unauthorized,
                    new { error = "Invalid branch key" });
                return Task.FromResult(denied);
            }

            if (state == BranchSecretState.Valid)
                request.Properties[BranchIdProperty] = branchId;

            return continuation();
        }

        private static string Header(HttpRequestMessage request, string name)
        {
            System.Collections.Generic.IEnumerable<string> values;
            return request.Headers.TryGetValues(name, out values) ? values.FirstOrDefault() : null;
        }

        private static bool IsCentralNode()
        {
            try
            {
                var context = CompositionRoot.BranchService.GetRuntimeContext();
                return context != null && string.Equals(context.NodeRole, "central", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}
