using System;

namespace PosCs.Domain.Entities
{
    /// <summary>Independent money movement (plan Phase 5, spec §19). Paid amounts are never
    /// stored on invoices — they are always derived by summing these rows.</summary>
    public class Payment
    {
        public string Id { get; set; }
        public double Amount { get; set; }
        public string PaymentMethod { get; set; }
        public DateTime Date { get; set; }
        public string InvoiceId { get; set; }
        public string ClientId { get; set; }
        public string SupplierId { get; set; }
        public string Reference { get; set; }
        public string Notes { get; set; }
        public string CreatedBy { get; set; }
        /// <summary>The shift that was open when the money moved (plan Phase 9); null for
        /// movements outside any shift.</summary>
        public string ShiftId { get; set; }

        /// <summary>Navigation properties populated when listing.</summary>
        public Client Client { get; set; }
        public Supplier Supplier { get; set; }
        public string InvoiceNumber { get; set; }
    }
}
