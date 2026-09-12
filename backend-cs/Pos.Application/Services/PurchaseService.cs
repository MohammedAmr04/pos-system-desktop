using System;
using System.Collections.Generic;
using System.Linq;
using PosCs.Application.Models;
using PosCs.Application.Ports;
using PosCs.Domain.Entities;
using PosCs.Domain.Exceptions;

namespace PosCs.Application.Services
{
    /// <summary>Purchase invoice use cases (spec §11–§13). Drafts are side-effect free;
    /// posting adds stock through the ledger; cancelled invoices are immutable.</summary>
    public class PurchaseService
    {
        private readonly IPurchaseRepository _purchases;

        public PurchaseService(IPurchaseRepository purchases)
        {
            _purchases = purchases;
        }

        public PurchaseInvoicePageResult GetPaged(string status, string query, int page, int pageSize)
        {
            if (pageSize > 100) pageSize = 100;
            if (page < 1) page = 1;
            return _purchases.GetPaged(status, query, page, pageSize);
        }

        public PurchaseInvoice GetById(string id)
        {
            return _purchases.GetById(id);
        }

        public PurchaseInvoice Save(SavePurchaseRequest request, string userId)
        {
            return SaveCore(request, userId, existingId: null);
        }

        /// <summary>Edits an existing invoice. Editing a posted purchase is a transactional
        /// reversal + re-application inside the repository (spec §13); cancelled are immutable.
        /// Posted invoices preserve their status when edited (only header fields are mutable).</summary>
        public PurchaseInvoice Update(string id, SavePurchaseRequest request)
        {
            var existing = _purchases.GetById(id);
            if (existing.Status == "posted" && request.Status == "draft")
                request.Status = "posted";
            return SaveCore(request, existing.CreatedBy, existingId: id);
        }

        private PurchaseInvoice SaveCore(SavePurchaseRequest request, string userId, string existingId)
        {
            Validate(request, targetStatus: request.Status == "posted" ? "posted" : "draft");

            var invoice = new PurchaseInvoice
            {
                Id = existingId,
                SupplierId = NullIfEmpty(request.SupplierId),
                SupplierInvoiceNumber = NullIfEmpty(request.SupplierInvoiceNumber),
                Date = request.Date ?? DateTime.Now,
                PaymentMethod = request.PaymentMethod == "credit" ? "credit" : "cash",
                Status = request.Status == "posted" ? "posted" : "draft",
                Discount = request.Discount,
                Tax = request.Tax,
                Notes = NullIfEmpty(request.Notes),
                CreatedBy = userId
            };

            var items = request.Lines.Select(ToItem).ToList();
            RecalculateTotals(invoice, items);
            return _purchases.Save(invoice, items);
        }

        public PurchaseInvoice Post(string id)
        {
            var existing = _purchases.GetById(id);
            if (existing.Status != "draft")
                throw new DomainValidationException("Only draft purchase invoices can be posted");
            return RepostAs(existing, "posted");
        }

        public PurchaseInvoice Cancel(string id)
        {
            return _purchases.Cancel(id);
        }

        /// <summary>Re-posts an invoice at a target status through the transactional Save path —
        /// posted edits are reversal + re-application, never direct updates (spec §13).</summary>
        private PurchaseInvoice RepostAs(PurchaseInvoice existing, string targetStatus)
        {
            var request = new SavePurchaseRequest
            {
                SupplierId = existing.SupplierId,
                SupplierInvoiceNumber = existing.SupplierInvoiceNumber,
                Date = existing.Date,
                PaymentMethod = existing.PaymentMethod,
                Status = targetStatus,
                Discount = existing.Discount,
                Tax = existing.Tax,
                Notes = existing.Notes,
                Lines = (existing.Items ?? new List<PurchaseInvoiceItem>()).Select(i => new PurchaseLineRequest
                {
                    ProductId = i.ProductId,
                    ProductUnitId = i.ProductUnitId,
                    Quantity = i.Quantity,
                    UnitCost = i.UnitCost,
                    NewRetailPrice = i.NewRetailPrice,
                    NewWholesalePrice = i.NewWholesalePrice
                }).ToList()
            };

            return SaveCore(request, existing.CreatedBy, existing.Id);
        }

        private static void RecalculateTotals(PurchaseInvoice invoice, List<PurchaseInvoiceItem> items)
        {
            foreach (var item in items)
                item.LineTotal = Math.Round(item.Quantity * item.UnitCost, 2);
            invoice.Subtotal = Math.Round(items.Sum(i => i.LineTotal), 2);
            invoice.Total = Math.Round(invoice.Subtotal - invoice.Discount + invoice.Tax, 2);
        }

        private static void Validate(SavePurchaseRequest request, string targetStatus)
        {
            if (request.Lines == null || request.Lines.Count == 0)
                throw new DomainValidationException("Purchase invoice requires at least one line");
            if (request.PaymentMethod == "credit" && string.IsNullOrWhiteSpace(request.SupplierId))
                throw new DomainValidationException("Credit purchases require a supplier");
            if (targetStatus != "posted") return;

            foreach (var line in request.Lines)
            {
                if (line.Quantity <= 0)
                    throw new DomainValidationException("Line quantities must be greater than zero");
                if (line.UnitCost < 0)
                    throw new DomainValidationException("Line cost cannot be negative");
            }
            if (request.Discount < 0 || request.Tax < 0)
                throw new DomainValidationException("Discount and tax cannot be negative");
        }

        private static PurchaseInvoiceItem ToItem(PurchaseLineRequest line)
        {
            return new PurchaseInvoiceItem
            {
                ProductId = line.ProductId,
                ProductUnitId = line.ProductUnitId,
                Quantity = line.Quantity,
                UnitCost = line.UnitCost,
                LineTotal = Math.Round(line.Quantity * line.UnitCost, 2),
                NewRetailPrice = line.NewRetailPrice,
                NewWholesalePrice = line.NewWholesalePrice
            };
        }

        private static string NullIfEmpty(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
