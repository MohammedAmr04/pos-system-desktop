using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Dapper;
using PosCs.Attributes;
using PosCs.Helpers;
using PosCs.Models;

namespace PosCs.Controllers
{
    [RoutePrefix("api/reports")]
    public class ReportsController : ApiController
    {
        [Route("low-stock")]
        [HttpGet]
        [RequirePermission("reports.view", "low_stock_report")]
        public HttpResponseMessage GetLowStock()
        {
            try
            {
                using (var conn = DbConnectionFactory.CreateConnection())
                {
                    var products = conn.Query<Product>(
                        "SELECT * FROM Product WHERE lowStockThreshold > 0 AND stockQuantity <= lowStockThreshold ORDER BY stockQuantity ASC");
                    return Request.CreateResponse(HttpStatusCode.OK, products);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[API ERR] Failed to fetch low stock report: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to fetch low stock report");
            }
        }
    }
}
