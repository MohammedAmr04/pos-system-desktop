using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PosCs.Services
{
    public class ReceiptService
    {
        private const int LineWidth = 42;

        public string BuildReceiptText(List<ReceiptItem> items, double total, double discount, bool arabic)
        {
            return arabic ? FormatArabicReceipt(items, total, discount)
                          : FormatEnglishReceipt(items, total, discount);
        }

        public string FormatEnglishReceipt(List<ReceiptItem> items, double total, double discount)
        {
            var sb = new StringBuilder();
            var line = new string('=', 40);
            sb.AppendLine($"\n{line}");
            sb.AppendLine("     POINT OF SALE RECEIPT");
            sb.AppendLine(line);
            sb.AppendLine();
            sb.AppendLine($"{"Item".PadRight(20)} {"Qty".PadLeft(4)} {"Price".PadLeft(8)}");
            sb.AppendLine(new string('-', 40));

            foreach (var item in items)
            {
                var lineTotal = item.SalePrice * item.Quantity;
                sb.Append($"{item.Name.Substring(0, Math.Min(item.Name.Length, 18)).PadRight(20)} ");
                sb.Append($"{item.Quantity.ToString().PadLeft(4)} ");
                sb.AppendLine($"{lineTotal:F2}".PadLeft(8));
            }

            sb.AppendLine(new string('-', 40));
            sb.AppendLine($"{"Subtotal:".PadRight(24)} {total:F2}".PadLeft(12));

            if (discount > 0)
            {
                sb.AppendLine($"{"Discount:".PadRight(24)} {discount:F2}".PadLeft(12));
                var finalTotal = Math.Max(0, total - discount);
                sb.AppendLine($"{"Total:".PadRight(24)} {finalTotal:F2}".PadLeft(12));
            }

            sb.AppendLine($"\n{line}");
            sb.AppendLine("       Thank you for your purchase!");
            sb.AppendLine(line);
            return sb.ToString();
        }

        public string FormatArabicReceipt(List<ReceiptItem> items, double total, double discount)
        {
            var sb = new StringBuilder();
            var line = new string('=', 40);
            sb.AppendLine($"\n{line}");
            sb.AppendLine("     إيصال المبيعات");
            sb.AppendLine(line);
            sb.AppendLine();
            sb.AppendLine($"{"الكمية".PadLeft(8)} {"السعر".PadLeft(10)} {"الصنف".PadRight(20)}");
            sb.AppendLine(new string('-', 40));

            foreach (var item in items)
            {
                var lineTotal = item.SalePrice * item.Quantity;
                sb.Append($"{item.Quantity.ToString().PadLeft(8)} ");
                sb.Append($"{lineTotal:F2}".PadLeft(10) + " ");
                sb.AppendLine(item.Name.Substring(0, Math.Min(item.Name.Length, 18)).PadRight(20));
            }

            sb.AppendLine(new string('-', 40));
            sb.AppendLine($"{"المجموع:".PadLeft(8)} {total:F2}".PadLeft(28));

            if (discount > 0)
            {
                sb.AppendLine($"{"الخصم:".PadLeft(8)} {discount:F2}".PadLeft(28));
                var finalTotal = Math.Max(0, total - discount);
                sb.AppendLine($"{"الإجمالي:".PadLeft(8)} {finalTotal:F2}".PadLeft(28));
            }

            sb.AppendLine($"\n{line}");
            sb.AppendLine("       شكراً لشرائك!");
            sb.AppendLine(line);
            return sb.ToString();
        }

        public string BuildEscPosReceipt(List<ReceiptItem> items, double total, double discount,
            string invoiceId, DateTime? createdAt)
        {
            var data = new List<byte>();

            // Initialize printer
            data.AddRange(new byte[] { 0x1B, 0x40 }); // ESC @

            // Center align
            data.AddRange(new byte[] { 0x1B, 0x61, 0x01 }); // ESC a 1

            // Bold on
            data.AddRange(new byte[] { 0x1B, 0x45, 0x01 }); // ESC E 1

            // Double height text
            data.AddRange(new byte[] { 0x1D, 0x21, 0x11 }); // GS ! 0x11

            AddLine(data, "POS SYSTEM");

            // Normal text
            data.AddRange(new byte[] { 0x1D, 0x21, 0x00 }); // GS ! 0
            data.AddRange(new byte[] { 0x1B, 0x45, 0x00 }); // ESC E 0

            AddLine(data, "SALES RECEIPT");
            AddLine(data, new string('-', 32));

            // Left align
            data.AddRange(new byte[] { 0x1B, 0x61, 0x00 }); // ESC a 0

            var dateStr = createdAt?.ToString("MM/dd/yyyy HH:mm:ss") ?? DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");
            AddLine(data, $"Date: {dateStr}");
            AddLine(data, $"Invoice #: {invoiceId?.PadLeft(6, '0') ?? "000000"}");
            AddLine(data, new string('-', 32));

            // Header
            string[] headers = { "ITEM", "QTY", "PRICE" };
            AddLine(data, FormatTableRow(headers[0], headers[1], headers[2]));
            AddLine(data, new string('-', 32));

            foreach (var item in items)
            {
                var name = item.Name.Length > 20 ? item.Name.Substring(0, 20) : item.Name;
                var lineTotal = item.SalePrice * item.Quantity;
                var row = FormatTableRow(name, item.Quantity.ToString(), $"${lineTotal:F2}");
                AddLine(data, row);
            }

            AddLine(data, new string('-', 32));

            if (discount > 0)
            {
                var subtotal = total + discount;
                AddLine(data, FormatAmountLine("SUBTOTAL", subtotal));
                AddLine(data, FormatAmountLine("DISCOUNT", -discount));
                AddLine(data, new string('-', 32));
            }

            // Bold + double height for total
            data.AddRange(new byte[] { 0x1B, 0x45, 0x01 });
            data.AddRange(new byte[] { 0x1D, 0x21, 0x11 });
            AddLine(data, FormatAmountLine("TOTAL", total));
            data.AddRange(new byte[] { 0x1D, 0x21, 0x00 });
            data.AddRange(new byte[] { 0x1B, 0x45, 0x00 });

            // Center footer
            data.AddRange(new byte[] { 0x1B, 0x61, 0x01 });
            AddLine(data, "");
            AddLine(data, "THANK YOU!");
            AddLine(data, "");
            AddLine(data, "");

            // Cut paper
            data.AddRange(new byte[] { 0x1B, 0x6D }); // ESC m (partial cut)
            // Feed
            data.AddRange(new byte[] { 0x1B, 0x64, 0x05 }); // ESC d 5

            return Encoding.ASCII.GetString(data.ToArray());
        }

        private string FormatTableRow(string left, string center, string right)
        {
            var totalWidth = 32;
            var leftWidth = 16;
            var centerWidth = 6;
            var rightWidth = totalWidth - leftWidth - centerWidth;

            var l = left.Length > leftWidth ? left.Substring(0, leftWidth) : left.PadRight(leftWidth);
            var c = center.Length > centerWidth ? center.Substring(0, centerWidth) : center.PadLeft(centerWidth);
            var r = right.Length > rightWidth ? right.Substring(0, rightWidth) : right.PadLeft(rightWidth);

            return l + c + r;
        }

        private string FormatAmountLine(string label, double amount)
        {
            var totalWidth = 32;
            var labelWidth = 16;
            var amountStr = $"${amount:F2}";
            var padding = totalWidth - labelWidth - amountStr.Length;
            return label.PadRight(labelWidth) + new string(' ', Math.Max(0, padding)) + amountStr;
        }

        private void AddLine(List<byte> data, string text)
        {
            data.AddRange(Encoding.ASCII.GetBytes(text + "\n"));
        }

        public string BuildEscPosBarcode(string barcode, string productName, double? price, int count)
        {
            var data = new List<byte>();

            for (int i = 0; i < count; i++)
            {
                if (i > 0)
                {
                    data.AddRange(Encoding.ASCII.GetBytes("\n"));
                }

                // Center
                data.AddRange(new byte[] { 0x1B, 0x61, 0x01 });

                // Product name
                if (!string.IsNullOrEmpty(productName))
                {
                    data.AddRange(new byte[] { 0x1B, 0x45, 0x01 });
                    data.AddRange(new byte[] { 0x1D, 0x21, 0x11 });
                    var name = productName.Length > 20 ? productName.Substring(0, 20) : productName;
                    data.AddRange(Encoding.ASCII.GetBytes(name + "\n"));
                    data.AddRange(new byte[] { 0x1D, 0x21, 0x00 });
                    data.AddRange(new byte[] { 0x1B, 0x45, 0x00 });
                    data.AddRange(Encoding.ASCII.GetBytes("\n"));
                }

                // Code128 barcode
                // GS k 73 (Code128) followed by data
                data.AddRange(new byte[] { 0x1D, 0x6B, 0x49 });
                var barcodeBytes = Encoding.ASCII.GetBytes(barcode ?? "");
                data.Add((byte)(barcodeBytes.Length + 2));
                // Start Code B
                data.Add(0x7B);
                data.Add(0x42);
                data.AddRange(barcodeBytes);

                if (price.HasValue)
                {
                    data.AddRange(Encoding.ASCII.GetBytes("\n\n"));
                    data.AddRange(new byte[] { 0x1D, 0x21, 0x22 });
                    data.AddRange(Encoding.ASCII.GetBytes($"${price.Value:F2}"));
                    data.AddRange(new byte[] { 0x1D, 0x21, 0x00 });
                }

                data.AddRange(Encoding.ASCII.GetBytes("\n"));
                data.AddRange(new byte[] { 0x1B, 0x6D }); // Cut
            }

            return Encoding.ASCII.GetString(data.ToArray());
        }
    }

    public class ReceiptItem
    {
        public string Name { get; set; }
        public int Quantity { get; set; }
        public double SalePrice { get; set; }
    }
}
