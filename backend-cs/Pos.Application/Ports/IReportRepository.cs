using System;
using System.Collections.Generic;

namespace PosCs.Application.Ports
{
    /// <summary>Read-only aggregations over historical snapshot data (plan Phase 11, spec §29).
    /// Profit uses the FIFO COGS snapshots captured at sale time; returns use their stored
    /// refund/restored-cost totals. No live recalculation.</summary>
    public interface IReportRepository
    {
        SalesReport GetSalesReport(DateTime from, DateTime to);
        PurchasesReport GetPurchasesReport(DateTime from, DateTime to);
        ProfitReport GetProfitReport(DateTime from, DateTime to);
        List<InventoryValuationRow> GetInventoryValuation();
        ReturnsReport GetReturnsReport(DateTime from, DateTime to);
        ExpensesReport GetExpensesReport(DateTime from, DateTime to);
        CashReport GetCashReport(DateTime from, DateTime to);
    }

    public sealed class SalesByDayRow
    {
        public string Day { get; set; }
        public int InvoiceCount { get; set; }
        public double NetSales { get; set; }
        public double Discounts { get; set; }
    }

    public sealed class SalesByMethodRow
    {
        /// <summary>'cash' | 'credit'.</summary>
        public string PaymentMethod { get; set; }
        public int InvoiceCount { get; set; }
        public double Total { get; set; }
    }

    public sealed class TopProductRow
    {
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public double Quantity { get; set; }
        public double Revenue { get; set; }
        public double Cost { get; set; }
    }

    public sealed class SalesByUserRow
    {
        public string UserName { get; set; }
        public int InvoiceCount { get; set; }
        public double Total { get; set; }
    }

    public sealed class SalesReport
    {
        public double GrossSales { get; set; }
        public double Discounts { get; set; }
        public double NetSales { get; set; }
        public int InvoiceCount { get; set; }
        public List<SalesByDayRow> ByDay { get; set; } = new List<SalesByDayRow>();
        public List<SalesByMethodRow> ByPaymentMethod { get; set; } = new List<SalesByMethodRow>();
        public List<TopProductRow> TopProducts { get; set; } = new List<TopProductRow>();
        public List<SalesByUserRow> ByCashier { get; set; } = new List<SalesByUserRow>();
    }

    public sealed class PurchasesByDayRow
    {
        public string Day { get; set; }
        public int InvoiceCount { get; set; }
        public double Total { get; set; }
    }

    public sealed class PurchasesBySupplierRow
    {
        public string SupplierName { get; set; }
        public int InvoiceCount { get; set; }
        public double Total { get; set; }
    }

    public sealed class PurchasesReport
    {
        public double Total { get; set; }
        public int InvoiceCount { get; set; }
        public List<PurchasesByDayRow> ByDay { get; set; } = new List<PurchasesByDayRow>();
        public List<PurchasesBySupplierRow> BySupplier { get; set; } = new List<PurchasesBySupplierRow>();
    }

    public sealed class ProfitByDayRow
    {
        public string Day { get; set; }
        public double Revenue { get; set; }
        public double Cost { get; set; }
        public double Profit { get; set; }
    }

    public sealed class ProfitReport
    {
        public double GrossSales { get; set; }
        public double Cogs { get; set; }
        public double SaleRefunds { get; set; }
        public double RestoredCosts { get; set; }
        public double NetRevenue { get; set; }
        public double NetCogs { get; set; }
        public double GrossProfit { get; set; }
        public double MarginPercent { get; set; }
        public List<ProfitByDayRow> ByDay { get; set; } = new List<ProfitByDayRow>();
    }

    public sealed class InventoryValuationRow
    {
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public double StockQuantity { get; set; }
        /// <summary>FIFO layer value when layers exist; BuyPrice fallback otherwise.</summary>
        public double UnitCost { get; set; }
        public double Value { get; set; }
        public string CategoryName { get; set; }
    }

    public sealed class ReturnsReport
    {
        public int SaleReturnCount { get; set; }
        public double SaleRefundTotal { get; set; }
        public int PurchaseReturnCount { get; set; }
        public double PurchaseRefundTotal { get; set; }
        public List<ReturnsByDayRow> SaleReturnsByDay { get; set; } = new List<ReturnsByDayRow>();
    }

    public sealed class ReturnsByDayRow
    {
        public string Day { get; set; }
        public int Count { get; set; }
        public double Total { get; set; }
    }

    public sealed class ExpensesByCategoryRow
    {
        public string CategoryName { get; set; }
        public int Count { get; set; }
        public double Total { get; set; }
    }

    public sealed class ExpensesReport
    {
        public double TotalCash { get; set; }
        public int Count { get; set; }
        public List<ExpensesByCategoryRow> ByCategory { get; set; } = new List<ExpensesByCategoryRow>();
    }

    public sealed class CashShiftRow
    {
        public int Number { get; set; }
        public string OpenedAt { get; set; }
        public string ClosedAt { get; set; }
        public double OpeningCash { get; set; }
        public double ExpectedCash { get; set; }
        public double CountedCash { get; set; }
        public double Difference { get; set; }
    }

    public sealed class CashReport
    {
        public double TotalOpening { get; set; }
        public double TotalExpected { get; set; }
        public double TotalCounted { get; set; }
        public double TotalDifference { get; set; }
        public int ShiftCount { get; set; }
        public List<CashShiftRow> Shifts { get; set; } = new List<CashShiftRow>();
    }
}
