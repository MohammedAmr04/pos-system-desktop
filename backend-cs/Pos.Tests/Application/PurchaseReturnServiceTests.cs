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
    public class FakePurchaseReturnRepository : IPurchaseReturnRepository
    {
        public List<PurchaseReturn> Stored = new List<PurchaseReturn>();
        public Dictionary<string, double> ReturnedBefore = new Dictionary<string, double>();
        public int NextNumber = 1;

        public PurchaseReturn Create(PurchaseReturn purchaseReturn, List<PurchaseReturnDetail> details)
        {
            purchaseReturn.Id = Guid.NewGuid().ToString("N");
            purchaseReturn.Number = NextNumber++;
            purchaseReturn.Details = details;
            Stored.Add(purchaseReturn);
            return purchaseReturn;
        }

        public PurchaseReturnPageResult GetPaged(int page, int pageSize, string purchaseInvoiceId) =>
            new PurchaseReturnPageResult { Items = Stored, Total = Stored.Count };

        public Dictionary<string, double> SumReturnedByPurchase(string purchaseInvoiceId) =>
            new Dictionary<string, double>(ReturnedBefore);

        public List<PurchaseReturn> ListPostedBySupplier(string supplierId) =>
            Stored.Where(r => r.PurchaseInvoice?.SupplierId == supplierId).ToList();

        public bool HasPostedReturns(string purchaseInvoiceId) => Stored.Count > 0;
    }

    public class PurchaseReturnServiceTests
    {
        private static PurchaseInvoice PostedPurchase(string lineId = "pline-1", double qty = 10, double unitCost = 8)
        {
            return new PurchaseInvoice
            {
                Id = "pur-1",
                Status = "posted",
                PaymentMethod = "cash",
                Items = new List<PurchaseInvoiceItem>
                {
                    new PurchaseInvoiceItem { Id = lineId, ProductId = "p-1", UnitName = "قطعة", Quantity = qty, QuantityFactor = 1, UnitCost = unitCost }
                }
            };
        }

        private static CreatePurchaseReturnRequest Request(string lineId = "pline-1", double qty = 1) =>
            new CreatePurchaseReturnRequest
            {
                Items = new List<CreatePurchaseReturnItem>
                {
                    new CreatePurchaseReturnItem { PurchaseItemId = lineId, Quantity = qty }
                }
            };

        private static PurchaseReturnService ServiceWith(FakePurchaseRepository purchases, FakePurchaseReturnRepository returns) =>
            new PurchaseReturnService(returns, purchases);

        [Fact]
        public void Requires_At_Least_One_Item()
        {
            var ex = Assert.Throws<DomainValidationException>(() =>
                ServiceWith(new FakePurchaseRepository(), new FakePurchaseReturnRepository())
                    .Create("pur-1", new CreatePurchaseReturnRequest { Items = new List<CreatePurchaseReturnItem>() }, "u"));
            Assert.Contains("No return items", ex.Message);
        }

        [Fact]
        public void Missing_Purchase_Throws()
        {
            Assert.Throws<NotFoundException>(() =>
                ServiceWith(new FakePurchaseRepository(), new FakePurchaseReturnRepository())
                    .Create("missing", Request(), "u"));
        }

        [Theory]
        [InlineData("draft")]
        [InlineData("cancelled")]
        public void Only_Posted_Purchases_Can_Be_Returned(string status)
        {
            var invoice = PostedPurchase();
            invoice.Status = status;
            var purchases = new FakePurchaseRepository { Stored = invoice };

            Assert.Throws<DomainValidationException>(() =>
                ServiceWith(purchases, new FakePurchaseReturnRepository()).Create("pur-1", Request(), "u"));
        }

        [Fact]
        public void Quantity_Cannot_Exceed_Purchased_Minus_Returned()
        {
            var purchases = new FakePurchaseRepository { Stored = PostedPurchase(qty: 10) };
            var returns = new FakePurchaseReturnRepository();
            returns.ReturnedBefore["pline-1"] = 4;

            // 6 remaining: asking for 7 must fail, asking for 6 must pass.
            Assert.Throws<DomainValidationException>(() =>
                ServiceWith(purchases, returns).Create("pur-1", Request(qty: 7), "u"));

            var ok = ServiceWith(purchases, returns).Create("pur-1", Request(qty: 6), "u");
            Assert.Equal(6, ok.Details[0].Quantity);
        }

        [Fact]
        public void Zero_Or_Negative_Quantities_Are_Rejected()
        {
            var purchases = new FakePurchaseRepository { Stored = PostedPurchase() };

            Assert.Throws<DomainValidationException>(() =>
                ServiceWith(purchases, new FakePurchaseReturnRepository()).Create("pur-1", Request(qty: 0), "u"));
        }

        [Fact]
        public void Duplicate_Lines_In_One_Request_Are_Rejected()
        {
            var purchases = new FakePurchaseRepository { Stored = PostedPurchase() };
            var dto = new CreatePurchaseReturnRequest
            {
                Items = new List<CreatePurchaseReturnItem>
                {
                    new CreatePurchaseReturnItem { PurchaseItemId = "pline-1", Quantity = 1 },
                    new CreatePurchaseReturnItem { PurchaseItemId = "pline-1", Quantity = 1 }
                }
            };

            Assert.Throws<DomainValidationException>(() =>
                ServiceWith(purchases, new FakePurchaseReturnRepository()).Create("pur-1", dto, "u"));
        }

        [Fact]
        public void Line_From_Another_Purchase_Is_Rejected()
        {
            var purchases = new FakePurchaseRepository { Stored = PostedPurchase(lineId: "other-line") };

            Assert.Throws<DomainValidationException>(() =>
                ServiceWith(purchases, new FakePurchaseReturnRepository()).Create("pur-1", Request("pline-1"), "u"));
        }

        [Fact]
        public void Create_Passes_Context_To_Repository()
        {
            var purchases = new FakePurchaseRepository { Stored = PostedPurchase(qty: 5, unitCost: 8) };
            var returns = new FakePurchaseReturnRepository();

            var result = ServiceWith(purchases, returns).Create(
                "pur-1", new CreatePurchaseReturnRequest
                {
                    Items = new List<CreatePurchaseReturnItem>
                    {
                        new CreatePurchaseReturnItem { PurchaseItemId = "pline-1", Quantity = 2 }
                    },
                    Notes = "damaged cartons"
                }, "manager-3");

            Assert.Equal("pur-1", result.PurchaseInvoiceId);
            Assert.Equal("manager-3", result.CreatedBy);
            Assert.Equal("damaged cartons", result.Notes);
            var detail = result.Details.Single();
            Assert.Equal(2, detail.Quantity);
            Assert.Equal("p-1", detail.ProductId);
            Assert.Equal("pline-1", detail.PurchaseItemId);
        }

        [Fact]
        public void Legacy_Line_With_Missing_Factor_Defaults_To_One()
        {
            var invoice = PostedPurchase();
            invoice.Items[0].QuantityFactor = 0;
            var purchases = new FakePurchaseRepository { Stored = invoice };

            var result = ServiceWith(purchases, new FakePurchaseReturnRepository()).Create("pur-1", Request(qty: 1), "u");

            Assert.Equal(1, result.Details.Single().QuantityFactor);
        }
    }
}
