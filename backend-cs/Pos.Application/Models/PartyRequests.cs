using System;
using System.Collections.Generic;
using PosCs.Domain.Entities;

namespace PosCs.Application.Models
{
    public sealed class CreateSupplierRequest
    {
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string Notes { get; set; }
    }

    /// <summary>Null members keep their current value; empty strings clear phone/address/notes.</summary>
    public sealed class UpdateSupplierRequest
    {
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string Notes { get; set; }
        public bool? IsActive { get; set; }
    }

    public sealed class CreateEmployeeRequest
    {
        public string Name { get; set; }
        public string Phone { get; set; }
    }

    /// <summary>Null members keep their current value; empty string clears the phone.</summary>
    public sealed class UpdateEmployeeRequest
    {
        public string Name { get; set; }
        public string Phone { get; set; }
        public bool? IsActive { get; set; }
    }

    public sealed class CreateClientRequest
    {
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string Notes { get; set; }
    }

    /// <summary>Null members keep their current value; empty strings clear phone/address/notes.</summary>
    public sealed class UpdateClientRequest
    {
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string Notes { get; set; }
        public bool? IsActive { get; set; }
    }

    /// <summary>
    /// Account statement for a party. Balance sign convention: a positive supplier balance
    /// means we owe the supplier; a positive client balance means the client owes us.
    /// Entries are populated from Phase 5 (payments) onwards; until then it is empty.
    /// </summary>
    public sealed class PartyStatementResult
    {
        public string PartyId { get; set; }
        public decimal Balance { get; set; }
        public List<PartyStatementEntry> Entries { get; set; } = new List<PartyStatementEntry>();
    }

    public sealed class PartyStatementEntry
    {
        public DateTime Date { get; set; }
        public string Description { get; set; }
        /// <summary>Amount increasing what we owe the party (purchases) or what clients owe us (sales).</summary>
        public decimal Debit { get; set; }
        /// <summary>Amount settling the balance (payments, returns).</summary>
        public decimal Credit { get; set; }
    }

    /// <summary>
    /// Posted sales of one client (oldest first) plus per-invoice paid:
    /// invoice-linked payments with the general payment pool distributed
    /// oldest-first (display only — stored payments are untouched).
    /// </summary>
    public sealed class ClientInvoicesResult
    {
        public List<Invoice> Items { get; set; } = new List<Invoice>();
        public Dictionary<string, double> PaidByInvoice { get; set; } = new Dictionary<string, double>();
    }

    /// <summary>
    /// Posted credit purchases of one supplier (oldest first) plus per-purchase paid:
    /// invoice-linked payments with the general payment pool distributed
    /// oldest-first (display only — stored payments are untouched).
    /// </summary>
    public sealed class SupplierPurchasesResult
    {
        public List<PurchaseInvoice> Items { get; set; } = new List<PurchaseInvoice>();
        public Dictionary<string, double> PaidByInvoice { get; set; } = new Dictionary<string, double>();
    }
}
