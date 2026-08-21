using System;
using System.Security.Cryptography;
using System.Text;

namespace PosCs.Domain.Rules
{
    /// <summary>
    /// Server-side license unlock code. The code is derived from the machine id with
    /// a secret that lives only on the backend — it is NOT shipped in the frontend
    /// bundle. Operators receive the 4-digit code from the vendor.
    /// </summary>
    public static class LicenseCode
    {
        private const string Secret = "POS-LICENSE-ACTIVATION-2026-v1";

        public static string ComputeUnlockCode(string machineId)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(Secret)))
            {
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes("unlock:" + (machineId ?? "")));
                var value = BitConverter.ToUInt32(hash, 0);
                return (value % 10000).ToString("D4");
            }
        }

        public static bool IsValidUnlockCode(string machineId, string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;
            var expected = ComputeUnlockCode(machineId);
            return string.Equals(expected, code.Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
