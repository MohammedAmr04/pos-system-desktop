using PosCs.Domain.Entities;
using PosCs.Domain.Exceptions;

namespace PosCs.Domain.Rules
{
    /// <summary>
    /// Validation rules for product units and barcodes. Messages match the legacy
    /// API responses exactly.
    /// </summary>
    public static class UnitRules
    {
        public static void ValidateNewUnit(string unitName, double quantityFactor, double retailPrice, double? wholesalePrice)
        {
            if (string.IsNullOrWhiteSpace(unitName))
                throw new DomainValidationException("Unit name is required");
            if (quantityFactor <= 1)
                throw new DomainValidationException("Quantity factor must be greater than 1");
            if (retailPrice <= 0)
                throw new DomainValidationException("Retail price is required");
            if (retailPrice < 0)
                throw new DomainValidationException("Retail price cannot be negative");
            if (wholesalePrice.HasValue && wholesalePrice.Value < 0)
                throw new DomainValidationException("Wholesale price cannot be negative");
        }

        public static void ValidateUnitUpdate(ProductUnit unit, string unitName, double? quantityFactor, double? retailPrice, double? wholesalePrice)
        {
            if (unit.IsBaseUnit && quantityFactor.HasValue && quantityFactor.Value != 1)
                throw new DomainValidationException("Base unit quantity factor must always be 1");
            if (!unit.IsBaseUnit && quantityFactor.HasValue && quantityFactor.Value <= 1)
                throw new DomainValidationException("Quantity factor must be greater than 1");
            if (retailPrice.HasValue && retailPrice.Value <= 0)
                throw new DomainValidationException("Retail price is required");
            if (retailPrice.HasValue && retailPrice.Value < 0)
                throw new DomainValidationException("Retail price cannot be negative");
            if (wholesalePrice.HasValue && wholesalePrice.Value < 0)
                throw new DomainValidationException("Wholesale price cannot be negative");
        }

        public static void EnsureNotBaseUnit(ProductUnit unit)
        {
            if (unit.IsBaseUnit)
                throw new DomainValidationException("The base unit cannot be deleted.");
        }

        public static void EnsureNotDefaultBarcode(ProductBarcode barcode)
        {
            if (barcode.IsDefault)
                throw new DomainValidationException("The default barcode cannot be deleted.");
        }
    }
}
