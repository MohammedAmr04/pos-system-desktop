using System;
using System.Management;
using System.Security.Cryptography;
using System.Text;

namespace PosCs.Infrastructure.Devices
{
    /// <summary>Stable per-machine identifier derived from hardware traits (SHA-256, 16 hex chars).</summary>
    public class MachineIdProvider : PosCs.Application.Ports.IMachineIdProvider
    {
        public string GetMachineId()
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
