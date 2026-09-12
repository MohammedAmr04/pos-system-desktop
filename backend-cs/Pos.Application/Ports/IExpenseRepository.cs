using System;
using System.Collections.Generic;
using PosCs.Domain.Entities;

namespace PosCs.Application.Ports
{
    /// <summary>Operating expenses (plan Phase 10, spec §26-27). Strictly separate from
    /// purchases. A cash expense is a drawer movement stamped with the active shift.</summary>
    public interface IExpenseRepository
    {
        Expense Create(Expense expense);

        ExpensePageResult GetPaged(string categoryId, DateTime? from, DateTime? to, int page, int pageSize);

        // Categories
        ExpenseCategory CreateCategory(ExpenseCategory category);
        ExpenseCategory UpdateCategory(ExpenseCategory category);
        List<ExpenseCategory> GetCategories(bool includeInactive);

        /// <summary>Total cash expenses recorded against a shift (drawer math).</summary>
        double SumCashByShift(string shiftId);
    }

    public sealed class ExpensePageResult
    {
        public List<Expense> Items { get; set; }
        public int Total { get; set; }
    }

    public sealed class CreateExpenseRequest
    {
        public string CategoryId { get; set; }
        public double Amount { get; set; }
        /// <summary>'cash' | 'card' | 'bank_transfer' — only 'cash' moves the drawer.</summary>
        public string PaymentMethod { get; set; }
        public DateTime? Date { get; set; }
        public string Description { get; set; }
        public string Reference { get; set; }
    }

    public sealed class SaveExpenseCategoryRequest
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool? IsActive { get; set; }
    }
}
