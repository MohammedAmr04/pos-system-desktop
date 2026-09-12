using System;
using System.Net;
using System.Web.Http;
using PosCs.Api;
using PosCs.Attributes;
using PosCs.Infrastructure.Persistence;

namespace PosCs.Controllers
{
    [RoutePrefix("api/backups")]
    public class BackupsController : ApiController
    {
        private readonly BackupService _service = CompositionRoot.BackupService;

        [HttpGet]
        [Route("")]
        [RequirePermission("backups.manage")]
        public IHttpActionResult List() { return Ok(_service.List()); }

        [HttpPost]
        [Route("")]
        [RequirePermission("backups.manage")]
        public IHttpActionResult Create()
        {
            try { return Ok(_service.Create(Request.GetOwinContextUserId())); }
            catch (Exception ex) { Console.Error.WriteLine("[API ERR] Backup failed: " + ex); return Content(HttpStatusCode.BadRequest, new { message = "Backup failed" }); }
        }

        [HttpPost]
        [Route("restore")]
        [RequirePermission("backups.manage")]
        public IHttpActionResult Restore([FromBody] PosCs.Application.Models.RestoreBackupRequest request)
        {
            try { return Ok(_service.Restore(request == null ? null : request.FileName, Request.GetOwinContextUserId())); }
            catch (Exception ex) { Console.Error.WriteLine("[API ERR] Restore failed: " + ex); return Content(HttpStatusCode.BadRequest, new { message = "Restore failed" }); }
        }
    }
}
