using System;
using System.IO;

namespace PosCs
{
    internal static class EnvironmentFile
    {
        public static void Load()
        {
            var directories = new[]
            {
                AppDomain.CurrentDomain.BaseDirectory,
                Environment.CurrentDirectory
            };

            foreach (var directory in directories)
            {
                var current = new DirectoryInfo(directory);
                for (var depth = 0; current != null && depth < 5; depth++, current = current.Parent)
                {
                    var path = Path.Combine(current.FullName, ".env");
                    if (File.Exists(path))
                        LoadFile(path);
                }
            }
        }

        private static void LoadFile(string path)
        {
            foreach (var rawLine in File.ReadAllLines(path))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                    continue;

                var separator = line.IndexOf('=');
                if (separator <= 0) continue;

                var key = line.Substring(0, separator).Trim();
                var value = line.Substring(separator + 1).Trim().Trim('"');
                if (key.Length > 0 && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key)))
                    Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}
