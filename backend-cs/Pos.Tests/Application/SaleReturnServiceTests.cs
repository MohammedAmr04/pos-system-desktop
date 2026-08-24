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
    public class FakeSaleReturnRepository : ISaleReturnRepository
    {
        public List<SaleReturn> Stored = new List<SaleReturn>();
        public Dictionary<string, double> ReturnedBefore = new Dictionary<string, double>();
        public int NextNumber = 1;

        public SaleReturn Create(SaleReturn saleReturn, List<SaleReturnDetail> details)
        {
            saleReturn.Id = Guid.NewGuid().ToString("N");
            saleReturn.Number = NextNumber++;
            saleReturn.Details = details;
            Stored.Add(saleReturn);
            return saleReturn;
        }

        public SaleReturnPageResult GetPaged(int page, int pageSize, string invoiceId) =>
            new SaleReturnPageResult { Items = Stored, Total = Stored.Count };

        public List<SaleReturnDetail> GetDetails(string returnId) =>
            Stored.FirstOrDefault(r => r.Id == returnId)?.Details ?? new List<SaleReturnDetail>();

        public Dictionary<string, double> SumReturnedByInvoice(string invoiceId) =>
            new Dictionary<string, double>(ReturnedBefore);
    }

    public class SaleReturnServiceTests
    {
        private static Invoice PostedInvoice(string detailId = "line-1", double qty = 3)
        {
            return new Invoice
            {
                Id = "inv-1",
                Status = "posted",
                PaymentMethod = "cash",
                InvoiceDetail = new List<InvoiceDetail>
                {
                    new InvoiceDetail { Id = detailId, ProductId = "p-1", UnitName = "قطعة", Quantity = qty, QuantityFactor = 1 }
                }
            };
        }

        private static CreateSaleReturnRequest Request(string detailId = "line-1", double qty = 1) =>
            new CreateSaleReturnRequest
            {
                Items = new List<SaleReturnItemRequest>
                {
                    new SaleReturnItemRequest { InvoiceDetailId = detailId, Quantity = qty }
                }
            };

        private static SaleReturnService ServiceWith(FakeInvoiceRepository invoices, FakeSaleReturnRepository returns) =>
            new SaleReturnService(returns, invoices);

        [Fact]
        public void Requires_At_Least_One_Item()
        {
            var ex = Assert.Throws<DomainValidationException>(() =>
                ServiceWith(new FakeInvoiceRepository(), new FakeSaleReturnRepository())
                    .Create("inv-1", new CreateSaleReturnRequest { Items = new List<SaleReturnItemRequest>() }, "u"));
            Assert.Contains("No return items", ex.Message);
        }

        [Fact]
        public void Missing_Invoice_Throws()
        {
            var ex = Assert.Throws<NotFoundException>(() =>
                ServiceWith(new FakeInvoiceRepository(), new FakeSaleReturnRepository())
                    .Create("missing", Request(), "u"));
        }

        [Theory]
        [InlineData("draft")]
        [InlineData("cancelled")]
        public void Only_Posted_Invoices_Can_Be_Returned(string status)
        {
            var invoice = PostedInvoice();
            invoice.Status = status;
            var invoices = new FakeInvoiceRepository { Existing = invoice };

            Assert.Throws<DomainValidationException>(() =>
                ServiceWith(invoices, new FakeSaleReturnRepository()).Create("inv-1", Request(), "u"));
        }

        [Fact]
        public void Quantity_Cannot_Exceed_Sold_Minus_Returned()
        {
            var invoices = new FakeInvoiceRepository { Existing = PostedInvoice(qty: 3) };
            var returns = new FakeSaleReturnRepository();
            returns.ReturnedBefore["line-1"] = 1;

            // 2 remaining: asking for 3 must fail, asking for 2 must pass.
            Assert.Throws<DomainValidationException>(() =>
                ServiceWith(invoices, returns).Create("inv-1", Request(qty: 3), "u"));

            var ok = ServiceWith(invoices, returns).Create("inv-1", Request(qty: 2), "u");
            Assert.Equal(2, ok.Details[0].Quantity);
        }

        [Fact]
        public void Zero_Or_Negative_Quantities_Are_Rejected()
        {
            var invoices = new FakeInvoiceRepository { Existing = PostedInvoice() };

            Assert.Throws<DomainValidationException>(() =>
                ServiceWith(invoices, new FakeSaleReturnRepository()).Create("inv-1", Request(qty: 0), "u"));
        }

        [Fact]
        public void Duplicate_Lines_In_One_Request_Are_Rejected()
        {
            var invoices = new FakeInvoiceRepository { Existing = PostedInvoice() };
            var dto = new CreateSaleReturnRequest
            {
                Items = new List<SaleReturnItemRequest>
                {
                    new SaleReturnItemRequest { InvoiceDetailId = "line-1", Quantity = 1 },
                    new SaleReturnItemRequest { InvoiceDetailId = "line-1", Quantity = 1 }
                }
            };

            Assert.Throws<DomainValidationException>(() =>
                ServiceWith(invoices, new FakeSaleReturnRepository()).Create("inv-1", dto, "u"));
        }

        [Fact]
        public void Line_From_Another_Invoice_Is_Rejected()
        {
            var invoices = new FakeInvoiceRepository { Existing = PostedInvoice(detailId: "other-line") };

            Assert.Throws<DomainValidationException>(() =>
                ServiceWith(invoices, new FakeSaleReturnRepository()).Create("inv-1", Request("line-1"), "u"));
        }

        [Fact]
        public void Create_Passes_Context_To_Repository()
        {
            var invoices = new FakeInvoiceRepository { Existing = PostedInvoice(qty: 5) };
            var returns = new FakeSaleReturnRepository();

            var result = ServiceWith(invoices, returns).Create(
                "inv-1", new CreateSaleReturnRequest
                {
                    Items = new List<SaleReturnItemRequest>
                    {
                        new SaleReturnItemRequest { InvoiceDetailId = "line-1", Quantity = 2 }
                    },
                    Notes = "damaged"
                }, "cashier-7");

            Assert.Equal("inv-1", result.InvoiceId);
            Assert.Equal("cashier-7", result.CreatedBy);
            Assert.Equal("damaged", result.Notes);
            Assert.Equal(2, result.Details.Single().Quantity);
        }

        [Fact]
        public void Legacy_Line_With_Missing_Factor_Defaults_To_One()
        {
            var invoice = PostedInvoice();
            invoice.InvoiceDetail[0].QuantityFactor = 0;
            var invoices = new FakeInvoiceRepository { Existing = invoice };
            var returns = new FakeSaleReturnRepository();

            var result = ServiceWith(invoices, returns).Create("inv-1", Request(qty: 1), "u");

            Assert.Equal(1, result.Details.Single().QuantityFactor);
        }
    }
}
