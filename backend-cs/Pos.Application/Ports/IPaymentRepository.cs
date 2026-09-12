using System;
using System.Collections.Generic;
using PosCs.Domain.Entities;

namespace PosCs.Application.Ports
{
    /// <summary>Payment persistence (plan Phase 5). Payments are append-only:
    /// no update/delete — corrections happen through new payments (spec §19).</summary>
    public interface IPaymentRepository
    {
        Payment Create(Payment payment);

        PaymentPageResult GetPaged(string clientId, string supplierId, string invoiceId, int page, int pageSize);

        /// <summary>Total paid against one invoice/purchase id.</summary>
        double SumByInvoice(string invoiceId);

        /// <summary>Total money received from a client.</summary>
        double SumByClient(string clientId);

        /// <summary>Total money paid to a supplier.</summary>
        double SumBySupplier(string supplierId);

        /// <summary>
        /// General (not invoice-linked) payments received from a client — the pool
        /// distributed oldest-first across the client's invoices (display only).
        /// </summary>
        double SumUnlinkedByClient(string clientId);

        /// <summary>
        /// General non-negative payments to a supplier — the pool distributed
        /// oldest-first across the supplier's credit purchases (display only).
        /// Negative amounts are cash-purchase refunds (drawer events), not settlements.
        /// </summary>
        double SumUnlinkedBySupplier(string supplierId);

        /// <summary>All payments from a client, oldest first (account statements).</summary>
        List<Payment> ListByClient(string clientId);

        /// <summary>All payments to a supplier, oldest first (account statements).</summary>
        List<Payment> ListBySupplier(string supplierId);

        /// <summary>Paid totals grouped by invoice/purchase id (payment-status badges).</summary>
        Dictionary<string, double> SumGroupedByInvoice(IEnumerable<string> invoiceIds);
    }

    public sealed class PaymentPageResult
    {
        public List<Payment> Items { get; set; }
        public int Total { get; set; }
    }

    public sealed class CreatePaymentRequest
    {
        public double Amount { get; set; }
        /// <summary>'cash' | 'card' | 'bank_transfer'.</summary>
        public string PaymentMethod { get; set; }
        public DateTime? Date { get; set; }
        public string InvoiceId { get; set; }
        public string ClientId { get; set; }
        public string SupplierId { get; set; }
        public string Reference { get; set; }
        public string Notes { get; set; }
    }
}
