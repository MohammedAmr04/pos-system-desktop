using System;
using System.Collections.Generic;
using System.Linq;
using PosCs.Application.Models;
using PosCs.Application.Ports;
using PosCs.Domain.Entities;
using PosCs.Domain.Exceptions;
using PosCs.Domain.Rules;

namespace PosCs.Application.Services
{
    /// <summary>
    /// Supplier use cases. Suppliers are deactivated once referenced — never deleted (spec §9.2).
    /// Names are intentionally NOT unique: different people/shops may share a name.
    /// </summary>
    public class SupplierService
    {
        private readonly ISupplierRepository _repo;
        private readonly IPaymentRepository _payments;
        private readonly IPurchaseRepository _purchases;
        private readonly IPurchaseReturnRepository _purchaseReturns;

        public SupplierService(ISupplierRepository repo, IPaymentRepository payments, IPurchaseRepository purchases,
            IPurchaseReturnRepository purchaseReturns)
        {
            _repo = repo;
            _payments = payments;
            _purchases = purchases;
            _purchaseReturns = purchaseReturns;
        }

        /// <summary>Active-only list for assignment pickers (inactive suppliers are not selectable).</summary>
        public List<Supplier> GetActive()
        {
            return _repo.GetAll().Where(s => s.IsActive).ToList();
        }

        public PagedResult<Supplier> GetPaged(int page, int pageSize, string query, string balanceFilter)
        {
            page = Math.Max(1, page);
            pageSize = Math.Max(1, Math.Min(pageSize, 100));
            return _repo.GetPaged(page, pageSize, query?.Trim(), NormalizeBalanceFilter(balanceFilter));
        }

        public Supplier GetById(string id)
        {
            var supplier = _repo.GetById(id);
            if (supplier == null)
                throw new NotFoundException("Supplier not found");
            return supplier;
        }

        public Supplier Create(CreateSupplierRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Name))
                throw new DomainValidationException("Supplier name is required");

            return _repo.Create(new Supplier
            {
                Name = request.Name.Trim(),
                Phone = Clean(request.Phone),
                Address = Clean(request.Address),
                Notes = Clean(request.Notes),
                IsActive = true
            });
        }

        public Supplier Update(string id, UpdateSupplierRequest request)
        {
            if (request == null)
                throw new DomainValidationException("Invalid supplier data");

            var existing = _repo.GetById(id);
            if (existing == null)
                throw new NotFoundException("Supplier not found");

            if (!string.IsNullOrWhiteSpace(request.Name))
                existing.Name = request.Name.Trim();

            // Null keeps the current value; empty string clears it.
            if (request.Phone != null)
                existing.Phone = Clean(request.Phone);
            if (request.Address != null)
                existing.Address = Clean(request.Address);
            if (request.Notes != null)
                existing.Notes = Clean(request.Notes);
            if (request.IsActive.HasValue)
                existing.IsActive = request.IsActive.Value;

            return _repo.Update(existing);
        }

        /// <summary>
        /// Account statement (plan Phase 5). Posted CREDIT purchases are what we owe (debit) —
        /// cash purchases settle instantly and never carry debt; recorded payments settle it
        /// (credit); purchase returns reduce the debt (credit, spec §9.3/§25). Auto refund
        /// payments for cash purchases are drawer events, not settlements, so they are skipped.
        /// Balance = credit purchases − returns − payments; positive means we still owe.
        /// </summary>
        public PartyStatementResult GetStatement(string id)
        {
            GetById(id);

            var entries = new List<PartyStatementEntry>();
            foreach (var purchase in _purchases.ListPostedBySupplier(id))
            {
                if (purchase.PaymentMethod == "cash")
                    continue;
                entries.Add(new PartyStatementEntry
                {
                    Date = purchase.Date,
                    Description = $"Purchase #{purchase.InvoiceNumber}",
                    Debit = (decimal)purchase.Total,
                    Credit = 0m
                });
            }
            foreach (var purchaseReturn in _purchaseReturns.ListPostedBySupplier(id))
            {
                var origin = _purchases.GetById(purchaseReturn.PurchaseInvoiceId);
                if (origin?.PaymentMethod == "cash")
                    continue;
                entries.Add(new PartyStatementEntry
                {
                    Date = purchaseReturn.Date,
                    Description = $"Purchase Return #{purchaseReturn.Number} (Purchase #{purchaseReturn.PurchaseInvoiceNumber})",
                    Debit = 0m,
                    Credit = (decimal)purchaseReturn.TotalAmount
                });
            }
            foreach (var payment in _payments.ListBySupplier(id))
            {
                // Negative amounts only exist on automatic cash-purchase refunds (drawer events).
                if (payment.Amount < 0)
                    continue;
                var label = string.IsNullOrWhiteSpace(payment.Reference)
                    ? $"Payment ({payment.PaymentMethod})"
                    : $"Payment ({payment.PaymentMethod}) - {payment.Reference}";
                entries.Add(new PartyStatementEntry
                {
                    Date = payment.Date,
                    Description = label,
                    Debit = 0m,
                    Credit = (decimal)payment.Amount
                });
            }

            decimal balance = 0m;
            foreach (var entry in entries.OrderBy(e => e.Date))
            {
                balance += entry.Debit - entry.Credit;
            }

            return new PartyStatementResult { PartyId = id, Balance = balance, Entries = entries };
        }

        /// <summary>
        /// Posted credit purchases of one supplier for the statement page table,
        /// oldest first. Per-purchase paid = invoice-linked payments plus the general
        /// (unlinked) payment pool distributed oldest-first — display only.
        /// Cash purchases settle instantly and are excluded, mirroring the statement.
        /// </summary>
        public SupplierPurchasesResult GetPurchases(string id)
        {
            GetById(id);

            var items = _purchases.ListPostedBySupplier(id).Where(p => p.PaymentMethod != "cash").ToList();
            var linked = _payments.SumGroupedByInvoice(items.Select(i => i.Id));
            var pool = _payments.SumUnlinkedBySupplier(id);
            var paid = PaymentAllocation.Allocate(
                items.Select(i => new AllocationInput
                {
                    Id = i.Id,
                    Total = i.Total,
                    LinkedPaid = linked.ContainsKey(i.Id) ? linked[i.Id] : 0
                }),
                pool);
            return new SupplierPurchasesResult { Items = items, PaidByInvoice = paid };
        }

        /// <summary>
        /// Normalizes the balance filter coming from the API: null/empty/"all" means
        /// no filtering; anything else must be a known value.
        /// </summary>
        internal static string NormalizeBalanceFilter(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            var normalized = value.Trim().ToLowerInvariant();
            if (normalized == "all")
                return null;
            if (normalized == "positive" || normalized == "negative" || normalized == "zero")
                return normalized;
            throw new DomainValidationException("Invalid balance filter. Allowed values: all, positive, negative, zero.");
        }

        private static string Clean(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
