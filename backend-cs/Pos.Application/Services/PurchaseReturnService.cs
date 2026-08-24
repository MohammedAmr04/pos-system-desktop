using System;
using System.Collections.Generic;
using System.Linq;
using PosCs.Application.Models;
using PosCs.Application.Ports;
using PosCs.Domain.Entities;
using PosCs.Domain.Exceptions;

namespace PosCs.Application.Services
{
    /// <summary>Purchase-return rules (plan Phase 8, spec §25): returns target a posted
    /// purchase, never exceed purchased-minus-returned, and refund at the historical line
    /// cost. Stock/FIFO/payment side effects live in the repository.</summary>
    public class PurchaseReturnService
    {
        private readonly IPurchaseReturnRepository _returns;
        private readonly IPurchaseRepository _purchases;

        public PurchaseReturnService(IPurchaseReturnRepository returns, IPurchaseRepository purchases)
        {
            _returns = returns;
            _purchases = purchases;
        }

        public PurchaseReturn Create(string purchaseInvoiceId, CreatePurchaseReturnRequest dto, string userId)
        {
            if (dto?.Items == null || dto.Items.Count == 0)
                throw new DomainValidationException("No return items provided");
            if (string.IsNullOrWhiteSpace(purchaseInvoiceId))
                throw new NotFoundException("Purchase invoice not found");

            var invoice = _purchases.GetById(purchaseInvoiceId);
            if (invoice == null)
                throw new NotFoundException("Purchase invoice not found");
            if (invoice.Status != "posted")
                throw new DomainValidationException("Only posted purchase invoices can be returned");

            var linesById = (invoice.Items ?? new List<PurchaseInvoiceItem>())
                .GroupBy(i => i.Id)
                .ToDictionary(g => g.Key, g => g.First());

            // Reject duplicate targets inside one request so per-line validation stays exact.
            var requestedIds = dto.Items.Select(i => i?.PurchaseItemId).ToList();
            if (requestedIds.Any(id => string.IsNullOrWhiteSpace(id)))
                throw new DomainValidationException("Return item is missing its original line reference");
            if (requestedIds.GroupBy(id => id).Any(g => g.Count() > 1))
                throw new DomainValidationException("Duplicate return lines are not allowed");

            var alreadyReturned = _returns.SumReturnedByPurchase(invoice.Id);

            var details = new List<PurchaseReturnDetail>();
            foreach (var item in dto.Items)
            {
                if (!linesById.TryGetValue(item.PurchaseItemId, out var line))
                    throw new DomainValidationException("Return line does not belong to the original purchase");

                var returned = alreadyReturned.TryGetValue(line.Id, out var r) ? r : 0;
                var available = line.Quantity - returned;
                if (item.Quantity <= 0 || item.Quantity > available + 1e-9)
                    throw new DomainValidationException(
                        $"Invalid return quantity; maximum available is {Math.Max(available, 0)}");

                details.Add(new PurchaseReturnDetail
                {
                    PurchaseItemId = line.Id,
                    ProductId = line.ProductId,
                    UnitName = line.UnitName,
                    Quantity = item.Quantity,
                    QuantityFactor = line.QuantityFactor > 0 ? line.QuantityFactor : 1
                });
            }

            var purchaseReturn = new PurchaseReturn
            {
                PurchaseInvoiceId = invoice.Id,
                Notes = dto.Notes,
                CreatedBy = userId
            };

            return _returns.Create(purchaseReturn, details);
        }

        public PurchaseReturnPageResult GetPaged(int page, int pageSize, string purchaseInvoiceId)
        {
            return _returns.GetPaged(page, pageSize, purchaseInvoiceId);
        }
    }
}
