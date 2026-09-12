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
    public class RecordingClientRepository : IClientRepository
    {
        public Client Existing;
        public string LastBalanceFilter = "not-called";

        public List<Client> GetAll() { return new List<Client>(); }

        public PagedResult<Client> GetPaged(int page, int pageSize, string query, string balanceFilter)
        {
            LastBalanceFilter = balanceFilter;
            return new PagedResult<Client> { Items = new List<Client>(), Total = 0 };
        }

        public Client GetById(string id) { return Existing != null && Existing.Id == id ? Existing : null; }
        public Client Create(Client client) { return client; }
        public Client Update(Client client) { return client; }
    }

    public class StubClientInvoiceRepository : IInvoiceRepository
    {
        public List<Invoice> Posted = new List<Invoice>();

        public Invoice GetById(string id) { return Posted.FirstOrDefault(i => i.Id == id); }
        public List<Invoice> GetRange(DateTime? from, DateTime? to) { return new List<Invoice>(); }

        public InvoicePageResult GetPaged(DateTime? from, DateTime? to, string query, string status, int page, int pageSize, string employeeId = null)
        {
            return new InvoicePageResult { Items = new List<Invoice>(), Total = 0 };
        }

        public Invoice Create(Invoice invoice, List<InvoiceDetail> items) { return invoice; }
        public Invoice Update(string id, Invoice invoice, List<InvoiceDetail> lines) { return invoice; }
        public Invoice Post(string id) { return GetById(id); }
        public Invoice Cancel(string id) { return GetById(id); }
        public List<Invoice> ListPostedByClient(string clientId) { return Posted.Where(i => i.ClientId == clientId).ToList(); }
    }

    public class ClientServiceTests
    {
        private static ClientService ServiceWith(RecordingClientRepository clients,
            FakePaymentRepository payments = null, StubClientInvoiceRepository invoices = null)
        {
            return new ClientService(clients, payments ?? new FakePaymentRepository(),
                invoices ?? new StubClientInvoiceRepository());
        }

        [Fact]
        public void GetPaged_InvalidBalanceFilter_Throws()
        {
            var service = ServiceWith(new RecordingClientRepository());

            var ex = Assert.Throws<DomainValidationException>(() =>
                service.GetPaged(1, 20, null, "bogus"));
            Assert.Contains("balance filter", ex.Message);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("", null)]
        [InlineData("all", null)]
        [InlineData(" ALL ", null)]
        [InlineData("positive", "positive")]
        [InlineData(" Positive ", "positive")]
        [InlineData("NEGATIVE", "negative")]
        [InlineData("zero", "zero")]
        public void GetPaged_NormalizesBalanceFilter(string input, string expected)
        {
            var clients = new RecordingClientRepository();
            var service = ServiceWith(clients);

            service.GetPaged(1, 20, null, input);

            Assert.Equal(expected, clients.LastBalanceFilter);
        }

        [Fact]
        public void GetInvoices_UnknownClient_ThrowsNotFound()
        {
            var service = ServiceWith(new RecordingClientRepository());

            Assert.Throws<NotFoundException>(() => service.GetInvoices("missing"));
        }

        [Fact]
        public void GetInvoices_ReturnsItemsWithPaidMap()
        {
            var clients = new RecordingClientRepository
            {
                Existing = new Client { Id = "c1", Name = "Ahmed" }
            };
            var invoices = new StubClientInvoiceRepository
            {
                Posted = new List<Invoice>
                {
                    new Invoice { Id = "i1", ClientId = "c1", InvoiceNumber = 1, TotalAmount = 100 },
                    new Invoice { Id = "i2", ClientId = "c1", InvoiceNumber = 2, TotalAmount = 200 },
                    new Invoice { Id = "i9", ClientId = "other", InvoiceNumber = 9, TotalAmount = 999 }
                }
            };
            var payments = new FakePaymentRepository();
            payments.Stored.Add(new Payment { Id = "p1", InvoiceId = "i1", ClientId = "c1", Amount = 40 });
            var service = ServiceWith(clients, payments, invoices);

            var result = service.GetInvoices("c1");

            Assert.Equal(2, result.Items.Count);
            Assert.Equal(40, result.PaidByInvoice["i1"]);
            Assert.Equal(0, result.PaidByInvoice["i2"]);
        }

        [Fact]
        public void GetInvoices_AllocatesGeneralPaymentsOldestFirst()
        {
            var clients = new RecordingClientRepository
            {
                Existing = new Client { Id = "c1", Name = "Ahmed" }
            };
            var invoices = new StubClientInvoiceRepository
            {
                Posted = new List<Invoice>
                {
                    new Invoice { Id = "i1", ClientId = "c1", InvoiceNumber = 1, TotalAmount = 1000 },
                    new Invoice { Id = "i2", ClientId = "c1", InvoiceNumber = 2, TotalAmount = 1000 }
                }
            };
            var payments = new FakePaymentRepository();
            payments.Stored.Add(new Payment { Id = "p1", InvoiceId = null, ClientId = "c1", Amount = 1500 });
            var service = ServiceWith(clients, payments, invoices);

            var result = service.GetInvoices("c1");

            Assert.Equal(1000, result.PaidByInvoice["i1"]);
            Assert.Equal(500, result.PaidByInvoice["i2"]);
        }

        [Fact]
        public void GetInvoices_CombinesLinkedAndGeneralPayments()
        {
            var clients = new RecordingClientRepository
            {
                Existing = new Client { Id = "c1", Name = "Ahmed" }
            };
            var invoices = new StubClientInvoiceRepository
            {
                Posted = new List<Invoice>
                {
                    new Invoice { Id = "i1", ClientId = "c1", InvoiceNumber = 1, TotalAmount = 1000 },
                    new Invoice { Id = "i2", ClientId = "c1", InvoiceNumber = 2, TotalAmount = 1000 }
                }
            };
            var payments = new FakePaymentRepository();
            payments.Stored.Add(new Payment { Id = "p1", InvoiceId = "i1", ClientId = "c1", Amount = 400 });
            payments.Stored.Add(new Payment { Id = "p2", InvoiceId = null, ClientId = "c1", Amount = 800 });
            var service = ServiceWith(clients, payments, invoices);

            var result = service.GetInvoices("c1");

            Assert.Equal(1000, result.PaidByInvoice["i1"]);
            Assert.Equal(200, result.PaidByInvoice["i2"]);
        }
    }
}
