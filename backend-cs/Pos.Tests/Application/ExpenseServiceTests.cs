using System;
using System.Collections.Generic;
using System.Linq;
using PosCs.Application.Ports;
using PosCs.Application.Services;
using PosCs.Domain.Entities;
using PosCs.Domain.Exceptions;
using Xunit;

namespace PosCs.Tests.Application
{
    public class FakeExpenseRepository : IExpenseRepository
    {
        public List<Expense> Stored = new List<Expense>();
        public List<ExpenseCategory> Categories = new List<ExpenseCategory>
        {
            new ExpenseCategory { Id = "cat-1", Name = "Rent", IsActive = true, CreatedAt = DateTime.Now },
            new ExpenseCategory { Id = "cat-off", Name = "Old", IsActive = false, CreatedAt = DateTime.Now },
        };

        public Expense Create(Expense expense)
        {
            var category = Categories.FirstOrDefault(c => c.Id == expense.CategoryId);
            if (category == null)
                throw new NotFoundException("Expense category not found");
            if (!category.IsActive)
                throw new DomainValidationException("Expense category is inactive");
            expense.Id = Guid.NewGuid().ToString("N");
            Stored.Add(expense);
            return expense;
        }

        public ExpensePageResult GetPaged(string categoryId, DateTime? from, DateTime? to, int page, int pageSize)
        {
            IEnumerable<Expense> q = Stored;
            if (!string.IsNullOrEmpty(categoryId)) q = q.Where(e => e.CategoryId == categoryId);
            if (from.HasValue) q = q.Where(e => e.Date >= from.Value);
            if (to.HasValue) q = q.Where(e => e.Date <= to.Value);
            var items = q.ToList();
            return new ExpensePageResult { Items = items, Total = items.Count };
        }

        public ExpenseCategory CreateCategory(ExpenseCategory category)
        {
            category.Id = Guid.NewGuid().ToString("N");
            Categories.Add(category);
            return category;
        }

        public ExpenseCategory UpdateCategory(ExpenseCategory category)
        {
            var existing = Categories.FirstOrDefault(c => c.Id == category.Id);
            if (existing == null)
                throw new NotFoundException("Expense category not found");
            existing.Name = category.Name;
            existing.IsActive = category.IsActive;
            return existing;
        }

        public List<ExpenseCategory> GetCategories(bool includeInactive) =>
            Categories.Where(c => includeInactive || c.IsActive).ToList();

        public double SumCashByShift(string shiftId) =>
            Stored.Where(e => e.ShiftId == shiftId && e.PaymentMethod == "cash").Sum(e => e.Amount);
    }

    public class ExpenseServiceTests
    {
        [Fact]
        public void Non_Positive_Amount_Is_Rejected()
        {
            var service = new ExpenseService(new FakeExpenseRepository());
            Assert.Throws<DomainValidationException>(() =>
                service.Create(new CreateExpenseRequest { CategoryId = "cat-1", Amount = 0 }, "u"));
            Assert.Throws<DomainValidationException>(() =>
                service.Create(new CreateExpenseRequest { CategoryId = "cat-1", Amount = -10 }, "u"));
        }

        [Fact]
        public void Unknown_Category_Is_Rejected()
        {
            Assert.Throws<NotFoundException>(() =>
                new ExpenseService(new FakeExpenseRepository()).Create(
                    new CreateExpenseRequest { CategoryId = "missing", Amount = 5 }, "u"));
        }

        [Fact]
        public void Inactive_Category_Is_Rejected()
        {
            Assert.Throws<DomainValidationException>(() =>
                new ExpenseService(new FakeExpenseRepository()).Create(
                    new CreateExpenseRequest { CategoryId = "cat-off", Amount = 5 }, "u"));
        }

        [Fact]
        public void Invalid_Payment_Method_Is_Rejected_And_Defaults_To_Cash()
        {
            var repo = new FakeExpenseRepository();
            var service = new ExpenseService(repo);

            Assert.Throws<DomainValidationException>(() =>
                service.Create(new CreateExpenseRequest { CategoryId = "cat-1", Amount = 5, PaymentMethod = "barter" }, "u"));

            var expense = service.Create(new CreateExpenseRequest { CategoryId = "cat-1", Amount = 7.256 }, "u");
            Assert.Equal("cash", expense.PaymentMethod);
            Assert.Equal(7.26, expense.Amount);
        }

        [Fact]
        public void Create_Trims_Fields_And_Keeps_User()
        {
            var repo = new FakeExpenseRepository();
            var expense = new ExpenseService(repo).Create(
                new CreateExpenseRequest
                {
                    CategoryId = "cat-1",
                    Amount = 50,
                    Description = "  office rent  ",
                    Reference = " ref-9 "
                }, "user-1");

            Assert.Equal("office rent", expense.Description);
            Assert.Equal("ref-9", expense.Reference);
            Assert.Equal("user-1", expense.CreatedBy);
            Assert.NotEqual(default, expense.Date);
        }

        [Fact]
        public void Category_Crud_Validates_Name_And_Toggles_Active()
        {
            var repo = new FakeExpenseRepository();
            var service = new ExpenseService(repo);

            Assert.Throws<DomainValidationException>(() =>
                service.CreateCategory(new SaveExpenseCategoryRequest { Name = "  " }));

            var created = service.CreateCategory(new SaveExpenseCategoryRequest { Name = "Marketing" });
            Assert.True(created.IsActive);

            var updated = service.UpdateCategory(created.Id,
                new SaveExpenseCategoryRequest { Name = "Marketing ", IsActive = false });
            Assert.False(updated.IsActive);

            var visible = service.GetCategories(includeInactive: false);
            Assert.DoesNotContain(visible, c => c.Id == created.Id);
        }
    }
}
