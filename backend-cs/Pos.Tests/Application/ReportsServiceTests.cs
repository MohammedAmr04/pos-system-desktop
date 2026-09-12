using System;
using System.Collections.Generic;
using PosCs.Application.Ports;
using PosCs.Application.Services;
using PosCs.Domain.Entities;
using Xunit;

namespace PosCs.Tests.Application
{
    public class FakeReportRepository : IReportRepository
    {
        public SalesReport SalesResult = new SalesReport { NetSales = 100 };
        public PurchasesReport PurchasesResult = new PurchasesReport { Total = 50 };
        public ProfitReport ProfitResult = new ProfitReport { GrossProfit = 25, MarginPercent = 20 };
        public List<InventoryValuationRow> InventoryResult = new List<InventoryValuationRow>();
        public ReturnsReport ReturnsResult = new ReturnsReport();
        public ExpensesReport ExpensesResult = new ExpensesReport();
        public CashReport CashResult = new CashReport();

        public DateTime? LastFrom;
        public DateTime? LastTo;

        public SalesReport GetSalesReport(DateTime from, DateTime to)
        { LastFrom = from; LastTo = to; return SalesResult; }

        public PurchasesReport GetPurchasesReport(DateTime from, DateTime to)
        { return PurchasesResult; }

        public ProfitReport GetProfitReport(DateTime from, DateTime to)
        { return ProfitResult; }

        public List<InventoryValuationRow> GetInventoryValuation()
        { return InventoryResult; }

        public ReturnsReport GetReturnsReport(DateTime from, DateTime to)
        { return ReturnsResult; }

        public ExpensesReport GetExpensesReport(DateTime from, DateTime to)
        { return ExpensesResult; }

        public CashReport GetCashReport(DateTime from, DateTime to)
        { return CashResult; }

        public List<EmployeePerformanceRow> GetEmployeePerformance(DateTime from, DateTime to)
        { return new List<EmployeePerformanceRow>(); }
    }

    public class FakeProductsRepository : IProductRepository
    {
        public List<Product> Stored = new List<Product>();

        public Product GetById(string id) => null;
        public List<Product> GetAll() => Stored;
        public List<Product> GetForPOS() => Stored;
        public List<Product> Search(string query, int limit) => Stored;
        public PosCs.Application.Models.PagedResult<Product> GetPaged(int page, int pageSize, string query) =>
            new PosCs.Application.Models.PagedResult<Product> { Items = Stored, Total = Stored.Count };
        public int Count() => Stored.Count;
        public Product CreateWithBaseUnit(Product product, ProductUnit baseUnit, string barcode) =>
            throw new NotImplementedException();
        public void UpdateWithBaseUnit(Product product, ProductUnit baseUnit, string newDefaultBarcode) =>
            throw new NotImplementedException();
        public bool Delete(string id) => false;
    }

    public class ReportsServiceTests
    {
        private static ReportsService MakeService(FakeReportRepository repo) =>
            new ReportsService(new FakeProductsRepository(), repo);

        [Fact]
        public void LowStock_Filters_By_Policy()
        {
            var products = new FakeProductsRepository();
            products.Stored.Add(new Product { Name = "low", StockQuantity = 1, LowStockThreshold = 5 });
            products.Stored.Add(new Product { Name = "ok", StockQuantity = 10, LowStockThreshold = 5 });

            var result = new ReportsService(products, new FakeReportRepository()).LowStock();

            Assert.Single(result);
            Assert.Equal("low", result[0].Name);
        }

        [Fact]
        public void Sales_Passes_Range_And_Returns_Repository_Result()
        {
            var repo = new FakeReportRepository();
            var from = new DateTime(2026, 8, 1);
            var to = new DateTime(2026, 8, 24);

            var report = MakeService(repo).Sales(from, to);

            Assert.Equal(100, report.NetSales);
            Assert.Equal(from, repo.LastFrom);
            Assert.Equal(to, repo.LastTo);
        }

        [Fact]
        public void Profit_And_Cash_Delegate_To_Repository()
        {
            var repo = new FakeReportRepository();
            var service = MakeService(repo);

            Assert.Equal(25, service.Profit(DateTime.Today, DateTime.Today).GrossProfit);
            Assert.Equal(20, service.Profit(DateTime.Today, DateTime.Today).MarginPercent);
            Assert.Same(repo.CashResult, service.Cash(DateTime.Today, DateTime.Today));
            Assert.Same(repo.ExpensesResult, service.Expenses(DateTime.Today, DateTime.Today));
        }
    }
}
