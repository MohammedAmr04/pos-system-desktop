using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using PosCs.Application.Services;
using PosCs.Attributes;

namespace PosCs.Controllers
{
    [RoutePrefix("api/reports")]
    public class ReportsController : ApiController
    {
        private readonly ReportsService _service = CompositionRoot.ReportsService;

        [Route("low-stock")]
        [HttpGet]
        [RequirePermission("reports.view", "low_stock_report")]
        public HttpResponseMessage GetLowStock()
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK, _service.LowStock());
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to fetch low stock report: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to fetch low stock report");
            }
        }
    }
}
