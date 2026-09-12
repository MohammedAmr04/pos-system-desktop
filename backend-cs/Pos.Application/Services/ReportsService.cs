using System;
using System.Collections.Generic;
using System.Linq;
using PosCs.Application.Ports;
using PosCs.Domain.Entities;
using PosCs.Domain.Rules;

namespace PosCs.Application.Services
{
    /// <summary>Reporting use cases. Phase 11 adds read-only aggregations over historical
    /// snapshots (sales/purchases/inventory/profit/returns/expenses/cash) via IReportRepository.</summary>
    public class ReportsService
    {
        private readonly IProductRepository _products;
        private readonly IReportRepository _reports;

        public ReportsService(IProductRepository products, IReportRepository reports)
        {
            _products = products;
            _reports = reports;
        }

        public List<Product> LowStock()
        {
            return _products.GetAll()
                .Where(LowStockPolicy.IsLowStock)
                .OrderBy(p => p.StockQuantity)
                .ToList();
        }

        public SalesReport Sales(DateTime from, DateTime to) => _reports.GetSalesReport(from, to);

        public PurchasesReport Purchases(DateTime from, DateTime to) => _reports.GetPurchasesReport(from, to);

        public ProfitReport Profit(DateTime from, DateTime to) => _reports.GetProfitReport(from, to);

        public List<InventoryValuationRow> InventoryValuation() => _reports.GetInventoryValuation();

        public ReturnsReport Returns(DateTime from, DateTime to) => _reports.GetReturnsReport(from, to);

        public ExpensesReport Expenses(DateTime from, DateTime to) => _reports.GetExpensesReport(from, to);

        public CashReport Cash(DateTime from, DateTime to) => _reports.GetCashReport(from, to);

        public List<EmployeePerformanceRow> EmployeePerformance(DateTime from, DateTime to) => _reports.GetEmployeePerformance(from, to);
    }
}
