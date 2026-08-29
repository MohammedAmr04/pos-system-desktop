using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using Microsoft.Data.Sqlite;
using PosCs.Application.Models;
using PosCs.Application.Ports;
using PosCs.Domain.Entities;
using PosCs.Domain.Exceptions;
using PosCs.Domain.Rules;

namespace PosCs.Infrastructure.Persistence
{
    /// <summary>Transactional shift pipeline (plan Phase 9): one open shift at a time; close
    /// derives expected cash from the Payment rows stamped with the shift (spec §27):
    /// opening + cash sales + other cash in − cash refunds − supplier cash out − cash expenses.</summary>
    public class ShiftRepository : IShiftRepository
    {
        public Shift Create(Shift shift)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        var openCount = conn.ExecuteScalar<int>(
                            "SELECT COUNT(1) FROM Shift WHERE status = 'open'", transaction: tx);
                        if (openCount > 0)
                            throw new DomainValidationException("An open shift already exists");

                        shift.Id = Guid.NewGuid().ToString("N");
                        shift.Number = conn.ExecuteScalar<int>(
                            "SELECT COALESCE(MAX(number), 0) + 1 FROM Shift", transaction: tx);
                        shift.OpenedAt = DateTime.Now;
                        shift.Status = "open";

                        conn.Execute(@"
                            INSERT INTO Shift (id, number, openedBy, openingCash, openedAt, notes, status)
                            VALUES (@id, @number, @openedBy, @openingCash, @openedAt, @notes, @status)",
                            new
                            {
                                id = shift.Id,
                                number = shift.Number,
                                openedBy = shift.OpenedBy,
                                openingCash = shift.OpeningCash,
                                openedAt = shift.OpenedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                                notes = shift.Notes,
                                status = shift.Status
                            }, transaction: tx);

                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }

                return GetByIdCore(conn, shift.Id);
            }
        }

        public Shift GetActive()
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                var shift = conn.QueryFirstOrDefault<Shift>(
                    "SELECT * FROM Shift WHERE status = 'open' ORDER BY number DESC LIMIT 1");
                if (shift != null)
                    shift.OpenedBy = ResolveUserName(conn, shift.OpenedBy);
                return shift;
            }
        }

        public Shift GetById(string id)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
                return GetByIdCore(conn, id);
        }

        public ShiftPageResult GetPaged(string status, int page, int pageSize)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                var clauses = new List<string>();
                var parameters = new DynamicParameters();

                if (!string.IsNullOrWhiteSpace(status) && status != "all")
                {
                    clauses.Add("status = @status");
                    parameters.Add("status", status);
                }
                parameters.Add("pageSize", pageSize);
                parameters.Add("offset", (page - 1) * pageSize);

                var where = clauses.Count > 0 ? " WHERE " + string.Join(" AND ", clauses) : "";

                var total = conn.ExecuteScalar<int>($"SELECT COUNT(1) FROM Shift{where}", parameters);
                var items = conn.Query<Shift>(
                    $"SELECT * FROM Shift{where} ORDER BY number DESC LIMIT @pageSize OFFSET @offset",
                    parameters).ToList();

                foreach (var shift in items)
                    shift.OpenedBy = ResolveUserName(conn, shift.OpenedBy);

                return new ShiftPageResult { Items = items, Total = total };
            }
        }

        public Shift Close(string shiftId, double countedCash)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        var shift = conn.QueryFirstOrDefault<Shift>(
                            "SELECT * FROM Shift WHERE id = @id", new { id = shiftId }, transaction: tx);
                        if (shift == null)
                            throw new NotFoundException("Shift not found");
                        if (shift.Status != "open")
                            throw new DomainValidationException("Only an open shift can be closed");

                        var expected = shift.OpeningCash + SumShiftCash(conn, tx, shift.Id);

                        shift.ClosedAt = DateTime.Now;
                        shift.CountedCash = Math.Round(countedCash, 2);
                        shift.ExpectedCash = Math.Round(expected, 2);
                        shift.Difference = Math.Round(countedCash - expected, 2);
                        shift.Status = "closed";

                        conn.Execute(@"
                            UPDATE Shift SET closedAt = @closedAt, countedCash = @countedCash,
                                expectedCash = @expectedCash, difference = @difference, status = @status
                            WHERE id = @id",
                            new
                            {
                                closedAt = shift.ClosedAt.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                                countedCash = shift.CountedCash,
                                expectedCash = shift.ExpectedCash,
                                difference = shift.Difference,
                                status = shift.Status,
                                id = shift.Id
                            }, transaction: tx);

                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }

                return GetByIdCore(conn, shiftId);
            }
        }

        public ShiftReport GetReport(string shiftId)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                var shift = GetByIdCore(conn, shiftId);
                if (shift == null)
                    throw new NotFoundException("Shift not found");

                var rows = conn.Query<ReportRow>(
                    @"SELECT p.id AS Id, p.amount AS Amount, p.clientId AS ClientId, p.supplierId AS SupplierId,
                             p.reference AS Reference, p.date AS Date,
                             i.id AS SaleInvoiceId, pi.id AS PurchaseInvoiceId
                      FROM Payment p
                      LEFT JOIN Invoice i ON i.id = p.invoiceId
                      LEFT JOIN PurchaseInvoice pi ON pi.id = p.invoiceId
                      WHERE p.shiftId = @id AND p.paymentMethod = 'cash'
                      ORDER BY p.date, p.rowid",
                    new { id = shift.Id }).ToList();

                var report = new ShiftReport
                {
                    Shift = shift,
                    OpeningCash = shift.OpeningCash,
                    Entries = new List<ShiftReportEntry>()
                };

                report.Entries.Add(new ShiftReportEntry
                {
                    Date = shift.OpenedAt,
                    Description = $"Opening cash (shift #{shift.Number})",
                    Amount = shift.OpeningCash
                });

                foreach (var row in rows)
                {
                    // The drawer bucket is decided by the business transaction the payment
                    // belongs to (sale / purchase / client payment / supplier payment), NOT by
                    // Payment.clientId being set — walk-in sales store clientId = NULL.
                    var split = ShiftCashRules.FromPayment(
                        row.SaleInvoiceId != null, row.PurchaseInvoiceId != null, row.ClientId != null, row.Amount);
                    string label;

                    switch (split.Bucket)
                    {
                        case ShiftCashRules.Bucket.CashSale:
                            report.CashSales += split.Delta;
                            label = $"Cash sale {row.Reference}";
                            break;
                        case ShiftCashRules.Bucket.SaleRefund:
                            report.SaleRefunds += split.Delta;
                            label = $"Sale refund {row.Reference}";
                            break;
                        case ShiftCashRules.Bucket.OtherCashIn:
                            report.OtherCashIn += split.Delta;
                            label = $"Client payment ({row.Reference ?? "cash"})";
                            break;
                        case ShiftCashRules.Bucket.SupplierRefundIn:
                            report.SupplierRefundsIn += split.Delta;
                            label = $"Purchase refund {row.Reference}";
                            break;
                        default:
                            report.SupplierPaymentsOut += split.Delta;
                            label = $"Supplier payment ({row.Reference ?? "cash"})";
                            break;
                    }

                    report.Entries.Add(new ShiftReportEntry
                    {
                        Date = DateTime.Parse(row.Date),
                        Description = label,
                        Amount = split.DrawerEffect
                    });
                }

                var expenseRows = conn.Query<ExpenseRow>(
                    @"SELECT e.amount AS Amount, e.description AS Description, c.name AS CategoryName, e.date AS Date
                      FROM Expense e
                      JOIN ExpenseCategory c ON c.id = e.categoryId
                      WHERE e.shiftId = @id AND e.paymentMethod = 'cash'
                      ORDER BY e.date, e.rowid",
                    new { id = shift.Id }).ToList();

                foreach (var exp in expenseRows)
                {
                    report.ExpensesOut += exp.Amount;
                    report.Entries.Add(new ShiftReportEntry
                    {
                        Date = DateTime.Parse(exp.Date),
                        Description = $"Expense — {exp.CategoryName}{(string.IsNullOrEmpty(exp.Description) ? "" : $" ({exp.Description})")}",
                        Amount = -exp.Amount
                    });
                }

                report.ExpectedCash = Math.Round(
                    report.OpeningCash + report.CashSales - report.SaleRefunds
                    + report.OtherCashIn - report.SupplierPaymentsOut + report.SupplierRefundsIn
                    - report.ExpensesOut, 2);

                return report;
            }
        }

        public PagedResult<Invoice> GetShiftInvoices(string shiftId, int page, int pageSize)
        {
            using (var conn = DbConnectionFactory.CreateConnection())
            {
                var total = conn.ExecuteScalar<int>(
                    "SELECT COUNT(1) FROM Invoice WHERE shiftId = @id",
                    new { id = shiftId });

                var items = conn.Query<Invoice>(
                    "SELECT * FROM Invoice WHERE shiftId = @id ORDER BY createdAt DESC, rowid DESC LIMIT @pageSize OFFSET @offset",
                    new { id = shiftId, pageSize, offset = (page - 1) * pageSize }).ToList();

                foreach (var inv in items)
                    AttachClient(conn, inv);

                return new PagedResult<Invoice> { Items = items, Total = total };
            }
        }

        /// <summary>Signed sum of a shift's cash drawer movements: each row's signed drawer effect
        /// (sale +, refund −, supplier money out −, refund in +) exactly as the shift report
        /// classifies it. Cash expenses (Phase 10) are drawer outflows and subtract directly.</summary>
        private static double SumShiftCash(SqliteConnection conn, SqliteTransaction tx, string shiftId)
        {
            var rows = conn.Query<ShiftCashRow>(
                @"SELECT p.clientId AS ClientId, p.amount AS Amount, i.id AS SaleInvoiceId, pi.id AS PurchaseInvoiceId
                  FROM Payment p
                  LEFT JOIN Invoice i ON i.id = p.invoiceId
                  LEFT JOIN PurchaseInvoice pi ON pi.id = p.invoiceId
                  WHERE p.shiftId = @shiftId AND p.paymentMethod = 'cash'",
                new { shiftId }, transaction: tx).ToList();

            var payments = rows.Sum(r => ShiftCashRules.FromPayment(
                r.SaleInvoiceId != null, r.PurchaseInvoiceId != null, r.ClientId != null, r.Amount).DrawerEffect);
            var expenses = conn.ExecuteScalar<double>(
                "SELECT COALESCE(SUM(amount), 0) FROM Expense WHERE shiftId = @shiftId AND paymentMethod = 'cash'",
                new { shiftId }, transaction: tx);

            return payments - expenses;
        }

        private static string ResolveUserName(SqliteConnection conn, string userId)
        {
            if (string.IsNullOrEmpty(userId)) return userId;
            return conn.ExecuteScalar<string>("SELECT username FROM User WHERE id = @id", new { id = userId }) ?? userId;
        }

        private static void AttachClient(SqliteConnection conn, Invoice inv)
        {
            if (string.IsNullOrEmpty(inv.ClientId)) return;
            inv.Client = conn.QueryFirstOrDefault<Client>("SELECT * FROM Client WHERE id = @id", new { id = inv.ClientId });
        }

        private static Shift GetByIdCore(SqliteConnection conn, string id)
        {
            var shift = conn.QueryFirstOrDefault<Shift>("SELECT * FROM Shift WHERE id = @id", new { id });
            if (shift != null)
                shift.OpenedBy = ResolveUserName(conn, shift.OpenedBy);
            return shift;
        }

        private sealed class ShiftCashRow
        {
            public string ClientId { get; set; }
            public double Amount { get; set; }
            public string SaleInvoiceId { get; set; }
            public string PurchaseInvoiceId { get; set; }
        }

        private sealed class ReportRow
        {
            public string Id { get; set; }
            public double Amount { get; set; }
            public string ClientId { get; set; }
            public string SupplierId { get; set; }
            public string Reference { get; set; }
            public string Date { get; set; }
            public string SaleInvoiceId { get; set; }
            public string PurchaseInvoiceId { get; set; }
        }

        private sealed class ExpenseRow
        {
            public double Amount { get; set; }
            public string Description { get; set; }
            public string CategoryName { get; set; }
            public string Date { get; set; }
        }
    }
}
