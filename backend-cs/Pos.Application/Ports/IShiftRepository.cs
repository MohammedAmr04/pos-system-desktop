using System;
using System.Collections.Generic;
using PosCs.Application.Models;
using PosCs.Domain.Entities;

namespace PosCs.Application.Ports
{
    /// <summary>Shift persistence (plan Phase 9). Opening/closing are transactional;
    /// expected cash is always derived from stamped cash payments, never stored editable.</summary>
    public interface IShiftRepository
    {
        Shift Create(Shift shift);

        /// <summary>The single open shift, or null when none is active.</summary>
        Shift GetActive();

        Shift GetById(string id);

        ShiftPageResult GetPaged(string status, int page, int pageSize);

        /// <summary>Closes an open shift: derives expected cash from its stamped cash
        /// payments and stores counted/difference atomically.</summary>
        Shift Close(string shiftId, double countedCash);

        /// <summary>Cash movement breakdown for one shift (end-of-shift report).</summary>
        ShiftReport GetReport(string shiftId);

        /// <summary>Invoices created during one shift (shift detail page), newest first.</summary>
        PagedResult<Invoice> GetShiftInvoices(string shiftId, int page, int pageSize);
    }

    public sealed class ShiftPageResult
    {
        public List<Shift> Items { get; set; }
        public int Total { get; set; }
    }

    public sealed class OpenShiftRequest
    {
        public double OpeningCash { get; set; }
        public string Notes { get; set; }
    }

    public sealed class CloseShiftRequest
    {
        public double CountedCash { get; set; }
    }

    public sealed class ShiftReportEntry
    {
        public DateTime Date { get; set; }
        public string Description { get; set; }
        /// <summary>Signed drawer effect: positive = money in, negative = money out.</summary>
        public double Amount { get; set; }
    }

    public sealed class ShiftReport
    {
        public Shift Shift { get; set; }
        public double OpeningCash { get; set; }
        public double CashSales { get; set; }
        public double SaleRefunds { get; set; }
        public double OtherCashIn { get; set; }
        public double SupplierPaymentsOut { get; set; }
        public double SupplierRefundsIn { get; set; }
        /// <summary>Cash operating expenses recorded during the shift (Phase 10).</summary>
        public double ExpensesOut { get; set; }
        public double ExpectedCash { get; set; }
        public List<ShiftReportEntry> Entries { get; set; } = new List<ShiftReportEntry>();
    }
}
