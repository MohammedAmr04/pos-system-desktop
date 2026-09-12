using System;
using System.Collections.Generic;
using PosCs.Domain.Entities;

namespace PosCs.Application.Ports
{
    /// <summary>Purchase invoice persistence. Lifecycle transitions are transactional and
    /// append-only on the ledger — posted rows are never deleted (spec §12/§13).</summary>
    public interface IPurchaseRepository
    {
        PurchaseInvoicePageResult GetPaged(string status, string query, int page, int pageSize);

        PurchaseInvoice GetById(string id);

        /// <summary>
        /// Single transactional entry point for create/update across the lifecycle:
        /// new draft/posted, draft edit, draft→post, and posted edit as an in-transaction
        /// reversal + re-application (spec §13). Cancelled invoices are immutable.
        /// </summary>
        PurchaseInvoice Save(PurchaseInvoice invoice, List<PurchaseInvoiceItem> items);

        /// <summary>Safe cancellation of a posted purchase — appends reversal rows, deletes nothing.</summary>
        PurchaseInvoice Cancel(string id);

        /// <summary>Posted purchases for one supplier, oldest first (account statements).</summary>
        List<PurchaseInvoice> ListPostedBySupplier(string supplierId);
    }

    public sealed class PurchaseInvoicePageResult
    {
        public List<PurchaseInvoice> Items { get; set; }
        public int Total { get; set; }
        public double PostedTotal { get; set; }
    }

    public sealed class PurchaseLineRequest
    {
        public string ProductId { get; set; }
        public string ProductUnitId { get; set; }
        public double Quantity { get; set; }
        public double UnitCost { get; set; }
        public double? NewRetailPrice { get; set; }
        public double? NewWholesalePrice { get; set; }
    }

    public sealed class SavePurchaseRequest
    {
        public string SupplierId { get; set; }
        public string SupplierInvoiceNumber { get; set; }
        public DateTime? Date { get; set; }
        /// <summary>'cash' | 'credit'.</summary>
        public string PaymentMethod { get; set; }
        /// <summary>'draft' | 'posted' — 'posted' saves AND posts in one call.</summary>
        public string Status { get; set; }
        public double Discount { get; set; }
        public double Tax { get; set; }
        public string Notes { get; set; }
        public List<PurchaseLineRequest> Lines { get; set; }
    }
}
