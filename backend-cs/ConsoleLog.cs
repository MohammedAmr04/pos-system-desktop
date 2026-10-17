using System;
using System.IO;
using System.Text;
using PosCs.Infrastructure.Config;

namespace PosCs
{
    /// <summary>
    /// Mirrors Console.Out/Console.Error into a daily log file. Under IIS there is no
    /// console, so the ~190 existing Console.* call sites would otherwise be lost.
    /// Enabled by setting POS_LOG_DIR (env var or Web.config appSettings); relative
    /// paths resolve against the site root. Self-host keeps the console untouched —
    /// output is teed to both destinations.
    /// </summary>
    internal static class ConsoleLog
    {
        private static readonly object Sync = new object();
        private static string _directory;
        private static StreamWriter _writer;
        private static string _writerDate;
        private static bool _disabled;
        private static bool _installed;

        public static void Install()
        {
            if (_installed) return;
            _installed = true;

            var dir = PosConfig.Get("POS_LOG_DIR");
            if (string.IsNullOrWhiteSpace(dir)) return;

            if (!Path.IsPathRooted(dir))
                dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dir);

            try
            {
                Directory.CreateDirectory(dir);
            }
            catch
            {
                return;
            }

            _directory = dir;
            PruneOldFiles();

            Console.SetOut(new TeeWriter(Console.Out));
            Console.SetError(new TeeWriter(Console.Error));
        }

        private static void WriteLine(string line)
        {
            lock (Sync)
            {
                if (_disabled || _directory == null) return;
                try
                {
                    EnsureWriter();
                    _writer.Write(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                    _writer.Write(" ");
                    _writer.WriteLine(line);
                    _writer.Flush();
                }
                catch
                {
                    _disabled = true;
                    try { _writer?.Dispose(); } catch { }
                    _writer = null;
                }
            }
        }

        private static void EnsureWriter()
        {
            var date = DateTime.Now.ToString("yyyyMMdd");
            if (_writer != null && date == _writerDate) return;

            try { _writer?.Dispose(); } catch { }
            var path = Path.Combine(_directory, "pos-" + date + ".log");
            _writer = new StreamWriter(path, true, Encoding.UTF8);
            _writerDate = date;
        }

        private static void PruneOldFiles()
        {
            try
            {
                var cutoff = DateTime.Now.AddDays(-14);
                foreach (var path in Directory.GetFiles(_directory, "pos-*.log"))
                {
                    if (File.GetLastWriteTime(path) < cutoff)
                        File.Delete(path);
                }
            }
            catch
            {
            }
        }

        private sealed class TeeWriter : TextWriter
        {
            private readonly TextWriter _original;
            private readonly StringBuilder _pending = new StringBuilder();

            public TeeWriter(TextWriter original)
            {
                _original = original;
            }

            public override Encoding Encoding => _original.Encoding;

            public override void Write(char value)
            {
                WriteOriginal(value.ToString());
                Append(value.ToString());
            }

            public override void Write(string value)
            {
                if (value == null) return;
                WriteOriginal(value);
                Append(value);
            }

            public override void Write(char[] buffer, int index, int count)
            {
                if (buffer == null || count <= 0) return;
                var text = new string(buffer, index, count);
                WriteOriginal(text);
                Append(text);
            }

            private void WriteOriginal(string text)
            {
                try
                {
                    _original.Write(text);
                }
                catch
                {
                    // No usable console (e.g. IIS worker process); the file sink still records it.
                }
            }

            private void Append(string text)
            {
                lock (_pending)
                {
                    _pending.Append(text);
                    var content = _pending.ToString();
                    var start = 0;
                    while (true)
                    {
                        var newline = content.IndexOf('\n', start);
                        if (newline < 0) break;
                        var line = content.Substring(start, newline - start);
                        if (line.EndsWith("\r")) line = line.Substring(0, line.Length - 1);
                        ConsoleLog.WriteLine(line);
                        start = newline + 1;
                    }
                    _pending.Remove(0, Math.Min(_pending.Length, start));
                }
            }
        }
    }
}
