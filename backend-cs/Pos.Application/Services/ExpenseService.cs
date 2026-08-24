using System;
using PosCs.Application.Ports;
using PosCs.Domain.Entities;
using PosCs.Domain.Exceptions;

namespace PosCs.Application.Services
{
    /// <summary>Expense use cases (plan Phase 10, spec §26-27): operating costs recorded
    /// against the active shift. Cash expenses are drawer outflows; strictly separate
    /// from purchases and never touch client/supplier balances.</summary>
    public class ExpenseService
    {
        private static readonly string[] AllowedMethods = { "cash", "card", "bank_transfer" };

        private readonly IExpenseRepository _expenses;

        public ExpenseService(IExpenseRepository expenses)
        {
            _expenses = expenses;
        }

        public Expense Create(CreateExpenseRequest request, string userId)
        {
            if (request == null)
                throw new DomainValidationException("Invalid expense data");
            if (string.IsNullOrWhiteSpace(request.CategoryId))
                throw new DomainValidationException("Expense category is required");
            if (request.Amount <= 0)
                throw new DomainValidationException("Expense amount must be greater than zero");
            var method = string.IsNullOrWhiteSpace(request.PaymentMethod) ? "cash" : request.PaymentMethod;
            if (Array.IndexOf(AllowedMethods, method) < 0)
                throw new DomainValidationException("Invalid payment method");

            return _expenses.Create(new Expense
            {
                CategoryId = request.CategoryId,
                Amount = Math.Round(request.Amount, 2),
                PaymentMethod = method,
                Date = request.Date ?? DateTime.Now,
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                Reference = string.IsNullOrWhiteSpace(request.Reference) ? null : request.Reference.Trim(),
                CreatedBy = userId
            });
        }

        public ExpensePageResult GetPaged(string categoryId, DateTime? from, DateTime? to, int page, int pageSize)
        {
            if (pageSize > 100) pageSize = 100;
            if (page < 1) page = 1;
            return _expenses.GetPaged(categoryId, from, to, page, pageSize);
        }

        public System.Collections.Generic.List<ExpenseCategory> GetCategories(bool includeInactive)
        {
            return _expenses.GetCategories(includeInactive);
        }

        public ExpenseCategory CreateCategory(SaveExpenseCategoryRequest request)
        {
            ValidateCategory(request);
            return _expenses.CreateCategory(new ExpenseCategory
            {
                Name = request.Name.Trim(),
                IsActive = true
            });
        }

        public ExpenseCategory UpdateCategory(string id, SaveExpenseCategoryRequest request)
        {
            if (request == null)
                throw new DomainValidationException("Invalid category data");
            ValidateCategory(request);
            return _expenses.UpdateCategory(new ExpenseCategory
            {
                Id = id,
                Name = request.Name.Trim(),
                IsActive = request.IsActive ?? true
            });
        }

        private static void ValidateCategory(SaveExpenseCategoryRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Name))
                throw new DomainValidationException("Category name is required");
        }
    }
}
