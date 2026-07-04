using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using PosCs.Formatters;

namespace PosCs.Builders
{
    public class ReceiptItemModel
    {
        public string Name { get; set; }
        public int Quantity { get; set; }
        public double SalePrice { get; set; }
    }

    public class ReceiptInvoiceModel
    {
        public string Id { get; set; }
        public DateTime? CreatedAt { get; set; }
        public double Discount { get; set; }
        public double TotalAmount { get; set; }
    }

    public class ReceiptBuilder : IDisposable
    {
        private readonly int ReceiptWidth; 
        private readonly Bitmap _bitmap;
        private readonly Graphics _graphics;
        private int _currentY = 0;
        
        private readonly Font _regularFont;
        private readonly Font _boldFont;
        private readonly Font _headerFont;
        private readonly StringFormat _rtlFormat;
        private readonly StringFormat _centerFormat;
        private readonly StringFormat _ltrFormat;

        public ReceiptBuilder(int width = 384)
        {
            ReceiptWidth = width;
            _bitmap = new Bitmap(ReceiptWidth, 4000);
            _graphics = Graphics.FromImage(_bitmap);
            
            _graphics.Clear(Color.White);
            _graphics.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;

            _regularFont = new Font("Tahoma", 10, FontStyle.Regular);
            _boldFont = new Font("Tahoma", 10, FontStyle.Bold);
            _headerFont = new Font("Tahoma", 12, FontStyle.Bold);

            _rtlFormat = new StringFormat(StringFormatFlags.DirectionRightToLeft)
            {
                Alignment = StringAlignment.Near,
                LineAlignment = StringAlignment.Near
            };

            _centerFormat = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Near
            };
            
            _ltrFormat = new StringFormat
            {
                Alignment = StringAlignment.Near,
                LineAlignment = StringAlignment.Near
            };
        }

        public void AddLogo(string logoPath)
        {
            if (File.Exists(logoPath))
            {
                try
                {
                    using (Image logo = Image.FromFile(logoPath))
                    {
                        float ratio = (float)logo.Height / logo.Width;
                        int newWidth = ReceiptWidth / 2;
                        int newHeight = (int)(newWidth * ratio);
                        
                        int x = (ReceiptWidth - newWidth) / 2;
                        
                        _graphics.DrawImage(logo, x, _currentY, newWidth, newHeight);
                        _currentY += newHeight + 10;
                    }
                }
                catch (Exception)
                {
                    // Ignore image loading errors
                }
            }
        }

        public void AddHeader(ReceiptInvoiceModel invoice)
        {
            DrawStringCenter($"رقم الفاتورة: #{invoice.Id}", _headerFont);
            
            DateTime printDate = invoice.CreatedAt.HasValue ? invoice.CreatedAt.Value : DateTime.Now;
            DrawStringCenter($"التاريخ: {printDate:yyyy-MM-dd HH:mm}", _regularFont);
            
            DrawLine();
        }

        public void AddItems(IEnumerable<ReceiptItemModel> items)
        {
            foreach (var item in items)
            {
                double itemTotal = item.Quantity * item.SalePrice;
                string leftText = $"{itemTotal:F2}";
                string rightText = $"{item.Name} x{item.Quantity}";

                DrawItemLine(rightText, leftText, _regularFont);
            }
            DrawLine();
        }

        public void AddTotals(ReceiptInvoiceModel invoice)
        {
            if (invoice.Discount > 0)
            {
                DrawItemLine("الخصم:", TextFormatter.FormatCurrency(invoice.Discount), _regularFont);
                DrawLine();
            }

            DrawItemLine("الإجمالي النهائي:", TextFormatter.FormatCurrency(invoice.TotalAmount), _boldFont);
            DrawLine();
        }

        public void AddFooter()
        {
            DrawStringCenter("شكراً لزيارتكم!", _boldFont);
            DrawStringCenter("Software by Antigravity", _regularFont);
            _currentY += 40; // Add extra padding at the bottom for tearing
        }

        private void DrawItemLine(string rightRtlText, string leftLtrText, Font font)
        {
            // Measure right side text (Arabic)
            SizeF rightSize = _graphics.MeasureString(rightRtlText, font, ReceiptWidth, _rtlFormat);
            
            // Measure left side text (Numbers/Currency)
            SizeF leftSize = _graphics.MeasureString(leftLtrText, font, ReceiptWidth, _ltrFormat);

            // Draw right side (RTL handles Arabic shaping perfectly natively via GDI+)
            RectangleF rightRect = new RectangleF(0, _currentY, ReceiptWidth, rightSize.Height);
            _graphics.DrawString(rightRtlText, font, Brushes.Black, rightRect, _rtlFormat);

            // Draw left side (LTR)
            RectangleF leftRect = new RectangleF(0, _currentY, ReceiptWidth, leftSize.Height);
            _graphics.DrawString(leftLtrText, font, Brushes.Black, leftRect, _ltrFormat);

            _currentY += (int)Math.Max(rightSize.Height, leftSize.Height) + 5;
        }

        private void DrawStringCenter(string text, Font font)
        {
            SizeF size = _graphics.MeasureString(text, font, ReceiptWidth, _centerFormat);
            RectangleF rect = new RectangleF(0, _currentY, ReceiptWidth, size.Height);
            _graphics.DrawString(text, font, Brushes.Black, rect, _centerFormat);
            _currentY += (int)size.Height + 5;
        }

        private void DrawLine()
        {
            _currentY += 5;
            _graphics.DrawLine(Pens.Black, 10, _currentY, ReceiptWidth - 10, _currentY);
            _currentY += 10;
        }

        public Bitmap GetFinishedReceipt()
        {
            // Crop the final bitmap to the actual used height
            Bitmap finalReceipt = new Bitmap(ReceiptWidth, _currentY);
            using (Graphics g = Graphics.FromImage(finalReceipt))
            {
                g.DrawImage(_bitmap, new Rectangle(0, 0, ReceiptWidth, _currentY), 
                                     new Rectangle(0, 0, ReceiptWidth, _currentY), GraphicsUnit.Pixel);
            }
            return finalReceipt;
        }

        public void Dispose()
        {
            _graphics?.Dispose();
            _bitmap?.Dispose();
            _regularFont?.Dispose();
            _boldFont?.Dispose();
            _headerFont?.Dispose();
            _rtlFormat?.Dispose();
            _centerFormat?.Dispose();
            _ltrFormat?.Dispose();
        }
    }
}
