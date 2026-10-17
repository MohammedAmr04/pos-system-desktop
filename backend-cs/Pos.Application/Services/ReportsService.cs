using System;
using System.Collections.Generic;
using System.Linq;
using PosCs.Application.Ports;
using PosCs.Domain.Entities;
using PosCs.Domain.Exceptions;
using PosCs.Domain.Rules;

namespace PosCs.Application.Services
{
    /// <summary>Reporting use cases. Phase 11 adds read-only aggregations over historical
    /// snapshots (sales/purchases/inventory/profit/returns/expenses/cash) via IReportRepository.</summary>
    public class ReportsService
    {
        private readonly IProductRepository _products;
        private readonly IReportRepository _reports;
        private readonly IBranchContext _branchContext;

        public ReportsService(IProductRepository products, IReportRepository reports, IBranchContext branchContext)
        {
            _products = products;
            _reports = reports;
            _branchContext = branchContext;
        }

        public List<Product> LowStock()
        {
            return _products.GetAll()
                .Where(LowStockPolicy.IsLowStock)
                .OrderBy(p => p.StockQuantity)
                .ToList();
        }

        public SalesReport Sales(DateTime from, DateTime to, string branchId = null) => _reports.GetSalesReport(from, to, _branchContext.ApplyScope(branchId));

        public PurchasesReport Purchases(DateTime from, DateTime to, string branchId = null) => _reports.GetPurchasesReport(from, to, _branchContext.ApplyScope(branchId));

        public ProfitReport Profit(DateTime from, DateTime to, string branchId = null) => _reports.GetProfitReport(from, to, _branchContext.ApplyScope(branchId));

        public List<InventoryValuationRow> InventoryValuation() => _reports.GetInventoryValuation();

        public List<BranchStockRow> BranchStock(string branchId)
        {
            var scoped = _branchContext.ApplyScope(branchId);
            if (string.IsNullOrWhiteSpace(scoped))
                throw new DomainValidationException("Branch is required");
            return _reports.GetBranchStock(scoped);
        }

        public ReturnsReport Returns(DateTime from, DateTime to, string branchId = null) => _reports.GetReturnsReport(from, to, _branchContext.ApplyScope(branchId));

        public ExpensesReport Expenses(DateTime from, DateTime to, string branchId = null) => _reports.GetExpensesReport(from, to, _branchContext.ApplyScope(branchId));

        public CashReport Cash(DateTime from, DateTime to, string branchId = null) => _reports.GetCashReport(from, to, _branchContext.ApplyScope(branchId));

        public List<EmployeePerformanceRow> EmployeePerformance(DateTime from, DateTime to, string branchId = null) => _reports.GetEmployeePerformance(from, to, _branchContext.ApplyScope(branchId));
    }
}
