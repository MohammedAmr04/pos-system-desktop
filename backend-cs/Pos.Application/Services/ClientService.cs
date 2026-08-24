using System;
using System.Collections.Generic;
using System.Linq;
using PosCs.Application.Models;
using PosCs.Application.Ports;
using PosCs.Domain.Entities;
using PosCs.Domain.Exceptions;

namespace PosCs.Application.Services
{
    /// <summary>
    /// Client use cases. Clients are deactivated once referenced — never deleted (spec §10.2),
    /// because credit sales require a stable answer to "who owes us money?".
    /// Names are intentionally NOT unique: different customers may share a name.
    /// </summary>
    public class ClientService
    {
        private readonly IClientRepository _repo;
        private readonly IPaymentRepository _payments;
        private readonly IInvoiceRepository _invoices;

        public ClientService(IClientRepository repo, IPaymentRepository payments, IInvoiceRepository invoices)
        {
            _repo = repo;
            _payments = payments;
            _invoices = invoices;
        }

        /// <summary>Active-only list for assignment pickers (inactive clients are not selectable).</summary>
        public List<Client> GetActive()
        {
            return _repo.GetAll().Where(c => c.IsActive).ToList();
        }

        public PagedResult<Client> GetPaged(int page, int pageSize, string query)
        {
            page = Math.Max(1, page);
            pageSize = Math.Max(1, Math.Min(pageSize, 100));
            return _repo.GetPaged(page, pageSize, query?.Trim());
        }

        public Client GetById(string id)
        {
            var client = _repo.GetById(id);
            if (client == null)
                throw new NotFoundException("Client not found");
            return client;
        }

        public Client Create(CreateClientRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Name))
                throw new DomainValidationException("Client name is required");

            return _repo.Create(new Client
            {
                Name = request.Name.Trim(),
                Phone = Clean(request.Phone),
                Address = Clean(request.Address),
                Notes = Clean(request.Notes),
                IsActive = true
            });
        }

        public Client Update(string id, UpdateClientRequest request)
        {
            if (request == null)
                throw new DomainValidationException("Invalid client data");

            var existing = _repo.GetById(id);
            if (existing == null)
                throw new NotFoundException("Client not found");

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
        /// Account statement (plan Phase 5 + 6). Posted credit/cash sales with this client are
        /// debits; payments received settle them. A negative balance means the client has
        /// advance credit with us. Sales returns join in Phase 7.
        /// </summary>
        public PartyStatementResult GetStatement(string id)
        {
            GetById(id);

            var entries = new List<PartyStatementEntry>();
            foreach (var invoice in _invoices.ListPostedByClient(id))
            {
                entries.Add(new PartyStatementEntry
                {
                    Date = invoice.CreatedAt,
                    Description = $"Invoice #{invoice.InvoiceNumber}",
                    Debit = (decimal)invoice.TotalAmount,
                    Credit = 0m
                });
            }
            foreach (var payment in _payments.ListByClient(id))
            {
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

        private static string Clean(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
