using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace PosCs.Helpers
{
    public static class RawPrinterHelper
    {
        [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool StartDocPrinter(IntPtr hPrinter, int level, ref DOCINFO pDocInfo);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct DOCINFO
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pDocName;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pOutputFile;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pDataType;
        }

        public static bool SendStringToPrinter(string printerName, string text)
        {
            var bytes = Encoding.ASCII.GetBytes(text);
            return SendBytesToPrinter(printerName, bytes);
        }

        public static bool SendBytesToPrinter(string printerName, byte[] bytes)
        {
            if (!OpenPrinter(printerName.Normalize(), out IntPtr hPrinter, IntPtr.Zero))
            {
                Console.Error.WriteLine($"[PRINT] Failed to open printer '{printerName}'");
                return false;
            }

            try
            {
                var docInfo = new DOCINFO
                {
                    pDocName = "POS Receipt",
                    pDataType = Raw
                };

                if (!StartDocPrinter(hPrinter, 1, ref docInfo))
                {
                    var error = Marshal.GetLastWin32Error();
                    Console.Error.WriteLine($"[PRINT] StartDocPrinter failed. Win32 Error = {error}");
                    return false;
                }

                if (!StartPagePrinter(hPrinter))
                {
                    var error = Marshal.GetLastWin32Error();
                    Console.Error.WriteLine($"[PRINT] StartPagePrinter failed. Win32 Error = {error}");
                    return false;
                }

                var pBytes = Marshal.AllocHGlobal(bytes.Length);
                try
                {
                    Marshal.Copy(bytes, 0, pBytes, bytes.Length);
                    if (!WritePrinter(hPrinter, pBytes, bytes.Length, out int written))
                    {
                        Console.Error.WriteLine("[PRINT] WritePrinter failed");
                        return false;
                    }
                    if (written != bytes.Length)
                    {
                        Console.Error.WriteLine($"[PRINT] Only wrote {written} of {bytes.Length} bytes");
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(pBytes);
                }

                EndPagePrinter(hPrinter);
                EndDocPrinter(hPrinter);
                return true;
            }
            finally
            {
                ClosePrinter(hPrinter);
            }
        }
    }
}
