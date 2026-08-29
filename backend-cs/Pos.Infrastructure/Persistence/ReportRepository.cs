using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using PosCs.Application.Ports;
using PosCs.Infrastructure.Persistence;

namespace PosCs.Infrastructure.Persistence
{
    /// <summary>Report aggregations (plan Phase 11). Reads historical snapshots only:
    /// Invoice/InvoiceDetail (FinalTotal + TotalCost FIFO COGS), SaleReturn/PurchaseReturn
    /// stored totals, Expense, Shift close values, and CostLayer for stock valuation.</summary>
    public class ReportRepository : IReportRepository
    {
        public SalesReport GetSalesReport(DateTime from, DateTime to)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                var range = new { from, to };
                var report = new SalesReport();

                var totals = conn.QueryFirstOrDefault<TotalsRow>(
                    @"SELECT COUNT(1) AS InvoiceCount,
                             COALESCE(SUM(totalAmount), 0) AS GrossSales,
                             COALESCE(SUM(discountAmount), 0) AS Discounts
                      FROM Invoice
                      WHERE status = 'posted' AND createdAt BETWEEN @from AND @to", range);
                report.InvoiceCount = (int)totals.InvoiceCount;
                report.GrossSales = Math.Round((double)totals.GrossSales, 2);
                report.Discounts = Math.Round((double)totals.Discounts, 2);
                report.NetSales = Math.Round(report.GrossSales, 2);

                report.ByDay = conn.Query<SalesByDayRow>(
                    @"SELECT date(createdAt) AS Day, COUNT(1) AS InvoiceCount,
                             COALESCE(SUM(totalAmount), 0) AS NetSales,
                             COALESCE(SUM(discountAmount), 0) AS Discounts
                      FROM Invoice
                      WHERE status = 'posted' AND createdAt BETWEEN @from AND @to
                      GROUP BY date(createdAt) ORDER BY Day", range).ToList();

                report.ByPaymentMethod = conn.Query<SalesByMethodRow>(
                    @"SELECT paymentMethod AS PaymentMethod, COUNT(1) AS InvoiceCount,
                             COALESCE(SUM(totalAmount), 0) AS Total
                      FROM Invoice
                      WHERE status = 'posted' AND createdAt BETWEEN @from AND @to
                      GROUP BY paymentMethod ORDER BY Total DESC", range).ToList();

                report.TopProducts = conn.Query<TopProductRow>(
                    @"SELECT d.productId AS ProductId,
                             COALESCE(p.name, d.productId) AS ProductName,
                             COALESCE(SUM(d.quantity * d.quantityFactor), 0) AS Quantity,
                             COALESCE(SUM(d.finalTotal), 0) AS Revenue,
                             COALESCE(SUM(d.totalCost), 0) AS Cost
                       FROM InvoiceDetail d
                       JOIN Invoice i ON i.id = d.invoiceId
                       LEFT JOIN Product p ON p.id = d.productId
                       WHERE i.status = 'posted' AND i.createdAt BETWEEN @from AND @to
                       GROUP BY d.productId, p.name
                       ORDER BY Revenue DESC
                       LIMIT 10", range).ToList();
                foreach (var row in report.TopProducts)
                {
                    row.Quantity = Math.Round(row.Quantity, 3);
                    row.Revenue = Math.Round(row.Revenue, 2);
                    row.Cost = Math.Round(row.Cost, 2);
                }

                report.ByCashier = conn.Query<SalesByUserRow>(
                    @"SELECT COALESCE(u.username, i.createdBy) AS UserName,
                             COUNT(1) AS InvoiceCount,
                             COALESCE(SUM(i.totalAmount), 0) AS Total
                      FROM Invoice i
                      LEFT JOIN User u ON u.id = i.createdBy
                      WHERE i.status = 'posted' AND i.createdAt BETWEEN @from AND @to
                      GROUP BY u.username, i.createdBy
                      ORDER BY Total DESC", range).ToList();

                return report;
            }
        }

        public PurchasesReport GetPurchasesReport(DateTime from, DateTime to)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                var range = new { from, to };
                var report = new PurchasesReport();

                var totals = conn.QueryFirstOrDefault<TotalsRow>(
                    @"SELECT COUNT(1) AS InvoiceCount, COALESCE(SUM(totalAmount), 0) AS Total
                      FROM PurchaseInvoice
                      WHERE status = 'posted' AND createdAt BETWEEN @from AND @to", range);
                report.InvoiceCount = (int)totals.InvoiceCount;
                report.Total = Math.Round((double)totals.Total, 2);

                report.ByDay = conn.Query<PurchasesByDayRow>(
                    @"SELECT date(createdAt) AS Day, COUNT(1) AS InvoiceCount,
                             COALESCE(SUM(totalAmount), 0) AS Total
                      FROM PurchaseInvoice
                      WHERE status = 'posted' AND createdAt BETWEEN @from AND @to
                      GROUP BY date(createdAt) ORDER BY Day", range).ToList();

                report.BySupplier = conn.Query<PurchasesBySupplierRow>(
                    @"SELECT COALESCE(s.name, pi.supplierId) AS SupplierName,
                             COUNT(1) AS InvoiceCount,
                             COALESCE(SUM(pi.totalAmount), 0) AS Total
                      FROM PurchaseInvoice pi
                      LEFT JOIN Supplier s ON s.id = pi.supplierId
                      WHERE pi.status = 'posted' AND pi.createdAt BETWEEN @from AND @to
                      GROUP BY s.name, pi.supplierId
                      ORDER BY Total DESC", range).ToList();

                return report;
            }
        }

        public ProfitReport GetProfitReport(DateTime from, DateTime to)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                var range = new { from, to };
                var report = new ProfitReport();

                var sales = conn.QueryFirstOrDefault<TotalsRow>(
                    @"SELECT COALESCE(SUM(d.finalTotal), 0) AS Revenue,
                             COALESCE(SUM(d.totalCost), 0) AS Cost
                       FROM InvoiceDetail d
                       JOIN Invoice i ON i.id = d.invoiceId
                       WHERE i.status = 'posted' AND i.createdAt BETWEEN @from AND @to", range);
                report.GrossSales = Math.Round((double)sales.Revenue, 2);
                report.Cogs = Math.Round((double)sales.Cost, 2);

                var returns = conn.QueryFirstOrDefault<TotalsRow>(
                    @"SELECT COALESCE(SUM(totalAmount), 0) AS Refunds,
                             COALESCE(SUM(restoredCost), 0) AS Restored
                      FROM SaleReturn
                      WHERE date BETWEEN @from AND @to", range);
                report.SaleRefunds = Math.Round((double)returns.Refunds, 2);
                report.RestoredCosts = Math.Round((double)returns.Restored, 2);

                report.NetRevenue = Math.Round(report.GrossSales - report.SaleRefunds, 2);
                report.NetCogs = Math.Round(report.Cogs - report.RestoredCosts, 2);
                report.GrossProfit = Math.Round(report.NetRevenue - report.NetCogs, 2);
                report.MarginPercent = report.NetRevenue != 0
                    ? Math.Round(report.GrossProfit / report.NetRevenue * 100, 2)
                    : 0;

                report.ByDay = conn.Query<ProfitByDayRow>(
                    @"SELECT date(i.createdAt) AS Day,
                             COALESCE(SUM(d.finalTotal), 0) AS Revenue,
                             COALESCE(SUM(d.totalCost), 0) AS Cost,
                             COALESCE(SUM(d.finalTotal - COALESCE(d.totalCost, 0)), 0) AS Profit
                       FROM InvoiceDetail d
                       JOIN Invoice i ON i.id = d.invoiceId
                       WHERE i.status = 'posted' AND i.createdAt BETWEEN @from AND @to
                       GROUP BY date(i.createdAt) ORDER BY Day", range).ToList();
                foreach (var row in report.ByDay)
                {
                    row.Revenue = Math.Round(row.Revenue, 2);
                    row.Cost = Math.Round(row.Cost, 2);
                    row.Profit = Math.Round(row.Profit, 2);
                }

                return report;
            }
        }

        public List<InventoryValuationRow> GetInventoryValuation()
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                // FIFO layers are the source of truth for value; products without layers
                // (legacy or fully sold) fall back to their last BuyPrice.
                var rows = conn.Query<InventoryValuationRow>(
                    @"SELECT p.id AS ProductId,
                             p.name AS ProductName,
                             p.stockQuantity AS StockQuantity,
                             COALESCE(layer.UnitCost, p.buyPrice, 0) AS UnitCost,
                             ROUND(p.stockQuantity * COALESCE(layer.UnitCost, p.buyPrice, 0), 2) AS Value,
                             c.name AS CategoryName
                      FROM Product p
                      LEFT JOIN Category c ON c.id = p.categoryId
                      LEFT JOIN (
                          SELECT productId, SUM(unitCost * quantityRemaining) / NULLIF(SUM(quantityRemaining), 0) AS UnitCost
                          FROM CostLayer
                          WHERE quantityRemaining > 0
                          GROUP BY productId
                      ) layer ON layer.productId = p.id
                      WHERE p.stockQuantity > 0
                      ORDER BY Value DESC").ToList();
                foreach (var row in rows)
                    row.StockQuantity = Math.Round(row.StockQuantity, 3);
                return rows;
            }
        }

        public ReturnsReport GetReturnsReport(DateTime from, DateTime to)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                var range = new { from, to };
                var report = new ReturnsReport();

                var sales = conn.QueryFirstOrDefault<TotalsRow>(
                    @"SELECT COUNT(1) AS Cnt, COALESCE(SUM(totalAmount), 0) AS Total
                      FROM SaleReturn WHERE date BETWEEN @from AND @to", range);
                report.SaleReturnCount = (int)sales.Cnt;
                report.SaleRefundTotal = Math.Round((double)sales.Total, 2);

                var purchases = conn.QueryFirstOrDefault<TotalsRow>(
                    @"SELECT COUNT(1) AS Cnt, COALESCE(SUM(totalAmount), 0) AS Total
                      FROM PurchaseReturn WHERE date BETWEEN @from AND @to", range);
                report.PurchaseReturnCount = (int)purchases.Cnt;
                report.PurchaseRefundTotal = Math.Round((double)purchases.Total, 2);

                report.SaleReturnsByDay = conn.Query<ReturnsByDayRow>(
                    @"SELECT date(date) AS Day, COUNT(1) AS Count, COALESCE(SUM(totalAmount), 0) AS Total
                      FROM SaleReturn WHERE date BETWEEN @from AND @to
                      GROUP BY date(date) ORDER BY Day", range).ToList();

                return report;
            }
        }

        public ExpensesReport GetExpensesReport(DateTime from, DateTime to)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                var range = new { from, to };
                var report = new ExpensesReport();

                var totals = conn.QueryFirstOrDefault<TotalsRow>(
                    @"SELECT COUNT(1) AS Cnt, COALESCE(SUM(amount), 0) AS Total
                      FROM Expense
                      WHERE paymentMethod = 'cash' AND date BETWEEN @from AND @to", range);
                report.Count = (int)totals.Cnt;
                report.TotalCash = Math.Round((double)totals.Total, 2);

                report.ByCategory = conn.Query<ExpensesByCategoryRow>(
                    @"SELECT COALESCE(c.name, e.categoryId) AS CategoryName,
                             COUNT(1) AS Count, COALESCE(SUM(e.amount), 0) AS Total
                      FROM Expense e
                      LEFT JOIN ExpenseCategory c ON c.id = e.categoryId
                      WHERE e.paymentMethod = 'cash' AND e.date BETWEEN @from AND @to
                      GROUP BY c.name, e.categoryId
                      ORDER BY Total DESC", range).ToList();

                return report;
            }
        }

        public CashReport GetCashReport(DateTime from, DateTime to)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                var range = new { from, to };
                var report = new CashReport();

                var rows = conn.Query<CashShiftRow>(
                    @"SELECT number AS Number, openedAt AS OpenedAt, closedAt AS ClosedAt,
                             openingCash AS OpeningCash, expectedCash AS ExpectedCash,
                             countedCash AS CountedCash, difference AS Difference
                      FROM Shift
                      WHERE status = 'closed' AND closedAt BETWEEN @from AND @to
                      ORDER BY number DESC", range).ToList();
                report.Shifts = rows;
                report.ShiftCount = rows.Count;
                report.TotalOpening = Math.Round(rows.Sum(r => r.OpeningCash), 2);
                report.TotalExpected = Math.Round(rows.Sum(r => r.ExpectedCash), 2);
                report.TotalCounted = Math.Round(rows.Sum(r => r.CountedCash), 2);
                report.TotalDifference = Math.Round(rows.Sum(r => r.Difference), 2);
                return report;
            }
        }

        /// <summary>Shared projection for scalar aggregate queries (Dapper maps by alias).</summary>
        private sealed class TotalsRow
        {
            public int InvoiceCount { get; set; }
            public double GrossSales { get; set; }
            public double Discounts { get; set; }
            public double Total { get; set; }
            public double Revenue { get; set; }
            public double Cost { get; set; }
            public double Refunds { get; set; }
            public double Restored { get; set; }
            public int Cnt { get; set; }
        }
    }
}
