using System;
using System.Collections.Generic;
using PosCs.Application.Ports;
using PosCs.Domain.Entities;
using PosCs.Domain.Exceptions;

namespace PosCs.Application.Services
{
    /// <summary>Payment use cases (plan Phase 5, spec §19–§21). Payments are independent,
    /// append-only transactions; paid/remaining/status are derived by consumers.</summary>
    public class PaymentService
    {
        private static readonly HashSet<string> AllowedMethods =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "cash", "card", "bank_transfer" };

        private readonly IPaymentRepository _payments;
        private readonly IClientRepository _clients;
        private readonly ISupplierRepository _suppliers;
        private readonly IInvoiceRepository _invoices;
        private readonly IPurchaseRepository _purchases;

        public PaymentService(IPaymentRepository payments, IClientRepository clients,
            ISupplierRepository suppliers, IInvoiceRepository invoices, IPurchaseRepository purchases)
        {
            _payments = payments;
            _clients = clients;
            _suppliers = suppliers;
            _invoices = invoices;
            _purchases = purchases;
        }

        public PaymentPageResult GetPaged(string clientId, string supplierId, string invoiceId, int page, int pageSize)
        {
            if (pageSize > 100) pageSize = 100;
            if (page < 1) page = 1;
            return _payments.GetPaged(clientId, supplierId, invoiceId, page, pageSize);
        }

        /// <summary>Paid totals per invoice/purchase id; remaining + status are derived by callers
        /// from their own totals (status is never stored, spec §20).</summary>
        public Dictionary<string, double> GetPaidTotals(IEnumerable<string> invoiceIds)
        {
            return _payments.SumGroupedByInvoice(invoiceIds);
        }

        public Payment Create(CreatePaymentRequest request, string userId)
        {
            if (request.Amount <= 0)
                throw new DomainValidationException("Amount must be greater than zero");

            if (string.IsNullOrWhiteSpace(request.ClientId) && string.IsNullOrWhiteSpace(request.SupplierId))
                throw new DomainValidationException("A payment must target a client or a supplier");
            if (!string.IsNullOrWhiteSpace(request.ClientId) && !string.IsNullOrWhiteSpace(request.SupplierId))
                throw new DomainValidationException("A payment cannot target both a client and a supplier");

            if (!AllowedMethods.Contains(request.PaymentMethod ?? ""))
                throw new DomainValidationException("Invalid payment method");

            if (!string.IsNullOrWhiteSpace(request.ClientId) && _clients.GetById(request.ClientId.Trim()) == null)
                throw new NotFoundException("Client not found");
            if (!string.IsNullOrWhiteSpace(request.SupplierId) && _suppliers.GetById(request.SupplierId.Trim()) == null)
                throw new NotFoundException("Supplier not found");

            var payment = new Payment
            {
                Id = Guid.NewGuid().ToString("N"),
                Amount = Math.Round(request.Amount, 2),
                PaymentMethod = request.PaymentMethod.ToLowerInvariant(),
                Date = request.Date ?? DateTime.Now,
                InvoiceId = NullIfEmpty(request.InvoiceId),
                ClientId = NullIfEmpty(request.ClientId),
                SupplierId = NullIfEmpty(request.SupplierId),
                Reference = NullIfEmpty(request.Reference),
                Notes = NullIfEmpty(request.Notes),
                CreatedBy = userId
            };

            if (payment.InvoiceId != null)
                ValidateInvoiceLink(payment);

            return _payments.Create(payment);
        }

        /// <summary>An invoice-linked payment must reference an existing invoice that belongs
        /// to the same party as the payment itself.</summary>
        private void ValidateInvoiceLink(Payment payment)
        {
            if (payment.ClientId != null)
            {
                var invoice = _invoices.GetById(payment.InvoiceId);
                if (invoice == null)
                    throw new NotFoundException("Invoice not found");
            }
            else
            {
                var purchase = _purchases.GetById(payment.InvoiceId);
                if (purchase == null)
                    throw new NotFoundException("Purchase invoice not found");
            }
        }

        private static string NullIfEmpty(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
