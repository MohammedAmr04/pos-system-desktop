using System;
using System.Runtime.InteropServices;

namespace PosCs.Services
{
    public interface IPrinterService
    {
        bool PrintBytes(string printerName, byte[] bytes, string documentName = "POS Receipt");
    }

    public class PrinterService : IPrinterService
    {
        [DllImport("winspool.drv", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern bool StartDocPrinter(IntPtr hPrinter, int level, ref DOCINFO pDocInfo);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private struct DOCINFO
        {
            [MarshalAs(UnmanagedType.LPStr)] public string pDocName;
            [MarshalAs(UnmanagedType.LPStr)] public string pOutputFile;
            [MarshalAs(UnmanagedType.LPStr)] public string pDataType;
        }

        public bool PrintBytes(string printerName, byte[] bytes, string documentName = "POS Receipt")
        {
            if (!OpenPrinter(printerName, out IntPtr hPrinter, IntPtr.Zero))
            {
                Console.Error.WriteLine($"[PRINT] Failed to open printer '{printerName}'");
                return false;
            }

            try
            {
                var docInfo = new DOCINFO
                {
                    pDocName = documentName,
                    pDataType = "RAW"
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
