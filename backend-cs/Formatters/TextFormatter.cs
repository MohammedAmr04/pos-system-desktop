using System;

namespace PosCs.Formatters
{
    public class TextFormatter
    {
        public static string FormatCurrency(double amount)
        {
            return $"{amount:F2} ج.م";
        }

        public static string FormatItemLine(string itemName, string priceText, int totalWidth = 42)
        {
            // For image drawing, we don't necessarily need to pad with spaces 
            // since we can draw strings at explicit X coordinates. 
            // However, returning a single formatted string can be useful for fallback printing.
            
            int spacesNeeded = totalWidth - (itemName.Length + priceText.Length);
            if (spacesNeeded < 1) spacesNeeded = 1;

            return itemName + new string(' ', spacesNeeded) + priceText;
        }
    }
}
