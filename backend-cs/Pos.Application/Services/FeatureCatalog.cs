using System.Collections.Generic;
using System.Linq;

namespace PosCs.Application.Services
{
    /// <summary>
    /// System-defined tenant features. The same list must be mirrored in the frontend
    /// constants and in docs/FEATURES.md.
    /// </summary>
    public static class FeatureCatalog
    {
        public static readonly System.Collections.Generic.IReadOnlyList<string> Keys = new List<string>
        {
            "multiple_units",
            "multiple_barcodes",
            "wholesale_price",
            "product_discount",
            "invoice_discount",
            "price_override",
            "low_stock_report",
            "receipt_printing",
            "barcode_printing",
            "categories",
            "brands"
        };

        public static bool IsKnown(string key)
        {
            return Keys.Contains(key);
        }
    }
}
