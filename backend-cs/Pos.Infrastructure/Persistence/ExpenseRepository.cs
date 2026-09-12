using System;
using System.Collections.Generic;
using Dapper;
using PosCs.Application.Ports;
using PosCs.Domain.Entities;
using PosCs.Domain.Exceptions;
using PosCs.Infrastructure.Persistence;

namespace PosCs.Infrastructure.Persistence
{
    /// <summary>Expense persistence (plan Phase 10). Expenses are append-only; a new expense
    /// is stamped with the active shift so cash spending shows up in that shift's drawer math.</summary>
    public class ExpenseRepository : IExpenseRepository
    {
        private const string SelectWithCategory =
            @"SELECT e.*, c.name AS CategoryName
              FROM Expense e
              JOIN ExpenseCategory c ON c.id = e.categoryId";

        public Expense Create(Expense expense)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            using (var tx = conn.BeginTransaction())
            {
                try
                {
                    var category = conn.QueryFirstOrDefault<ExpenseCategory>(
                        "SELECT * FROM ExpenseCategory WHERE id = @id",
                        new { id = expense.CategoryId }, transaction: tx);
                    if (category == null)
                        throw new NotFoundException("Expense category not found");
                    if (!category.IsActive)
                        throw new DomainValidationException("Expense category is inactive");

                    expense.Id = Guid.NewGuid().ToString("N");
                    if (expense.CreatedAt == default)
                        expense.CreatedAt = DateTime.Now;
                    if (expense.Date == default)
                        expense.Date = DateTime.Now;

                    // Stamp with the active shift, when one is open.
                    var shiftId = conn.ExecuteScalar<string>(
                        "SELECT id FROM Shift WHERE status = 'open'", transaction: tx);
                    expense.ShiftId = shiftId;

                    conn.Execute(
                        @"INSERT INTO Expense (id, categoryId, amount, paymentMethod, date, description, reference, shiftId, createdBy, createdAt)
                          VALUES (@Id, @CategoryId, @Amount, @PaymentMethod, @Date, @Description, @Reference, @ShiftId, @CreatedBy, @CreatedAt)",
                        expense, transaction: tx);

                    tx.Commit();
                    return expense;
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        public ExpensePageResult GetPaged(string categoryId, DateTime? from, DateTime? to, int page, int pageSize)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                var where = " WHERE 1 = 1";
                var p = new DynamicParameters();
                if (!string.IsNullOrEmpty(categoryId))
                {
                    where += " AND e.categoryId = @categoryId";
                    p.Add("categoryId", categoryId);
                }
                if (from.HasValue)
                {
                    where += " AND e.date >= @from";
                    p.Add("from", from.Value);
                }
                if (to.HasValue)
                {
                    where += " AND e.date <= @to";
                    p.Add("to", to.Value);
                }

                var total = conn.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM Expense e" + where, p);

                var items = conn.Query<ExpenseRow>(SelectWithCategory + where +
                        " ORDER BY e.createdAt DESC, e.id DESC LIMIT @limit OFFSET @offset",
                        MergePaging(p, page, pageSize))
                    .AsList();

                return new ExpensePageResult { Items = MapRows(items), Total = total };
            }
        }

        public ExpenseCategory CreateCategory(ExpenseCategory category)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                category.Id = Guid.NewGuid().ToString("N");
                category.CreatedAt = DateTime.Now;
                conn.Execute(
                    @"INSERT INTO ExpenseCategory (id, name, isActive, createdAt)
                      VALUES (@Id, @Name, @IsActive, @CreatedAt)", category);
                return category;
            }
        }

        public ExpenseCategory UpdateCategory(ExpenseCategory category)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                var existing = conn.QueryFirstOrDefault<ExpenseCategory>(
                    "SELECT * FROM ExpenseCategory WHERE id = @id", new { id = category.Id });
                if (existing == null)
                    throw new NotFoundException("Expense category not found");

                conn.Execute(
                    @"UPDATE ExpenseCategory SET name = @Name, isActive = @IsActive WHERE id = @Id",
                    new { category.Id, category.Name, IsActive = category.IsActive ? 1 : 0 });
                existing.Name = category.Name;
                existing.IsActive = category.IsActive;
                return existing;
            }
        }

        public List<ExpenseCategory> GetCategories(bool includeInactive)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                var where = includeInactive ? "" : " WHERE isActive = 1";
                return conn.Query<ExpenseCategory>(
                    "SELECT * FROM ExpenseCategory" + where + " ORDER BY name").AsList();
            }
        }

        public double SumCashByShift(string shiftId)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                return conn.ExecuteScalar<double>(
                    "SELECT COALESCE(SUM(amount), 0) FROM Expense WHERE shiftId = @shiftId AND paymentMethod = 'cash'",
                    new { shiftId });
            }
        }

        private static DynamicParameters MergePaging(DynamicParameters p, int page, int pageSize)
        {
            p.Add("limit", pageSize);
            p.Add("offset", (page - 1) * pageSize);
            return p;
        }

        private static List<Expense> MapRows(IEnumerable<ExpenseRow> rows)
        {
            var list = new List<Expense>();
            foreach (var r in rows)
            {
                list.Add(new Expense
                {
                    Id = r.id,
                    CategoryId = r.categoryId,
                    CategoryName = r.categoryName,
                    Amount = r.amount,
                    PaymentMethod = r.paymentMethod,
                    Date = r.date,
                    Description = r.description,
                    Reference = r.reference,
                    ShiftId = r.shiftId,
                    CreatedBy = r.createdBy,
                    CreatedAt = r.createdAt,
                });
            }
            return list;
        }

        private sealed class ExpenseRow
        {
            public string id { get; set; }
            public string categoryId { get; set; }
            public string categoryName { get; set; }
            public double amount { get; set; }
            public string paymentMethod { get; set; }
            public DateTime date { get; set; }
            public string description { get; set; }
            public string reference { get; set; }
            public string shiftId { get; set; }
            public string createdBy { get; set; }
            public DateTime createdAt { get; set; }
        }
    }
}
