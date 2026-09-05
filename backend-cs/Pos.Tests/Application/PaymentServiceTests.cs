using System;
using System.Collections.Generic;
using System.Linq;
using PosCs.Application.Models;
using PosCs.Application.Ports;
using PosCs.Application.Services;
using PosCs.Domain.Entities;
using PosCs.Domain.Exceptions;
using Xunit;

namespace PosCs.Tests.Application
{
    public class FakePaymentRepository : IPaymentRepository
    {
        public List<Payment> Stored = new List<Payment>();

        public Payment Create(Payment payment)
        {
            Stored.Add(payment);
            return payment;
        }

        public PaymentPageResult GetPaged(string clientId, string supplierId, string invoiceId, int page, int pageSize)
        {
            IEnumerable<Payment> query = Stored;
            if (clientId != null) query = query.Where(p => p.ClientId == clientId);
            if (supplierId != null) query = query.Where(p => p.SupplierId == supplierId);
            if (invoiceId != null) query = query.Where(p => p.InvoiceId == invoiceId);
            var items = query.ToList();
            return new PaymentPageResult { Items = items, Total = items.Count };
        }

        public double SumByInvoice(string invoiceId) =>
            Stored.Where(p => p.InvoiceId == invoiceId).Sum(p => p.Amount);

        public double SumByClient(string clientId) =>
            Stored.Where(p => p.ClientId == clientId).Sum(p => p.Amount);

        public double SumBySupplier(string supplierId) =>
            Stored.Where(p => p.SupplierId == supplierId).Sum(p => p.Amount);

        public List<Payment> ListByClient(string clientId) =>
            Stored.Where(p => p.ClientId == clientId).ToList();

        public List<Payment> ListBySupplier(string supplierId) =>
            Stored.Where(p => p.SupplierId == supplierId).ToList();

        public Dictionary<string, double> SumGroupedByInvoice(System.Collections.Generic.IEnumerable<string> invoiceIds)
        {
            var set = new HashSet<string>(invoiceIds ?? Enumerable.Empty<string>());
            return Stored.Where(p => p.InvoiceId != null && set.Contains(p.InvoiceId))
                .GroupBy(p => p.InvoiceId)
                .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));
        }
    }

    public class FakePartyLookup
    {
        // Minimal stubs: the real repositories throw/return null for missing ids.
        public class StubClientRepository : IClientRepository
        {
            public Client Existing;
            public Client Create(Client client) => client;
            public Client Update(Client client) => client;
            public Client GetById(string id) => Existing != null && Existing.Id == id ? Existing : null;
            public List<Client> GetAll() => new List<Client>();
            public PagedResult<Client> GetPaged(int page, int pageSize, string query) =>
                new PagedResult<Client> { Items = new List<Client>(), Total = 0 };
        }

        public class StubSupplierRepository : ISupplierRepository
        {
            public Supplier Existing;
            public Supplier Create(Supplier supplier) => supplier;
            public Supplier Update(Supplier supplier) => supplier;
            public Supplier GetById(string id) => Existing != null && Existing.Id == id ? Existing : null;
            public List<Supplier> GetAll() => new List<Supplier>();
            public PagedResult<Supplier> GetPaged(int page, int pageSize, string query) =>
                new PagedResult<Supplier> { Items = new List<Supplier>(), Total = 0 };
        }
    }

    public class FakeInvoiceRepository : IInvoiceRepository
    {
        public Invoice Existing;

        public Invoice Create(Invoice invoice, List<InvoiceDetail> items)
        {
            Existing = invoice;
            return invoice;
        }

        public Invoice GetById(string id) => Existing != null && Existing.Id == id ? Existing : null;

        public List<Invoice> GetRange(DateTime? from, DateTime? to) => new List<Invoice>();

        public InvoicePageResult GetPaged(DateTime? from, DateTime? to, string query, string status, int page, int pageSize, string employeeId = null) =>
            new InvoicePageResult { Items = new List<Invoice>(), Total = 0 };

        public Invoice Update(string id, Invoice invoice, List<InvoiceDetail> lines) => invoice;

        public Invoice Post(string id) => Existing;

        public Invoice Cancel(string id) => Existing;

        public List<Invoice> ListPostedByClient(string clientId) => new List<Invoice>();
    }

    public class PaymentServiceTests
    {
        private static PaymentService ServiceWith(FakePaymentRepository payments,
            FakePartyLookup.StubClientRepository clients = null,
            FakePartyLookup.StubSupplierRepository suppliers = null,
            FakeInvoiceRepository invoices = null)
        {
            return new PaymentService(
                payments,
                clients ?? new FakePartyLookup.StubClientRepository(),
                suppliers ?? new FakePartyLookup.StubSupplierRepository(),
                invoices ?? new FakeInvoiceRepository(),
                new FakePurchaseRepository());
        }

        [Fact]
        public void Amount_MustBePositive()
        {
            var request = ValidRequest(amount: 0);
            var ex = Assert.Throws<DomainValidationException>(() =>
                ServiceWith(new FakePaymentRepository()).Create(request, "user-1"));
            Assert.Contains("greater than zero", ex.Message);
        }

        [Fact]
        public void MustTargetExactlyOnePartyType()
        {
            var service = ServiceWith(new FakePaymentRepository());

            var none = Assert.Throws<DomainValidationException>(() =>
                service.Create(ValidRequest(clientId: null, supplierId: null), "user-1"));
            Assert.Contains("client or a supplier", none.Message);

            var both = Assert.Throws<DomainValidationException>(() =>
                service.Create(ValidRequest(supplierId: "s1"), "user-1"));
            Assert.Contains("cannot target both", both.Message);
        }

        [Fact]
        public void Method_MustBeKnown()
        {
            var ex = Assert.Throws<DomainValidationException>(() =>
                ServiceWith(new FakePaymentRepository()).Create(ValidRequest(method: "crypto"), "user-1"));
            Assert.Contains("Invalid payment method", ex.Message);
        }

        [Fact]
        public void UnknownParty_IsRejected()
        {
            var ex = Assert.Throws<NotFoundException>(() =>
                ServiceWith(new FakePaymentRepository()).Create(ValidRequest(), "user-1"));
            Assert.Contains("Client not found", ex.Message);
        }

        [Fact]
        public void Create_PersistsNormalizedPayment()
        {
            var payments = new FakePaymentRepository();
            var clients = new FakePartyLookup.StubClientRepository { Existing = new Client { Id = "c1", Name = "Test" } };
            var service = ServiceWith(payments, clients: clients);

            var payment = service.Create(new CreatePaymentRequest
            {
                Amount = 250.555,
                PaymentMethod = "CASH",
                ClientId = " c1 ",
                Reference = "receipt 12"
            }, "user-9");

            Assert.Equal(250.56, payment.Amount);
            Assert.Equal("cash", payment.PaymentMethod);
            Assert.Equal("c1", payment.ClientId);
            Assert.Null(payment.SupplierId);
            Assert.Equal("user-9", payment.CreatedBy);
            Assert.Single(payments.Stored);
        }

        [Fact]
        public void SupplierPayment_ForUnknownSupplier_IsRejected()
        {
            var ex = Assert.Throws<NotFoundException>(() =>
                ServiceWith(new FakePaymentRepository()).Create(
                    ValidRequest(supplierId: "nope", clientId: null), "user-1"));
            Assert.Contains("Supplier not found", ex.Message);
        }

        private static CreatePaymentRequest ValidRequest(double amount = 100, string clientId = "c1",
            string supplierId = null, string method = "cash")
        {
            return new CreatePaymentRequest
            {
                Amount = amount,
                ClientId = clientId,
                SupplierId = supplierId,
                PaymentMethod = method
            };
        }
    }
}
