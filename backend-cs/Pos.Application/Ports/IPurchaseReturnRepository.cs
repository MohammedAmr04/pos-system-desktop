using System;
using System.Collections.Generic;
using PosCs.Domain.Entities;

namespace PosCs.Application.Ports
{
    /// <summary>Purchase return persistence. Creation is a single transactional pipeline;
    /// posted returns are immutable history (spec §25).</summary>
    public interface IPurchaseReturnRepository
    {
        PurchaseReturn Create(PurchaseReturn purchaseReturn, List<PurchaseReturnDetail> details);

        PurchaseReturnPageResult GetPaged(int page, int pageSize, string purchaseInvoiceId);

        /// <summary>Returned quantity per purchase line id across all posted returns.</summary>
        Dictionary<string, double> SumReturnedByPurchase(string purchaseInvoiceId);

        /// <summary>Posted returns for one supplier (via the original purchase), oldest first
        /// (supplier account statements — spec §9.3).</summary>
        List<PurchaseReturn> ListPostedBySupplier(string supplierId);

        /// <summary>True when a posted purchase has at least one posted return; such purchases
        /// can no longer be edited or cancelled.</summary>
        bool HasPostedReturns(string purchaseInvoiceId);
    }

    public sealed class PurchaseReturnPageResult
    {
        public List<PurchaseReturn> Items { get; set; }
        public int Total { get; set; }
    }

    public sealed class CreatePurchaseReturnRequest
    {
        public List<CreatePurchaseReturnItem> Items { get; set; }
        public string Notes { get; set; }
    }

    public sealed class CreatePurchaseReturnItem
    {
        public string PurchaseItemId { get; set; }
        public double Quantity { get; set; }
    }
}
