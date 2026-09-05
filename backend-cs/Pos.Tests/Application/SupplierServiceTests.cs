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
    public class RecordingSupplierRepository : ISupplierRepository
    {
        public Supplier Existing;
        public string LastBalanceFilter = "not-called";

        public List<Supplier> GetAll() { return new List<Supplier>(); }

        public PagedResult<Supplier> GetPaged(int page, int pageSize, string query, string balanceFilter)
        {
            LastBalanceFilter = balanceFilter;
            return new PagedResult<Supplier> { Items = new List<Supplier>(), Total = 0 };
        }

        public Supplier GetById(string id) { return Existing != null && Existing.Id == id ? Existing : null; }
        public Supplier Create(Supplier supplier) { return supplier; }
        public Supplier Update(Supplier supplier) { return supplier; }
    }

    public class StubSupplierPurchaseRepository : IPurchaseRepository
    {
        public List<PurchaseInvoice> Posted = new List<PurchaseInvoice>();

        public PurchaseInvoicePageResult GetPaged(string status, string query, int page, int pageSize)
        {
            return new PurchaseInvoicePageResult { Items = new List<PurchaseInvoice>(), Total = 0 };
        }

        public PurchaseInvoice GetById(string id) { return Posted.FirstOrDefault(i => i.Id == id); }
        public PurchaseInvoice Save(PurchaseInvoice invoice, List<PurchaseInvoiceItem> items) { return invoice; }
        public PurchaseInvoice Cancel(string id) { return GetById(id); }

        public List<PurchaseInvoice> ListPostedBySupplier(string supplierId)
        {
            return Posted.Where(i => i.SupplierId == supplierId && i.Status == "posted").ToList();
        }
    }

    public class StubSupplierReturnRepository : IPurchaseReturnRepository
    {
        public PurchaseReturn Create(PurchaseReturn purchaseReturn, List<PurchaseReturnDetail> details) { return purchaseReturn; }

        public PurchaseReturnPageResult GetPaged(int page, int pageSize, string purchaseInvoiceId)
        {
            return new PurchaseReturnPageResult { Items = new List<PurchaseReturn>(), Total = 0 };
        }

        public Dictionary<string, double> SumReturnedByPurchase(string purchaseInvoiceId)
        {
            return new Dictionary<string, double>();
        }

        public List<PurchaseReturn> ListPostedBySupplier(string supplierId) { return new List<PurchaseReturn>(); }
        public bool HasPostedReturns(string purchaseInvoiceId) { return false; }
    }

    public class SupplierServiceTests
    {
        private static SupplierService ServiceWith(RecordingSupplierRepository suppliers,
            FakePaymentRepository payments = null, StubSupplierPurchaseRepository purchases = null)
        {
            return new SupplierService(suppliers, payments ?? new FakePaymentRepository(),
                purchases ?? new StubSupplierPurchaseRepository(), new StubSupplierReturnRepository());
        }

        [Fact]
        public void GetPaged_InvalidBalanceFilter_Throws()
        {
            var service = ServiceWith(new RecordingSupplierRepository());

            var ex = Assert.Throws<DomainValidationException>(() =>
                service.GetPaged(1, 20, null, "bogus"));
            Assert.Contains("balance filter", ex.Message);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("", null)]
        [InlineData("all", null)]
        [InlineData("positive", "positive")]
        [InlineData(" Negative ", "negative")]
        [InlineData("ZERO", "zero")]
        public void GetPaged_NormalizesBalanceFilter(string input, string expected)
        {
            var suppliers = new RecordingSupplierRepository();
            var service = ServiceWith(suppliers);

            service.GetPaged(1, 20, null, input);

            Assert.Equal(expected, suppliers.LastBalanceFilter);
        }

        [Fact]
        public void GetPurchases_UnknownSupplier_ThrowsNotFound()
        {
            var service = ServiceWith(new RecordingSupplierRepository());

            Assert.Throws<NotFoundException>(() => service.GetPurchases("missing"));
        }

        [Fact]
        public void GetPurchases_ExcludesCashPurchasesAndMapsPaid()
        {
            var suppliers = new RecordingSupplierRepository
            {
                Existing = new Supplier { Id = "s1", Name = "Distributor" }
            };
            var purchases = new StubSupplierPurchaseRepository
            {
                Posted = new List<PurchaseInvoice>
                {
                    new PurchaseInvoice { Id = "pur-1", SupplierId = "s1", Status = "posted", PaymentMethod = "credit", Total = 500 },
                    new PurchaseInvoice { Id = "pur-2", SupplierId = "s1", Status = "posted", PaymentMethod = "cash", Total = 100 },
                    new PurchaseInvoice { Id = "pur-9", SupplierId = "other", Status = "posted", PaymentMethod = "credit", Total = 999 }
                }
            };
            var payments = new FakePaymentRepository();
            payments.Stored.Add(new Payment { Id = "p1", InvoiceId = "pur-1", SupplierId = "s1", Amount = 200 });
            var service = ServiceWith(suppliers, payments, purchases);

            var result = service.GetPurchases("s1");

            Assert.Single(result.Items);
            Assert.Equal("pur-1", result.Items[0].Id);
            Assert.Equal(200, result.PaidByInvoice["pur-1"]);
        }

        [Fact]
        public void GetPurchases_AllocatesGeneralPaymentsOldestFirst()
        {
            var suppliers = new RecordingSupplierRepository
            {
                Existing = new Supplier { Id = "s1", Name = "Distributor" }
            };
            var purchases = new StubSupplierPurchaseRepository
            {
                Posted = new List<PurchaseInvoice>
                {
                    new PurchaseInvoice { Id = "pur-1", SupplierId = "s1", Status = "posted", PaymentMethod = "credit", Total = 1000 },
                    new PurchaseInvoice { Id = "pur-2", SupplierId = "s1", Status = "posted", PaymentMethod = "credit", Total = 1000 }
                }
            };
            var payments = new FakePaymentRepository();
            payments.Stored.Add(new Payment { Id = "p1", InvoiceId = null, SupplierId = "s1", Amount = 1500 });
            var service = ServiceWith(suppliers, payments, purchases);

            var result = service.GetPurchases("s1");

            Assert.Equal(1000, result.PaidByInvoice["pur-1"]);
            Assert.Equal(500, result.PaidByInvoice["pur-2"]);
        }
    }
}
