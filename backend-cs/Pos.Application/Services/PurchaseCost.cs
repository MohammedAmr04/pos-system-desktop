using System;

namespace PosCs.Application.Services
{
    public static class PurchaseCost
    {
        public static double PerBaseUnit(double enteredUnitCost, double quantityFactor)
        {
            if (quantityFactor <= 0)
                throw new ArgumentOutOfRangeException(nameof(quantityFactor));
            return Math.Round(enteredUnitCost / quantityFactor, 6);
        }

        public static double LineTotal(double quantity, double baseUnitCost, double quantityFactor)
        {
            return Math.Round(quantity * baseUnitCost * quantityFactor, 2);
        }
    }
}
