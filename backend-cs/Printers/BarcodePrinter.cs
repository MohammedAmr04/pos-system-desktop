using System.Collections.Generic;

namespace PosCs.Printers
{
    public class BarcodePrinter
    {
        public static byte[] GetBarcodeBytes(string barcode)
        {
            List<byte> bytes = new List<byte>();

            // Center alignment
            bytes.AddRange(new byte[] { 0x1B, 0x61, 0x01 });

            // Set barcode height
            bytes.AddRange(new byte[] { 0x1D, 0x68, 80 });
            
            // Set barcode width
            bytes.AddRange(new byte[] { 0x1D, 0x77, 0x03 });
            
            // Text position below barcode
            bytes.AddRange(new byte[] { 0x1D, 0x48, 0x02 });

            // Print Code128 barcode
            bytes.AddRange(new byte[] { 0x1D, 0x6B, 0x49 });
            bytes.Add((byte)barcode.Length);
            bytes.AddRange(System.Text.Encoding.ASCII.GetBytes(barcode));

            return bytes.ToArray();
        }
    }
}
