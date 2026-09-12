using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using PosCs.Api;
using PosCs.Application.Models;
using PosCs.Application.Services;
using PosCs.Attributes;

namespace PosCs.Controllers
{
    [RoutePrefix("api/employees")]
    public class EmployeesController : ApiController
    {
        private readonly EmployeeService _service = CompositionRoot.EmployeeService;

        [Route("")]
        [HttpGet]
        [RequirePermission("employees.view")]
        public HttpResponseMessage GetActive()
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK, _service.GetActive());
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to fetch employees: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to fetch employees");
            }
        }

        [Route("paged")]
        [HttpGet]
        [RequirePermission("employees.view")]
        public HttpResponseMessage GetPaged([FromUri] int page = 1, [FromUri] int pageSize = 20, [FromUri] string q = null)
        {
            try
            {
                var result = _service.GetPaged(page, pageSize, q);
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
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to fetch employees (paged): {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to fetch employees");
            }
        }

        [Route("{id}")]
        [HttpGet]
        [RequirePermission("employees.view")]
        public HttpResponseMessage GetById(string id)
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK, _service.GetById(id));
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to fetch employee {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to fetch employee");
            }
        }

        [Route("")]
        [HttpPost]
        [RequirePermission("employees.manage")]
        public HttpResponseMessage Create([FromBody] CreateEmployeeRequest dto)
        {
            try
            {
                var employee = _service.Create(dto);
                Console.WriteLine($"[API] Created employee: {employee.Id} '{employee.Name}'");
                return Request.CreateResponse(HttpStatusCode.OK, employee);
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to create employee: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to create employee");
            }
        }

        [Route("{id}")]
        [HttpPut]
        [RequirePermission("employees.manage")]
        public HttpResponseMessage Update(string id, [FromBody] UpdateEmployeeRequest dto)
        {
            try
            {
                var employee = _service.Update(id, dto);
                Console.WriteLine($"[API] Updated employee: {employee.Id} '{employee.Name}'");
                return Request.CreateResponse(HttpStatusCode.OK, employee);
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return ApiErrors.From(Request, ex, null);
                Console.Error.WriteLine($"[API ERR] Failed to update employee {id}: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Failed to update employee");
            }
        }

        // NOTE: no DELETE — posted invoices need a stable employee identity; deactivate instead.
    }
}
