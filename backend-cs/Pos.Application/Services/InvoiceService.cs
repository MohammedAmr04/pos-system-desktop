using System;
using System.Collections.Generic;
using System.Linq;
using PosCs.Application.Models;
using PosCs.Application.Ports;
using PosCs.Domain.Entities;
using PosCs.Domain.Enums;
using PosCs.Domain.Exceptions;
using PosCs.Domain.Rules;

namespace PosCs.Application.Services
{
    /// <summary>Sales use cases: checkout with pricing rules, discounts, stock decrement and queries.</summary>
    public class InvoiceService
    {
        private readonly IInvoiceRepository _invoices;
        private readonly IProductRepository _products;
        private readonly IProductUnitRepository _units;
        private readonly IAccessControl _access;
        private readonly IClock _clock;

        public InvoiceService(IInvoiceRepository invoices, IProductRepository products,
            IProductUnitRepository units, IAccessControl access, IClock clock)
        {
            _invoices = invoices;
            _products = products;
            _units = units;
            _access = access;
            _clock = clock;
        }

        public Invoice Create(CreateInvoiceRequest dto, string userId)
        {
            if (dto?.Items == null || dto.Items.Count == 0)
                throw new DomainValidationException("No items provided");

            var priceMode = PriceModes.Normalize(dto.PriceMode);
            var tenantId = _access.GetTenantIdForUser(userId);

            if (priceMode == PriceModes.Wholesale && !_access.HasFeature(tenantId, "wholesale_price"))
                throw new FeatureDisabledException("wholesale_price");

            var lineDetails = new List<InvoiceDetail>();
            var lineFinals = new List<double>();

            foreach (var item in dto.Items)
            {
                if (item.Quantity <= 0)
                    throw new DomainValidationException($"Quantity must be greater than zero for '{item.Name}'");

                var product = _products.GetById(item.ProductId);
                if (product == null)
                    throw new DomainValidationException($"Product not found: '{item.Name}'");

                var unit = _units.GetById(item.ProductUnitId);
                if (unit == null || unit.ProductId != product.Id)
                    unit = _units.GetBaseUnit(product.Id);
                if (unit == null)
                    throw new DomainValidationException($"No selling unit found for product '{product.Name}'");

                var originalPrice = PriceSelection.SelectSellingPrice(priceMode, unit.RetailPrice, unit.WholesalePrice);
                var unitPrice = PriceSelection.ResolveSubmittedPrice(item.UnitPrice, originalPrice);
                if (unitPrice < 0)
                    throw new DomainValidationException($"Unit price cannot be negative for '{product.Name}'");

                if (PriceSelection.IsPriceOverride(item.UnitPrice, originalPrice))
                {
                    if (!_access.HasPermission(userId, "price.override"))
                        throw new PermissionDeniedException("price.override");
                    if (!_access.HasFeature(tenantId, "price_override"))
                        throw new FeatureDisabledException("price_override");
                }

                string lineDiscountType = null;
                double lineDiscountValue = 0;
                double lineDiscountAmount = 0;

                if (!string.IsNullOrEmpty(item.DiscountType) && item.DiscountValue > 0)
                {
                    if (!_access.HasPermission(userId, "discounts.product"))
                        throw new PermissionDeniedException("discounts.product");
                    if (!_access.HasFeature(tenantId, "product_discount"))
                        throw new FeatureDisabledException("product_discount");

                    lineDiscountAmount = InvoicePricing.LineDiscountAmount(item.DiscountType, item.DiscountValue,
                        unitPrice * item.Quantity, product.Name);
                    lineDiscountType = item.DiscountType;
                    lineDiscountValue = item.DiscountValue;
                }

                var lineSubtotal = InvoicePricing.Round2(unitPrice * item.Quantity);
                var lineFinal = InvoicePricing.Round2(lineSubtotal - lineDiscountAmount);

                string priceEditNote = null;
                if (unitPrice != originalPrice && !string.IsNullOrWhiteSpace(item.PriceEditNote))
                    priceEditNote = item.PriceEditNote.Trim();

                lineDetails.Add(new InvoiceDetail
                {
                    ProductId = product.Id,
                    ProductUnitId = unit.Id,
                    UnitName = unit.UnitName,
                    Quantity = item.Quantity,
                    BuyPrice = product.BuyPrice,
                    OriginalUnitPrice = Math.Round(originalPrice, 2),
                    UnitPrice = Math.Round(unitPrice, 2),
                    DiscountType = lineDiscountType,
                    DiscountValue = lineDiscountValue,
                    DiscountAmount = lineDiscountAmount,
                    LineSubtotal = lineSubtotal,
                    FinalTotal = lineFinal,
                    QuantityFactor = unit.QuantityFactor,
                    PriceEditNote = priceEditNote,
                    Product = product
                });
                lineFinals.Add(item.AllowDiscount ? lineFinal : 0);
            }

            var eligibleTotal = lineFinals.Sum();

            var hasInvoiceDiscount = (!string.IsNullOrEmpty(dto.DiscountType) && dto.DiscountValue > 0) || dto.Discount > 0;
            if (hasInvoiceDiscount)
            {
                if (!_access.HasPermission(userId, "discounts.invoice"))
                    throw new PermissionDeniedException("discounts.invoice");
                if (!_access.HasFeature(tenantId, "invoice_discount"))
                    throw new FeatureDisabledException("invoice_discount");
            }

            double invoiceDiscountAmount;
            string discountType;
            double discountValue;
            if (!string.IsNullOrEmpty(dto.DiscountType) && dto.DiscountValue > 0)
            {
                discountType = dto.DiscountType;
                discountValue = dto.DiscountValue;
                invoiceDiscountAmount = InvoicePricing.InvoiceDiscountAmount(discountType, discountValue, 0, eligibleTotal);
            }
            else
            {
                discountType = null;
                discountValue = 0;
                invoiceDiscountAmount = InvoicePricing.InvoiceDiscountAmount(null, 0, dto.Discount, eligibleTotal);
            }

            var shares = InvoicePricing.DistributeInvoiceDiscount(invoiceDiscountAmount, lineFinals,
                dto.Items.Select(i => i.AllowDiscount).ToList());

            for (int i = 0; i < lineDetails.Count; i++)
            {
                InvoicePricing.EnsureProfitProtection(lineDetails[i].BuyPrice, lineDetails[i].Quantity,
                    lineDetails[i].LineSubtotal, lineDetails[i].DiscountAmount + shares[i],
                    lineDetails[i].UnitPrice, lineDetails[i].Product.Name);
            }

            var totalAmount = InvoicePricing.FinalizeLines(lineDetails, shares);

            return _invoices.Create(new Invoice
            {
                TotalAmount = totalAmount,
                Discount = invoiceDiscountAmount,
                DiscountType = discountType,
                DiscountValue = discountValue,
                DiscountAmount = invoiceDiscountAmount,
                PriceMode = priceMode
            }, lineDetails);
        }

        public List<Invoice> GetToday()
        {
            var today = _clock.Today;
            return _invoices.GetRange(today, today.AddDays(1).AddSeconds(-1));
        }

        public List<Invoice> GetFiltered(string from, string to)
        {
            ResolveRange(from, to, out var fromDate, out var toDate);
            return _invoices.GetRange(fromDate, toDate);
        }

        public InvoicePageResult GetPaged(int page, int pageSize, string from, string to, string q)
        {
            page = Math.Max(1, page);
            pageSize = Math.Max(1, Math.Min(pageSize, 100));
            ResolveRange(from, to, out var fromDate, out var toDate);
            return _invoices.GetPaged(fromDate, toDate, q, page, pageSize);
        }

        public Invoice GetById(string id)
        {
            var invoice = _invoices.GetById(id);
            if (invoice == null)
                throw new NotFoundException("Invoice not found");
            return invoice;
        }

        private void ResolveRange(string from, string to, out DateTime? fromDate, out DateTime? toDate)
        {
            fromDate = null;
            toDate = null;

            if (!string.IsNullOrEmpty(from))
                fromDate = DateTime.Parse(from);

            if (!string.IsNullOrEmpty(to))
                toDate = DateTime.Parse(to).Date.AddDays(1).AddSeconds(-1);

            if (!fromDate.HasValue && !toDate.HasValue)
            {
                fromDate = _clock.Today;
                toDate = _clock.Today.AddDays(1).AddSeconds(-1);
            }
        }
    }
}
