using PosCs.Domain.Entities;
using System;

namespace PosCs.Domain.Rules
{
    /// <summary>
    /// A product is low on stock when a threshold is configured (greater than zero)
    /// and the stock quantity has fallen to or below it.
    /// </summary>
    public static class LowStockPolicy
    {
        public static bool IsLowStock(Product product)
        {
            return product != null
                && string.Equals(product.ProductType ?? "product", "product", StringComparison.OrdinalIgnoreCase)
                && product.LowStockThreshold > 0
                && product.StockQuantity <= product.LowStockThreshold;
        }
    }
}
