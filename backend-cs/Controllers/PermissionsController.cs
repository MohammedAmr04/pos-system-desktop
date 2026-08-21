using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using PosCs.Attributes;

namespace PosCs.Controllers
{
    [RoutePrefix("api/permissions")]
    public class PermissionsController : ApiController
    {
        // Permissions are system-defined (seeded via migration) — read-only by design.
        [Route("")]
        [HttpGet]
        [RequirePermission("roles.manage")]
        public HttpResponseMessage GetAll()
        {
            try
            {
                var permissions = CompositionRoot.PermissionsRepo.GetAll();
                return Request.CreateResponse(HttpStatusCode.OK, permissions.Select(p => new
                {
                    id = p.Id,
                    key = p.Key,
                    name = p.Name,
                    description = p.Description,
                    resource = p.Resource,
                    action = p.Action
                }));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to fetch permissions: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to fetch permissions");
            }
        }
    }
}
