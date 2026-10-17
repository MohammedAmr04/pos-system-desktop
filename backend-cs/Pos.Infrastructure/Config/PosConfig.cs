using System;
using System.Configuration;

namespace PosCs.Infrastructure.Config
{
    public static class PosConfig
    {
        /// <summary>
        /// Copies appSettings entries into process environment variables so that
        /// legacy code reading Environment.GetEnvironmentVariable directly keeps
        /// working under IIS, where no .env file is loaded. Existing process
        /// environment variables always win; empty appSettings values are skipped.
        /// </summary>
        public static void SyncToEnvironment()
        {
            foreach (var key in ConfigurationManager.AppSettings.AllKeys)
            {
                if (string.IsNullOrWhiteSpace(key)) continue;
                if (!IsSupportedKey(key)) continue;
                if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key))) continue;

                var value = ConfigurationManager.AppSettings[key];
                if (string.IsNullOrWhiteSpace(value)) continue;
                Environment.SetEnvironmentVariable(key, value.Trim());
            }
        }

        private static bool IsSupportedKey(string key)
        {
            return key.StartsWith("POS_", StringComparison.OrdinalIgnoreCase)
                || key.Equals("PRINTER_NAME", StringComparison.OrdinalIgnoreCase)
                || key.Equals("API_PORT", StringComparison.OrdinalIgnoreCase);
        }

        public static string Get(string key)
        {
            var value = Environment.GetEnvironmentVariable(key);
            if (string.IsNullOrWhiteSpace(value))
                value = ConfigurationManager.AppSettings[key];
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        public static string Get(string key, string fallback)
        {
            return Get(key) ?? fallback;
        }

        public static bool GetBool(string key, bool fallback)
        {
            var value = Get(key);
            if (value == null) return fallback;
            switch (value.ToLowerInvariant())
            {
                case "1":
                case "true":
                case "yes":
                case "on":
                    return true;
                case "0":
                case "false":
                case "no":
                case "off":
                    return false;
                default:
                    return fallback;
            }
        }
    }
}
