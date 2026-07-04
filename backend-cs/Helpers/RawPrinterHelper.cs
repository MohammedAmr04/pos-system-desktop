using System;
using System.Runtime.InteropServices;
using System.Text;

namespace PosCs.Helpers
{
    public static class RawPrinterHelper
    {
        // تم تغيير CharSet هنا إلى Ansi
        [DllImport("winspool.drv", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool ClosePrinter(IntPtr hPrinter);

        // تم تغيير CharSet هنا إلى Ansi لتتوافق مع الـ Struct المعدل
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

        // تعديل الـ Struct بالكامل ليعتمد على Ansi و LPStr
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private struct DOCINFO
        {
            [MarshalAs(UnmanagedType.LPStr)]
            public string pDocName;
            [MarshalAs(UnmanagedType.LPStr)]
            public string pOutputFile;
            [MarshalAs(UnmanagedType.LPStr)]
            public string pDataType;
        }

        public static bool SendStringToPrinter(string printerName, string text)
        {
            // لدعم اللغة العربية بشكل صحيح على الطابعات الحرارية في مصر (كود بيج 1256)
            // يمكنك فك التعليق عن السطر بالأسفل إذا ظهرت علامات استفهام:
            // var bytes = Encoding.GetEncoding(1256).GetBytes(text);
            
            var bytes = Encoding.ASCII.GetBytes(text); 
            return SendBytesToPrinter(printerName, bytes);
        }

        public static bool SendBytesToPrinter(string printerName, byte[] bytes)
        {
            // استخدام الـ printerName مباشرة وتجنب الـ Normalize إن لم تكن هناك حاجة لها
            if (!OpenPrinter(printerName, out IntPtr hPrinter, IntPtr.Zero))
            {
                Console.Error.WriteLine($"[PRINT] Failed to open printer '{printerName}'");
                return false;
            }

            try
            {
                var docInfo = new DOCINFO
                {
                    pDocName = "POS Receipt",
                    pDataType = "RAW" // نمررها بحروف كبيرة وبترميز Ansi صريح الآن
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