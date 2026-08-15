using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using PosCs.Attributes;
using PosCs.Helpers;
using PosCs.Repositories;
using PosCs.Services;

namespace PosCs.Controllers
{
    [RoutePrefix("api/license")]
    public class LicenseController : ApiController
    {
        private readonly SettingsRepository _settingsRepo = new SettingsRepository();

        [Route("")]
        [HttpGet]
        public HttpResponseMessage Get()
        {
            try
            {
                var machineId = MachineId.GetMachineId();

                using (var conn = DbConnectionFactory.CreateConnection())
                {
                    var settings = _settingsRepo.GetByMachineId(conn, machineId);

                    if (settings == null)
                    {
                        _settingsRepo.Create(conn, machineId, false);
                        return Request.CreateResponse(HttpStatusCode.OK, new
                        {
                            status = "first_boot",
                            machineId
                        });
                    }

                    if (!settings.Unlocked)
                    {
                        return Request.CreateResponse(HttpStatusCode.OK, new
                        {
                            status = "locked",
                            machineId
                        });
                    }

                    var daysSinceActivation = (int)(DateTime.UtcNow - settings.ActivatedAt).TotalDays;

                    return Request.CreateResponse(HttpStatusCode.OK, new
                    {
                        status = "ok",
                        machineId,
                        daysSinceActivation
                    });
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] License check failed: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "License check failed");
            }
        }

        // Unlock now requires an authenticated user with license.manage AND a valid
        // server-side unlock code. The old behavior (any caller, hardcoded client code)
        // is gone.
        [Route("unlock")]
        [HttpPost]
        [RequirePermission("license.manage")]
        public HttpResponseMessage Unlock([FromBody] UnlockDto dto)
        {
            try
            {
                if (dto?.MachineId == null)
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Machine ID required");

                if (!LicenseService.IsValidUnlockCode(dto.MachineId, dto.Code))
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid unlock code");

                using (var conn = DbConnectionFactory.CreateConnection())
                {
                    _settingsRepo.Upsert(conn, dto.MachineId, true);
                    Console.WriteLine($"[API] License unlocked for machine: {dto.MachineId} (by {AuthorizationService.GetCurrentUserId(Request)})");
                    return Request.CreateResponse(HttpStatusCode.OK, new
                    {
                        success = true,
                        machineId = dto.MachineId
                    });
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] License unlock failed: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "License unlock failed");
            }
        }
    }

    public class UnlockDto
    {
        public string MachineId { get; set; }
        public string Code { get; set; }
    }
}
