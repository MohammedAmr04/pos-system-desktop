using System.Collections.Generic;
using System.Linq;

namespace PosCs.Services
{
    /// <summary>
    /// System-defined tenant features. The same list must be mirrored in the frontend
    /// constants and in docs/FEATURES.md.
    /// </summary>
    public static class FeatureCatalog
    {
        public static readonly IReadOnlyList<string> Keys = new List<string>
        {
            "multiple_units",
            "multiple_barcodes",
            "wholesale_price",
            "product_discount",
            "invoice_discount",
            "price_override",
            "low_stock_report",
            "receipt_printing",
            "barcode_printing"
        };

        public static bool IsKnown(string key)
        {
            return Keys.Contains(key);
        }
    }
}
