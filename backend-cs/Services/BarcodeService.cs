using System;

namespace PosCs.Services
{
    public static class BarcodeService
    {
        public static string GenerateBarcode()
        {
            var rng = new Random();
            return rng.Next(0, int.MaxValue).ToString("D12");
        }
    }
}
