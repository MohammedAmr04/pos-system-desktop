using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace PosCs.Controllers
{
    public class HealthController : ApiController
    {
        [Route("health")]
        [HttpGet]
        public HttpResponseMessage Get()
        {
            return Request.CreateResponse(HttpStatusCode.OK, new
            {
                status = "ok",
                timestamp = DateTime.UtcNow.ToString("o")
            });
        }
    }
}
