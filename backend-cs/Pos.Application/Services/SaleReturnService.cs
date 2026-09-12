using System;
using System.Collections.Generic;
using System.Linq;
using PosCs.Application.Models;
using PosCs.Application.Ports;
using PosCs.Domain.Entities;
using PosCs.Domain.Exceptions;

namespace PosCs.Application.Services
{
    /// <summary>Sales-return rules (plan Phase 7, spec §23): returns target a posted invoice,
    /// never exceed sold-minus-returned, refund at the historical line price, and reuse the
    /// original sale's payment method. Stock/FIFO/payment side effects live in the repository.</summary>
    public class SaleReturnService
    {
        private readonly ISaleReturnRepository _returns;
        private readonly IInvoiceRepository _invoices;

        public SaleReturnService(ISaleReturnRepository returns, IInvoiceRepository invoices)
        {
            _returns = returns;
            _invoices = invoices;
        }

        public SaleReturn Create(string invoiceId, CreateSaleReturnRequest dto, string userId)
        {
            if (dto?.Items == null || dto.Items.Count == 0)
                throw new DomainValidationException("No return items provided");
            if (string.IsNullOrWhiteSpace(invoiceId))
                throw new NotFoundException("Invoice not found");

            var invoice = _invoices.GetById(invoiceId);
            if (invoice == null)
                throw new NotFoundException("Invoice not found");
            if (invoice.Status != "posted")
                throw new DomainValidationException("Only posted invoices can be returned");

            var linesById = (invoice.InvoiceDetail ?? new List<InvoiceDetail>())
                .GroupBy(d => d.Id)
                .ToDictionary(g => g.Key, g => g.First());

            // Reject duplicate targets inside one request so per-line validation stays exact.
            var requestedIds = dto.Items.Select(i => i?.InvoiceDetailId).ToList();
            if (requestedIds.Any(id => string.IsNullOrWhiteSpace(id)))
                throw new DomainValidationException("Return item is missing its original line reference");
            if (requestedIds.GroupBy(id => id).Any(g => g.Count() > 1))
                throw new DomainValidationException("Duplicate return lines are not allowed");

            var alreadyReturned = _returns.SumReturnedByInvoice(invoice.Id);

            var details = new List<SaleReturnDetail>();
            foreach (var item in dto.Items)
            {
                if (!linesById.TryGetValue(item.InvoiceDetailId, out var line))
                    throw new DomainValidationException("Return line does not belong to the original invoice");

                var factor = line.QuantityFactor > 0 ? line.QuantityFactor : 1;
                var returned = alreadyReturned.TryGetValue(line.Id, out var r) ? r : 0;
                var available = line.Quantity - returned;
                if (item.Quantity <= 0 || item.Quantity > available + 1e-9)
                    throw new DomainValidationException(
                        $"Invalid return quantity; maximum available is {Math.Max(available, 0)}");

                details.Add(new SaleReturnDetail
                {
                    InvoiceDetailId = line.Id,
                    ProductId = line.ProductId,
                    UnitName = line.UnitName,
                    Quantity = item.Quantity,
                    QuantityFactor = factor
                });
            }

            var saleReturn = new SaleReturn
            {
                InvoiceId = invoice.Id,
                Notes = dto.Notes,
                CreatedBy = userId
            };

            return _returns.Create(saleReturn, details);
        }

        public SaleReturnPageResult GetPaged(int page, int pageSize, string invoiceId)
        {
            return _returns.GetPaged(page, pageSize, invoiceId);
        }
    }
}
