using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PosCs.Domain.Rules
{
    /// <summary>
    /// Server-side license unlock code. The code is derived from the machine id with
    /// a secret that lives only on the backend — it is NOT shipped in the frontend
    /// bundle and it is NOT hardcoded in source code. Operators receive the 4-digit
    /// code from the vendor.
    /// </summary>
    public static class LicenseCode
    {
        private const string SecretEnvVar = "POS_LICENSE_SECRET";
        private const string SecretFileName = "license.secret";

        private static string GetSecret()
        {
            var env = Environment.GetEnvironmentVariable(SecretEnvVar);
            if (!string.IsNullOrWhiteSpace(env))
                return env.Trim();

            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SecretFileName);
            if (File.Exists(path))
            {
                var fileSecret = File.ReadAllText(path).Trim();
                if (!string.IsNullOrWhiteSpace(fileSecret))
                    return fileSecret;
            }

            throw new InvalidOperationException(
                $"License secret is not configured. Set the {SecretEnvVar} environment variable or create a {SecretFileName} file next to the executable.");
        }

        public static string ComputeUnlockCode(string machineId)
        {
            var secret = GetSecret();
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
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
