using System;
using System.Collections.Generic;
using PosCs.Domain.Entities;
using PosCs.Domain.Enums;
using PosCs.Domain.Exceptions;
using PosCs.Domain.Rules;
using Xunit;

namespace PosCs.Tests.Domain
{
    public class InvoicePricingTests
    {
        [Theory]
        [InlineData(1.005, 1.0)]
        [InlineData(10.129, 10.13)]
        [InlineData(10.124, 10.12)]
        public void Round2_RoundsToTwoDecimals(double value, double expected)
        {
            Assert.Equal(expected, InvoicePricing.Round2(value));
        }

        [Fact]
        public void LineSubtotal_MultipliesAndRounds()
        {
            Assert.Equal(30.0, InvoicePricing.LineSubtotal(10.0, 3.0));
            Assert.Equal(3.33, InvoicePricing.LineSubtotal(1.111, 3.0));
        }

        [Fact]
        public void LineDiscount_Percentage_ComputesRoundedShare()
        {
            var amount = InvoicePricing.LineDiscountAmount("percentage", 10, 33.35, "Tea");
            Assert.Equal(3.34, amount);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(101)]
        public void LineDiscount_Percentage_OutOfRange_Throws(double value)
        {
            Assert.Throws<DomainValidationException>(() =>
                InvoicePricing.LineDiscountAmount("percentage", value, 100, "Tea"));
        }

        [Fact]
        public void LineDiscount_Fixed_ReturnsRoundedValue()
        {
            Assert.Equal(5.5, InvoicePricing.LineDiscountAmount("fixed", 5.5, 20, "Tea"));
        }

        [Fact]
        public void LineDiscount_Fixed_ExceedingSubtotal_Throws()
        {
            var ex = Assert.Throws<DomainValidationException>(() =>
                InvoicePricing.LineDiscountAmount("fixed", 25, 20, "Tea"));
            Assert.Contains("Tea", ex.Message);
        }

        [Fact]
        public void LineDiscount_UnknownType_Throws()
        {
            Assert.Throws<DomainValidationException>(() =>
                InvoicePricing.LineDiscountAmount("bogus", 5, 20, "Tea"));
        }

        [Fact]
        public void EligibleTotal_SkipsNonEligibleLines()
        {
            var totals = new[] { 10.0, 20.0, 40.0 };
            var allow = new[] { true, false, true };
            Assert.Equal(50.0, InvoicePricing.EligibleTotal(totals, allow));
        }

        [Fact]
        public void InvoiceDiscount_Percentage_AppliesToEligibleTotal()
        {
            Assert.Equal(15.0, InvoicePricing.InvoiceDiscountAmount("percentage", 10, 0, 150));
        }

        [Fact]
        public void InvoiceDiscount_Fixed_ExceedingEligible_Throws()
        {
            Assert.Throws<DomainValidationException>(() =>
                InvoicePricing.InvoiceDiscountAmount("fixed", 200, 0, 150));
        }

        [Fact]
        public void InvoiceDiscount_FlatPath_ClampsNegativeToZero()
        {
            Assert.Equal(7.0, InvoicePricing.InvoiceDiscountAmount(null, 0, 7.0, 150));
            Assert.Equal(0.0, InvoicePricing.InvoiceDiscountAmount(null, 0, -3.0, 150));
        }

        [Fact]
        public void DistributeInvoiceDiscount_ProportionalWithDriftCorrection()
        {
            // 100 discount over two equal lines of 150 each: 50 + 50, no drift.
            var shares = InvoicePricing.DistributeInvoiceDiscount(
                100, new[] { 150.0, 150.0 }, new[] { true, true });
            Assert.Equal(new[] { 50.0, 50.0 }, shares);

            // Drift case: 10 over three lines of 33.33/33.33/33.34.
            var drift = InvoicePricing.DistributeInvoiceDiscount(
                10, new[] { 33.33, 33.33, 33.34 }, new[] { true, true, true });
            Assert.Equal(10, Math.Round(drift[0] + drift[1] + drift[2], 2));
        }

        [Fact]
        public void DistributeInvoiceDiscount_RespectsNonEligibleLines()
        {
            var shares = InvoicePricing.DistributeInvoiceDiscount(
                10, new[] { 100.0, 100.0 }, new[] { false, true });
            Assert.Equal(0, shares[0]);
            Assert.Equal(10, shares[1]);
        }

        [Fact]
        public void EnsureProfitProtection_RejectsBelowCostPrice()
        {
            // Subtotal 9, discount 2 => effective (9-2)/2 = 3.50 < buy price 4.
            Assert.Throws<DomainValidationException>(() =>
                InvoicePricing.EnsureProfitProtection(4, 2, 9, 2, 4.5, "Tea"));
        }

        [Fact]
        public void EnsureProfitProtection_AllowsAtOrAboveCost()
        {
            InvoicePricing.EnsureProfitProtection(3.5, 2, 9, 2, 4.5, "Tea");
        }

        [Fact]
        public void FinalizeLines_MergesSharesAndReturnsTotal()
        {
            var lines = new List<InvoiceDetail>
            {
                new InvoiceDetail { LineSubtotal = 100, DiscountAmount = 5 },
                new InvoiceDetail { LineSubtotal = 50, DiscountAmount = 0 }
            };
            var total = InvoicePricing.FinalizeLines(lines, new[] { 5.0, 5.0 });
            Assert.Equal(10, lines[0].DiscountAmount);
            Assert.Equal(90, lines[0].FinalTotal);
            Assert.Equal(45, lines[1].FinalTotal);
            Assert.Equal(135, total);
        }
    }

    public class PriceSelectionTests
    {
        [Fact]
        public void SelectSellingPrice_RetailMode_IgnoresWholesale()
        {
            Assert.Equal(10, PriceSelection.SelectSellingPrice("retail", 10, 8));
        }

        [Fact]
        public void SelectSellingPrice_WholesaleMode_RequiresPositiveWholesale()
        {
            Assert.Equal(8, PriceSelection.SelectSellingPrice("wholesale", 10, 8));
            Assert.Equal(10, PriceSelection.SelectSellingPrice("wholesale", 10, null));
            Assert.Equal(10, PriceSelection.SelectSellingPrice("wholesale", 10, 0));
        }

        [Fact]
        public void ResolveSubmittedPrice_SubmittedWinsOnlyWhenPositive()
        {
            Assert.Equal(7, PriceSelection.ResolveSubmittedPrice(7, 10));
            Assert.Equal(10, PriceSelection.ResolveSubmittedPrice(0, 10));
        }

        [Theory]
        [InlineData(10, 10, false)]
        [InlineData(10.004, 10, false)]
        [InlineData(10.01, 10, true)]
        [InlineData(0, 10, false)]
        public void IsPriceOverride_DetectsMeaningfulDifferences(double submitted, double original, bool expected)
        {
            Assert.Equal(expected, PriceSelection.IsPriceOverride(submitted, original));
        }
    }

    public class UnitRulesTests
    {
        [Fact]
        public void ValidateNewUnit_RejectsInvalidInput()
        {
            Assert.Throws<DomainValidationException>(() => UnitRules.ValidateNewUnit(" ", 2, 5, null));
            Assert.Throws<DomainValidationException>(() => UnitRules.ValidateNewUnit("Box", 1, 5, null));
            Assert.Throws<DomainValidationException>(() => UnitRules.ValidateNewUnit("Box", 2, 0, null));
            Assert.Throws<DomainValidationException>(() => UnitRules.ValidateNewUnit("Box", 2, 5, -1));
        }

        [Fact]
        public void ValidateNewUnit_AcceptsValidInput()
        {
            UnitRules.ValidateNewUnit("Box", 12, 5, 4.5);
        }

        [Fact]
        public void ValidateUnitUpdate_BaseUnitMustKeepFactorOne()
        {
            var baseUnit = new ProductUnit { IsBaseUnit = true };
            Assert.Throws<DomainValidationException>(() =>
                UnitRules.ValidateUnitUpdate(baseUnit, null, 6, null, null));
        }

        [Fact]
        public void ValidateUnitUpdate_NonBaseUnitRequiresFactorAboveOne()
        {
            var unit = new ProductUnit { IsBaseUnit = false };
            Assert.Throws<DomainValidationException>(() =>
                UnitRules.ValidateUnitUpdate(unit, null, 1, null, null));
        }

        [Fact]
        public void EnsureNotBaseUnit_BlocksBaseUnitDeletion()
        {
            Assert.Throws<DomainValidationException>(() =>
                UnitRules.EnsureNotBaseUnit(new ProductUnit { IsBaseUnit = true }));
            UnitRules.EnsureNotBaseUnit(new ProductUnit { IsBaseUnit = false });
        }

        [Fact]
        public void EnsureNotDefaultBarcode_BlocksDefaultDeletion()
        {
            Assert.Throws<DomainValidationException>(() =>
                UnitRules.EnsureNotDefaultBarcode(new ProductBarcode { IsDefault = true }));
            UnitRules.EnsureNotDefaultBarcode(new ProductBarcode { IsDefault = false });
        }
    }

    public class LicenseCodeTests
    {
        [Fact]
        public void ComputeUnlockCode_IsDeterministicFourDigits()
        {
            var code = LicenseCode.ComputeUnlockCode("machine-123");
            Assert.Equal(4, code.Length);
            Assert.Equal(code, LicenseCode.ComputeUnlockCode("machine-123"));
        }

        [Fact]
        public void IsValidUnlockCode_AcceptsCorrectCode_IgnoringCaseAndWhitespace()
        {
            var code = LicenseCode.ComputeUnlockCode("machine-123");
            Assert.True(LicenseCode.IsValidUnlockCode("machine-123", $" {code.ToUpper()} "));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("0000")]
        [InlineData("12345")]
        public void IsValidUnlockCode_RejectsBadInput(string code)
        {
            Assert.False(LicenseCode.IsValidUnlockCode("machine-123", code));
        }
    }

    public class LowStockPolicyTests
    {
        [Fact]
        public void ProductsWithoutThresholdAreNeverLow()
        {
            Assert.False(LowStockPolicy.IsLowStock(new Product { LowStockThreshold = 0, StockQuantity = 0 }));
        }

        [Theory]
        [InlineData(5, 5, true)]
        [InlineData(5, 4, true)]
        [InlineData(5, 6, false)]
        public void ThresholdComparisonIsInclusive(double threshold, double stock, bool expected)
        {
            Assert.Equal(expected, LowStockPolicy.IsLowStock(
                new Product { LowStockThreshold = (int)threshold, StockQuantity = stock }));
        }
    }
}
