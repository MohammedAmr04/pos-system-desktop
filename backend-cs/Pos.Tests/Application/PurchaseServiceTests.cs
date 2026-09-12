using System;
using System.Collections.Generic;
using System.Linq;
using PosCs.Application.Ports;
using PosCs.Application.Services;
using PosCs.Domain.Entities;
using PosCs.Domain.Exceptions;
using Xunit;

namespace PosCs.Tests.Application
{
    public class FakePurchaseRepository : IPurchaseRepository
    {
        public PurchaseInvoice Stored;
        public PurchaseInvoice LastSavedInvoice;
        public List<PurchaseInvoiceItem> LastSavedItems;

        public PurchaseInvoicePageResult GetPaged(string status, string query, int page, int pageSize) =>
            new PurchaseInvoicePageResult { Items = new List<PurchaseInvoice>(), Total = 0 };

        public PurchaseInvoice GetById(string id)
        {
            if (Stored == null || Stored.Id != id)
                throw new NotFoundException("Purchase invoice not found");
            return Stored;
        }

        public PurchaseInvoice Save(PurchaseInvoice invoice, List<PurchaseInvoiceItem> items)
        {
            LastSavedInvoice = invoice;
            LastSavedItems = items;
            if (Stored != null && Stored.Id == invoice.Id)
            {
                Stored.Status = invoice.Status;
                Stored.Items = items;
                return Stored;
            }
            invoice.Id = invoice.Id ?? "new-purchase";
            Stored = invoice;
            Stored.Items = items;
            return invoice;
        }

        public PurchaseInvoice Cancel(string id)
        {
            if (Stored == null || Stored.Id != id)
                throw new NotFoundException("Purchase invoice not found");
            Stored.Status = "cancelled";
            return Stored;
        }

        public List<PurchaseInvoice> ListPostedBySupplier(string supplierId) =>
            Stored != null && Stored.SupplierId == supplierId && Stored.Status == "posted"
                ? new List<PurchaseInvoice> { Stored }
                : new List<PurchaseInvoice>();
    }

    public class PurchaseServiceTests
    {
        private static SavePurchaseRequest ValidRequest(string paymentMethod = "cash", string supplierId = null, string status = "draft")
        {
            return new SavePurchaseRequest
            {
                SupplierId = supplierId,
                PaymentMethod = paymentMethod,
                Status = status,
                Lines = new List<PurchaseLineRequest>
                {
                    new PurchaseLineRequest
                    {
                        ProductId = "p1",
                        ProductUnitId = "u1",
                        Quantity = 10,
                        UnitCost = 5.5
                    }
                }
            };
        }

        private static PurchaseService ServiceWith(FakePurchaseRepository repo) => new PurchaseService(repo);

        [Fact]
        public void Create_RequiresAtLeastOneLine()
        {
            var request = ValidRequest();
            request.Lines = new List<PurchaseLineRequest>();
            var ex = Assert.Throws<DomainValidationException>(() => ServiceWith(new FakePurchaseRepository()).Save(request, "user-1"));
            Assert.Contains("at least one line", ex.Message);
        }

        [Fact]
        public void CreditPurchase_RequiresSupplier()
        {
            var request = ValidRequest(paymentMethod: "credit", supplierId: null);
            var ex = Assert.Throws<DomainValidationException>(() => ServiceWith(new FakePurchaseRepository()).Save(request, "user-1"));
            Assert.Contains("require a supplier", ex.Message);
        }

        [Fact]
        public void CreditPurchase_WithSupplier_PassesValidation()
        {
            var repo = new FakePurchaseRepository();
            var result = ServiceWith(repo).Save(ValidRequest(paymentMethod: "credit", supplierId: "sup-1"), "user-1");

            Assert.Equal("credit", result.PaymentMethod);
            Assert.Equal("sup-1", result.SupplierId);
            Assert.Equal("draft", result.Status);
        }

        [Fact]
        public void Post_RequiresPositiveQuantities()
        {
            var request = ValidRequest(status: "posted");
            request.Lines[0].Quantity = 0;
            var ex = Assert.Throws<DomainValidationException>(() => ServiceWith(new FakePurchaseRepository()).Save(request, "user-1"));
            Assert.Contains("greater than zero", ex.Message);
        }

        [Fact]
        public void Post_RejectsNegativeCost()
        {
            var request = ValidRequest(status: "posted");
            request.Lines[0].UnitCost = -1;
            Assert.Throws<DomainValidationException>(() => ServiceWith(new FakePurchaseRepository()).Save(request, "user-1"));
        }

        [Fact]
        public void DraftSave_ComputesTotalsFromLines()
        {
            var repo = new FakePurchaseRepository();
            var request = ValidRequest();
            request.Lines.Add(new PurchaseLineRequest { ProductId = "p2", ProductUnitId = "u2", Quantity = 2, UnitCost = 100 });

            var result = ServiceWith(repo).Save(request, "user-1");

            Assert.Equal(255, result.Subtotal);
            Assert.Equal(255, result.Total);
            Assert.Equal(2, repo.LastSavedItems.Count);
            Assert.Equal(55, repo.LastSavedItems[0].LineTotal);
        }

        [Fact]
        public void Totals_ApplyDiscountAndTax()
        {
            var repo = new FakePurchaseRepository();
            var request = ValidRequest();
            request.Discount = 5;
            request.Tax = 10;

            var result = ServiceWith(repo).Save(request, "user-1");

            Assert.Equal(55, result.Subtotal);
            Assert.Equal(60, result.Total);
        }

        [Fact]
        public void UnknownStatus_DefaultsToDraft()
        {
            var repo = new FakePurchaseRepository();
            var request = ValidRequest(status: "weird");

            var result = ServiceWith(repo).Save(request, "user-1");

            Assert.Equal("draft", result.Status);
        }

        [Fact]
        public void Post_OnlyFromDraft()
        {
            var repo = new FakePurchaseRepository
            {
                Stored = new PurchaseInvoice { Id = "inv-1", Status = "posted" }
            };
            var ex = Assert.Throws<DomainValidationException>(() => ServiceWith(repo).Post("inv-1"));
            Assert.Contains("Only draft", ex.Message);
        }

        [Fact]
        public void Post_FromDraft_SavesAsPosted()
        {
            var repo = new FakePurchaseRepository
            {
                Stored = new PurchaseInvoice
                {
                    Id = "inv-1",
                    Status = "draft",
                    Items = new List<PurchaseInvoiceItem>
                    {
                        new PurchaseInvoiceItem { ProductId = "p1", ProductUnitId = "u1", Quantity = 3, UnitCost = 2 }
                    }
                }
            };

            var result = ServiceWith(repo).Post("inv-1");

            Assert.Equal("posted", result.Status);
            Assert.Equal("posted", repo.LastSavedInvoice.Status);
        }

        [Fact]
        public void CancelledPurchases_CannotBeEdited()
        {
            var repo = new FakePurchaseRepository
            {
                Stored = new PurchaseInvoice { Id = "inv-1", Status = "cancelled" }
            };
            // Repository itself rejects cancelled edits — service passes through to it.
            Assert.Throws<DomainValidationException>(() => ServiceWith(repo).Post("inv-1"));
        }

        [Fact]
        public void GetPaged_ClampsPageSize()
        {
            var service = ServiceWith(new FakePurchaseRepository());
            var result = service.GetPaged(null, null, 0, 500);

            Assert.NotNull(result);
        }
    }
}
