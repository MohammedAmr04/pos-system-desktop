using System;
using System.Collections.Generic;
using System.Linq;
using PosCs.Application.Models;
using PosCs.Application.Ports;
using PosCs.Application.Services;
using PosCs.Domain.Entities;
using Xunit;

namespace PosCs.Tests.Application
{
    public class StubPagedInvoiceRepository : IInvoiceRepository
    {
        public List<Invoice> Page = new List<Invoice>();
        public List<Invoice> Posted = new List<Invoice>();

        public Invoice GetById(string id) { return Page.Concat(Posted).FirstOrDefault(i => i.Id == id); }
        public List<Invoice> GetRange(DateTime? from, DateTime? to) { return new List<Invoice>(); }

        public InvoicePageResult GetPaged(DateTime? from, DateTime? to, string query, string status, int page, int pageSize, string employeeId = null)
        {
            return new InvoicePageResult { Items = Page.ToList(), Total = Page.Count };
        }

        public Invoice Create(Invoice invoice, List<InvoiceDetail> items) { return invoice; }
        public Invoice Update(string id, Invoice invoice, List<InvoiceDetail> lines) { return invoice; }
        public Invoice Post(string id) { return GetById(id); }
        public Invoice Cancel(string id) { return GetById(id); }

        public List<Invoice> ListPostedByClient(string clientId)
        {
            return Posted.Where(i => i.ClientId == clientId && i.Status == "posted").ToList();
        }
    }

    public class StubProductRepository : IProductRepository
    {
        public Product GetById(string id) { return null; }
        public List<Product> GetAll() { return new List<Product>(); }
        public List<Product> GetForPOS() { return new List<Product>(); }
        public List<Product> Search(string query, int limit) { return new List<Product>(); }
        public PagedResult<Product> GetPaged(int page, int pageSize, string query)
        {
            return new PagedResult<Product> { Items = new List<Product>(), Total = 0 };
        }
        public int Count() { return 0; }
        public Product CreateWithBaseUnit(Product product, ProductUnit baseUnit, string barcode) { return product; }
        public void UpdateWithBaseUnit(Product product, ProductUnit baseUnit, string newDefaultBarcode) { }
        public bool Delete(string id) { return false; }
    }

    public class StubProductUnitRepository : IProductUnitRepository
    {
        public ProductUnit GetById(string unitId) { return null; }
        public List<ProductUnit> GetByProduct(string productId) { return new List<ProductUnit>(); }
        public List<ProductBarcode> GetBarcodesByUnit(string unitId) { return new List<ProductBarcode>(); }
        public ProductUnit GetBaseUnit(string productId) { return null; }
        public ProductBarcode GetBarcodeById(string barcodeId) { return null; }
        public bool BarcodeExists(string barcode) { return false; }
        public string GenerateUniqueBarcode() { return "000"; }
        public ProductUnit Create(ProductUnit unit) { return unit; }
        public void Update(ProductUnit unit) { }
        public bool Delete(string unitId) { return false; }
        public ProductBarcode AddBarcode(string unitId, string barcode, bool isDefault = false) { return null; }
        public bool DeleteBarcode(string barcodeId) { return false; }
        public bool SetDefaultBarcode(string unitId, string barcodeId) { return false; }
        public void SyncUnitName(string unitId, string newName) { }
    }

    public class StubAccessControl : IAccessControl
    {
        public bool HasPermission(string userId, string permissionKey) { return true; }
        public bool HasFeature(string tenantId, string featureKey) { return true; }
        public string GetTenantIdForUser(string userId) { return "t1"; }
    }

    public class StubClock : IClock
    {
        public DateTime UtcNow { get { return new DateTime(2026, 9, 5); } }
        public DateTime Today { get { return new DateTime(2026, 9, 5); } }
    }

    public class InvoiceServiceTests
    {
        private static InvoiceService ServiceWith(StubPagedInvoiceRepository invoices, FakePaymentRepository payments)
        {
            return new InvoiceService(invoices, new StubProductRepository(), new StubProductUnitRepository(),
                new StubAccessControl(), new StubClock(), new RecordingClientRepository(),
                new FakeEmployeeRepository(), payments);
        }

        [Fact]
        public void GetPaged_AllocatesGeneralPoolOldestFirst()
        {
            var invoices = new StubPagedInvoiceRepository
            {
                Page = new List<Invoice>
                {
                    new Invoice { Id = "i1", ClientId = "c1", Status = "posted", InvoiceNumber = 1, TotalAmount = 1000 },
                    new Invoice { Id = "i2", ClientId = "c1", Status = "posted", InvoiceNumber = 2, TotalAmount = 1000 }
                },
                Posted = new List<Invoice>
                {
                    new Invoice { Id = "i1", ClientId = "c1", Status = "posted", InvoiceNumber = 1, TotalAmount = 1000 },
                    new Invoice { Id = "i2", ClientId = "c1", Status = "posted", InvoiceNumber = 2, TotalAmount = 1000 }
                }
            };
            var payments = new FakePaymentRepository();
            payments.Stored.Add(new Payment { Id = "p1", InvoiceId = null, ClientId = "c1", Amount = 1500 });

            var result = ServiceWith(invoices, payments).GetPaged(1, 20, null, null, null);

            Assert.Equal(1000, result.PaidByInvoice["i1"]);
            Assert.Equal(500, result.PaidByInvoice["i2"]);
        }

        [Fact]
        public void GetPaged_MatchesInvoice4Scenario_LinkedPlusGeneral()
        {
            var invoices = new StubPagedInvoiceRepository
            {
                Page = new List<Invoice>
                {
                    new Invoice { Id = "i4", ClientId = "c1", Status = "posted", InvoiceNumber = 4, TotalAmount = 34 }
                },
                Posted = new List<Invoice>
                {
                    new Invoice { Id = "i4", ClientId = "c1", Status = "posted", InvoiceNumber = 4, TotalAmount = 34 }
                }
            };
            var payments = new FakePaymentRepository();
            payments.Stored.Add(new Payment { Id = "p1", InvoiceId = "i4", ClientId = "c1", Amount = 15 });
            payments.Stored.Add(new Payment { Id = "p2", InvoiceId = null, ClientId = "c1", Amount = 19 });

            var result = ServiceWith(invoices, payments).GetPaged(1, 20, null, null, null);

            Assert.Equal(34, result.PaidByInvoice["i4"]);
        }

        [Fact]
        public void GetPaged_DraftAndWalkIn_KeepLinkedOnly()
        {
            var invoices = new StubPagedInvoiceRepository
            {
                Page = new List<Invoice>
                {
                    new Invoice { Id = "d1", ClientId = "c1", Status = "draft", InvoiceNumber = 5, TotalAmount = 500 },
                    new Invoice { Id = "w1", ClientId = null, Status = "posted", InvoiceNumber = 6, TotalAmount = 200 }
                },
                Posted = new List<Invoice>()
            };
            var payments = new FakePaymentRepository();
            payments.Stored.Add(new Payment { Id = "p1", InvoiceId = null, ClientId = "c1", Amount = 800 });
            payments.Stored.Add(new Payment { Id = "p2", InvoiceId = "w1", ClientId = null, Amount = 50 });

            var result = ServiceWith(invoices, payments).GetPaged(1, 20, null, null, null);

            Assert.Equal(0, result.PaidByInvoice["d1"]);
            Assert.Equal(50, result.PaidByInvoice["w1"]);
        }
    }
}
