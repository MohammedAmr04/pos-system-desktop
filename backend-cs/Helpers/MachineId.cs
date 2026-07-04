using System;
using System.Management;
using System.Security.Cryptography;
using System.Text;

namespace PosCs.Helpers
{
    public static class MachineId
    {
        public static string GetMachineId()
        {
            var parts = new StringBuilder();

            try
            {
                parts.Append(Environment.MachineName);
                parts.Append('-');
                parts.Append(Environment.OSVersion.Platform);
                parts.Append('-');

                // Processor architecture
                parts.Append(Environment.Is64BitOperatingSystem ? "x64" : "x86");
                parts.Append('-');

                // CPU model
                string cpuModel = "unknown";
                try
                {
                    using (var searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor"))
                    {
                        foreach (var obj in searcher.Get())
                        {
                            cpuModel = obj["ProcessorId"]?.ToString() ?? "unknown";
                            break;
                        }
                    }
                }
                catch
                {
                    // Fallback: use environment processor count
                    cpuModel = Environment.ProcessorCount.ToString();
                }
                parts.Append(cpuModel);
            }
            catch
            {
                parts.Append("unknown");
            }

            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(parts.ToString()));
                return BitConverter.ToString(hash).Replace("-", "").Substring(0, 16).ToLower();
            }
        }
    }
}
