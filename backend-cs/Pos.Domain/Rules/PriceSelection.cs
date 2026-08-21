namespace PosCs.Domain.Rules
{
    /// <summary>
    /// Retail/wholesale price selection for invoice lines, matching the legacy
    /// controller behavior: wholesale applies only when explicitly selected and a
    /// positive wholesale price exists; a submitted price overrides both.
    /// </summary>
    public static class PriceSelection
    {
        public static double SelectSellingPrice(string priceMode, double retailPrice, double? wholesalePrice)
        {
            var originalPrice = retailPrice;
            if (priceMode == Enums.PriceModes.Wholesale && wholesalePrice.HasValue && wholesalePrice.Value > 0)
                originalPrice = wholesalePrice.Value;
            return originalPrice;
        }

        public static double ResolveSubmittedPrice(double submittedUnitPrice, double originalPrice)
        {
            return submittedUnitPrice > 0 ? submittedUnitPrice : originalPrice;
        }

        /// <summary>True when the client submitted an explicit price override.</summary>
        public static bool IsPriceOverride(double submittedUnitPrice, double originalPrice)
        {
            return submittedUnitPrice > 0 && System.Math.Abs(submittedUnitPrice - originalPrice) > 0.005;
        }
    }
}
