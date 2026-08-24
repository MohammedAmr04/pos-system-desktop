using System;
using System.Net;
using System.Web.Http;
using PosCs.Api;
using PosCs.Application.Ports;
using PosCs.Application.Services;
using PosCs.Attributes;

namespace PosCs.Controllers
{
    [RoutePrefix("api/expenses")]
    public class ExpensesController : ApiController
    {
        private readonly ExpenseService _service = CompositionRoot.ExpenseService;

        [Route("")]
        [HttpGet]
        [RequirePermission("expenses.view")]
        public IHttpActionResult GetPaged([FromUri] string categoryId = null, [FromUri] DateTime? from = null,
            [FromUri] DateTime? to = null, [FromUri] int page = 1, [FromUri] int pageSize = 20)
        {
            try
            {
                return Ok(_service.GetPaged(categoryId, from, to, page, pageSize));
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return Content(HttpStatusCode.BadRequest, new { message = ex.Message });
                Console.WriteLine($"[API ERR] Failed to list expenses: {ex}");
                return InternalServerError(new Exception("Failed to list expenses"));
            }
        }

        [Route("")]
        [HttpPost]
        [RequirePermission("expenses.create")]
        public IHttpActionResult Create([FromBody] CreateExpenseRequest dto)
        {
            try
            {
                var userId = Request.GetOwinContextUserId();
                var expense = _service.Create(dto, userId);
                Console.WriteLine($"[API] Expense created amount={expense.Amount} method={expense.PaymentMethod} shift={expense.ShiftId ?? "none"}");
                return Ok(expense);
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return Content(HttpStatusCode.BadRequest, new { message = ex.Message });
                Console.WriteLine($"[API ERR] Failed to create expense: {ex}");
                return InternalServerError(new Exception("Failed to create expense"));
            }
        }

        [Route("categories")]
        [HttpGet]
        [RequirePermission("expenses.view")]
        public IHttpActionResult GetCategories([FromUri] bool includeInactive = false)
        {
            try
            {
                return Ok(_service.GetCategories(includeInactive));
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return Content(HttpStatusCode.BadRequest, new { message = ex.Message });
                Console.WriteLine($"[API ERR] Failed to list expense categories: {ex}");
                return InternalServerError(new Exception("Failed to list expense categories"));
            }
        }

        [Route("categories")]
        [HttpPost]
        [RequirePermission("expenses.categories")]
        public IHttpActionResult CreateCategory([FromBody] SaveExpenseCategoryRequest dto)
        {
            try
            {
                var category = _service.CreateCategory(dto);
                Console.WriteLine($"[API] Expense category '{category.Name}' created");
                return Ok(category);
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return Content(HttpStatusCode.BadRequest, new { message = ex.Message });
                Console.WriteLine($"[API ERR] Failed to create expense category: {ex}");
                return InternalServerError(new Exception("Failed to create expense category"));
            }
        }

        [Route("categories/{id}")]
        [HttpPut]
        [RequirePermission("expenses.categories")]
        public IHttpActionResult UpdateCategory(string id, [FromBody] SaveExpenseCategoryRequest dto)
        {
            try
            {
                return Ok(_service.UpdateCategory(id, dto));
            }
            catch (Exception ex)
            {
                if (ApiErrors.IsHandled(ex)) return Content(HttpStatusCode.BadRequest, new { message = ex.Message });
                Console.WriteLine($"[API ERR] Failed to update expense category {id}: {ex}");
                return InternalServerError(new Exception("Failed to update expense category"));
            }
        }
    }
}
