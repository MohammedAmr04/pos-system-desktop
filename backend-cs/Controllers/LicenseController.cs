using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using PosCs.Helpers;
using PosCs.Repositories;

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

        [Route("unlock")]
        [HttpPost]
        public HttpResponseMessage Unlock([FromBody] UnlockDto dto)
        {
            try
            {
                if (dto?.MachineId == null)
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Machine ID required");

                using (var conn = DbConnectionFactory.CreateConnection())
                {
                    _settingsRepo.Upsert(conn, dto.MachineId, true);
                    Console.WriteLine($"[API] License unlocked for machine: {dto.MachineId}");
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
    }
}
