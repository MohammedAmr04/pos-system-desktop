using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using PosCs.Api;
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

        [Route("sales")]
        [HttpGet]
        [RequirePermission("reports.view")]
        public IHttpActionResult GetSales([FromUri] DateTime from, [FromUri] DateTime to)
        {
            try
            {
                return Ok(_service.Sales(from, EndOfDay(to)));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to build sales report: {ex}");
                return InternalServerError(new Exception("Failed to build sales report"));
            }
        }

        [Route("purchases")]
        [HttpGet]
        [RequirePermission("reports.view")]
        public IHttpActionResult GetPurchases([FromUri] DateTime from, [FromUri] DateTime to)
        {
            try
            {
                return Ok(_service.Purchases(from, EndOfDay(to)));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to build purchases report: {ex}");
                return InternalServerError(new Exception("Failed to build purchases report"));
            }
        }

        [Route("profit")]
        [HttpGet]
        [RequirePermission("reports.view")]
        public IHttpActionResult GetProfit([FromUri] DateTime from, [FromUri] DateTime to)
        {
            try
            {
                return Ok(_service.Profit(from, EndOfDay(to)));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to build profit report: {ex}");
                return InternalServerError(new Exception("Failed to build profit report"));
            }
        }

        [Route("inventory")]
        [HttpGet]
        [RequirePermission("reports.view")]
        public IHttpActionResult GetInventory()
        {
            try
            {
                return Ok(_service.InventoryValuation());
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to build inventory report: {ex}");
                return InternalServerError(new Exception("Failed to build inventory report"));
            }
        }

        [Route("returns")]
        [HttpGet]
        [RequirePermission("reports.view")]
        public IHttpActionResult GetReturns([FromUri] DateTime from, [FromUri] DateTime to)
        {
            try
            {
                return Ok(_service.Returns(from, EndOfDay(to)));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to build returns report: {ex}");
                return InternalServerError(new Exception("Failed to build returns report"));
            }
        }

        [Route("expenses")]
        [HttpGet]
        [RequirePermission("reports.view")]
        public IHttpActionResult GetExpensesReport([FromUri] DateTime from, [FromUri] DateTime to)
        {
            try
            {
                return Ok(_service.Expenses(from, EndOfDay(to)));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to build expenses report: {ex}");
                return InternalServerError(new Exception("Failed to build expenses report"));
            }
        }

        [Route("cash")]
        [HttpGet]
        [RequirePermission("reports.view")]
        public IHttpActionResult GetCash([FromUri] DateTime from, [FromUri] DateTime to)
        {
            try
            {
                return Ok(_service.Cash(from, EndOfDay(to)));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to build cash report: {ex}");
                return InternalServerError(new Exception("Failed to build cash report"));
            }
        }

        [Route("employee-performance")]
        [HttpGet]
        [RequirePermission("reports.view")]
        public IHttpActionResult GetEmployeePerformance([FromUri] DateTime from, [FromUri] DateTime to)
        {
            try
            {
                return Ok(_service.EmployeePerformance(from, EndOfDay(to)));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to build employee performance report: {ex}");
                return InternalServerError(new Exception("Failed to build employee performance report"));
            }
        }

        private static DateTime EndOfDay(DateTime value) => value.Date.AddDays(1).AddSeconds(-1);
    }
}
