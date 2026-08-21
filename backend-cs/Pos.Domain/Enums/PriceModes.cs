using System;

namespace PosCs.Domain.Enums
{
    /// <summary>
    /// Wire/storage values for invoice price mode. Stored as a lowercase string in
    /// the database, so these are constants rather than a C# enum.
    /// </summary>
    public static class PriceModes
    {
        public const string Retail = "retail";
        public const string Wholesale = "wholesale";

        public static string Normalize(string wireValue)
        {
            return string.Equals(wireValue, Wholesale, StringComparison.OrdinalIgnoreCase)
                ? Wholesale
                : Retail;
        }
    }
}
