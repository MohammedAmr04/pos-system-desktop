using System;
using System.Collections.Generic;
using PosCs.Domain.Entities;
using PosCs.Domain.Enums;
using PosCs.Domain.Exceptions;

namespace PosCs.Domain.Rules
{
    /// <summary>
    /// Pure invoice pricing rules: line subtotals, line discounts, invoice-level
    /// discount distribution and profit protection. Extracted verbatim from the
    /// legacy InvoicesController.Create flow — do not change behavior here.
    /// </summary>
    public static class InvoicePricing
    {
        public static double Round2(double value)
        {
            return Math.Round(value, 2);
        }

        /// <summary>Rounded line subtotal (unit price x quantity).</summary>
        public static double LineSubtotal(double unitPrice, double quantity)
        {
            return Round2(unitPrice * quantity);
        }

        /// <summary>
        /// Computes the line discount amount. Callers must have already verified
        /// permission/feature gates; this only enforces arithmetic validity.
        /// </summary>
        public static double LineDiscountAmount(string discountType, double discountValue, double lineSubtotalBefore, string productName)
        {
            if (discountType == DiscountTypes.Percentage)
            {
                if (discountValue < 0 || discountValue > 100)
                    throw new DomainValidationException("Line discount percentage must be between 0 and 100");
                return Round2(lineSubtotalBefore * (discountValue / 100.0));
            }

            if (discountType == DiscountTypes.Fixed)
            {
                if (discountValue < 0)
                    throw new DomainValidationException("Line discount cannot be negative");
                if (discountValue > lineSubtotalBefore)
                    throw new DomainValidationException($"Line discount cannot exceed line total for '{productName}'");
                return Round2(discountValue);
            }

            throw new DomainValidationException("Invalid line discount type");
        }

        /// <summary>Sum of line finals restricted to lines that allow discounts.</summary>
        public static double EligibleTotal(IReadOnlyList<double> lineFinals, IReadOnlyList<bool> allowDiscount)
        {
            var eligible = 0.0;
            for (var i = 0; i < lineFinals.Count; i++)
            {
                if (allowDiscount[i])
                    eligible += lineFinals[i];
            }
            return eligible;
        }

        /// <summary>
        /// Invoice-level discount amount from a typed discount or the legacy flat
        /// discount field. Throws when the discount is invalid.
        /// </summary>
        public static double InvoiceDiscountAmount(string discountType, double discountValue, double flatDiscount, double eligibleTotal)
        {
            if (!string.IsNullOrEmpty(discountType) && discountValue > 0)
            {
                if (discountType == DiscountTypes.Percentage)
                {
                    if (discountValue < 0 || discountValue > 100)
                        throw new DomainValidationException("Percentage must be between 0 and 100");
                    return Round2(eligibleTotal * (discountValue / 100.0));
                }

                if (discountType == DiscountTypes.Fixed)
                {
                    if (discountValue < 0)
                        throw new DomainValidationException("Discount cannot be negative");
                    if (discountValue > eligibleTotal)
                        throw new DomainValidationException("Discount exceeds eligible amount");
                    return Round2(discountValue);
                }

                throw new DomainValidationException("Invalid discount type");
            }

            // Legacy flat-discount path: type/value are cleared.
            return Round2(Math.Max(0, flatDiscount));
        }

        /// <summary>
        /// Distributes an invoice discount proportionally over eligible lines
        /// (rounded per share) and corrects rounding drift on the last eligible
        /// line. Returns one share per line (0 for non-eligible lines).
        /// </summary>
        public static double[] DistributeInvoiceDiscount(double invoiceDiscountAmount, IReadOnlyList<double> lineFinals, IReadOnlyList<bool> allowDiscount)
        {
            var shares = new double[lineFinals.Count];
            if (!(invoiceDiscountAmount > 0) || !(lineFinals.Count > 0))
                return shares;

            var eligibleTotal = EligibleTotal(lineFinals, allowDiscount);
            if (!(eligibleTotal > 0))
                return shares;

            double distributed = 0;
            for (var i = 0; i < shares.Length; i++)
            {
                if (!allowDiscount[i])
                {
                    shares[i] = 0;
                    continue;
                }
                var share = Round2(invoiceDiscountAmount * (lineFinals[i] / eligibleTotal));
                distributed += share;
                shares[i] = share;
            }

            if (distributed != invoiceDiscountAmount)
            {
                var lastEligibleIdx = -1;
                for (var i = shares.Length - 1; i >= 0; i--)
                {
                    if (allowDiscount[i]) { lastEligibleIdx = i; break; }
                }
                if (lastEligibleIdx >= 0)
                    shares[lastEligibleIdx] += invoiceDiscountAmount - distributed;
            }

            return shares;
        }

        /// <summary>
        /// Rejects lines whose effective unit price (after all discounts) falls
        /// below cost price.
        /// </summary>
        public static void EnsureProfitProtection(double buyPrice, double quantity, double lineSubtotal, double totalLineDiscount, double unitPriceFallback, string productName)
        {
            var effectivePrice = quantity > 0
                ? Round2((lineSubtotal - totalLineDiscount) / quantity)
                : unitPriceFallback;
            if (effectivePrice < buyPrice)
                throw new DomainValidationException($"Profit protection: '{productName}' would sell below cost price");
        }

        /// <summary>
        /// Merges each line's own discount with its invoice-discount share,
        /// recomputes final totals and returns the rounded invoice total.
        /// </summary>
        public static double FinalizeLines(IList<InvoiceDetail> lines, IReadOnlyList<double> invoiceShares)
        {
            double totalAmount = 0;
            for (var i = 0; i < lines.Count; i++)
            {
                lines[i].DiscountAmount = Round2(lines[i].DiscountAmount + invoiceShares[i]);
                lines[i].FinalTotal = Round2(lines[i].LineSubtotal - lines[i].DiscountAmount);
                totalAmount += lines[i].FinalTotal;
            }
            return Round2(totalAmount);
        }
    }
}
