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
        public string UnitName { get; set; }
        public double Quantity { get; set; }
        public double SalePrice { get; set; }
        public double? FinalTotal { get; set; }
    }

    public class ReceiptInvoiceModel
    {
        public string Id { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int InvoiceNumber { get; set; }
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
        private readonly Font _storeNameFont;
        private readonly Font _totalFont;
        private readonly StringFormat _rtlFormat;
        private readonly StringFormat _centerFormat;
        private readonly StringFormat _ltrFormat;

        public ReceiptBuilder(int width = 576) // Default 576 dots for 80mm printer
        {
            ReceiptWidth = width;
            _bitmap = new Bitmap(ReceiptWidth, 4000);
            _graphics = Graphics.FromImage(_bitmap);
            
            _graphics.Clear(Color.White);
            _graphics.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;

            _regularFont = new Font("Tahoma", 16, FontStyle.Regular);
            _boldFont = new Font("Tahoma", 16, FontStyle.Bold);
            _headerFont = new Font("Tahoma", 18, FontStyle.Bold);
            _storeNameFont = new Font("Tahoma", 28, FontStyle.Bold);
            _totalFont = new Font("Tahoma", 20, FontStyle.Bold);

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
                        int newWidth = (int)(ReceiptWidth * 0.6f); // 60% of paper width
                        int newHeight = (int)(newWidth * ratio);
                        
                        int x = (ReceiptWidth - newWidth) / 2;
                        
                        _graphics.DrawImage(logo, x, _currentY, newWidth, newHeight);
                        _currentY += newHeight + 15;
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
          
            DrawLine();
            
            DrawStringCenter($"رقم الفاتورة: #{invoice.InvoiceNumber}", _headerFont);
            
            DateTime printDate = invoice.CreatedAt.HasValue ? invoice.CreatedAt.Value : DateTime.Now;
            DrawStringCenter($"التاريخ: {printDate:yyyy-MM-dd HH:mm}", _regularFont);
            
            DrawLine();
        }

        public void AddItems(IEnumerable<ReceiptItemModel> items)
        {
            foreach (var item in items)
            {
                double itemTotal = item.FinalTotal ?? (item.Quantity * item.SalePrice);
                string leftText = $"{itemTotal:F2}";
                string name = string.IsNullOrEmpty(item.UnitName) ? item.Name : $"{item.Name} ({item.UnitName})";
                string rightText = $"{name} : {item.Quantity}";

                DrawItemLine(rightText, leftText, _regularFont);
            }
            DrawLine();
        }

 public void AddTotals(ReceiptInvoiceModel invoice)
{
    if (invoice.Discount > 0)
    {
        double totalBeforeDiscount = invoice.TotalAmount + invoice.Discount;

        DrawLine();
        DrawItemLine("الإجمالي قبل الخصم:", TextFormatter.FormatCurrency(totalBeforeDiscount), _regularFont);
        
        DrawItemLine("الخصم:", TextFormatter.FormatCurrency(invoice.Discount), _regularFont);
        DrawLine();
    }
    else
    {
        DrawLine();
    }

    // الإجمالي النهائي (بخط عريض/كبير)
    DrawItemLine("الإجمالي النهائي:", TextFormatter.FormatCurrency(invoice.TotalAmount), _totalFont);
    DrawLine();
}

        public void AddFooter()
        {
            DrawStringCenter("شكراً لزيارتكم!", _boldFont);
            DrawStringCenter("Software by brazilyy", _regularFont);
            _currentY += 40; // Add extra padding at the bottom for tearing
        }

        private void DrawItemLine(string rightRtlText, string leftLtrText, Font font)
        {
            int margin = 15;
            int printableWidth = ReceiptWidth - (margin * 2);

            // Measure right side text (Arabic)
            SizeF rightSize = _graphics.MeasureString(rightRtlText, font, printableWidth, _rtlFormat);
            
            // Measure left side text (Numbers/Currency)
            SizeF leftSize = _graphics.MeasureString(leftLtrText, font, printableWidth, _ltrFormat);

            // Draw right side (RTL handles Arabic shaping perfectly natively via GDI+)
            RectangleF rightRect = new RectangleF(margin, _currentY, printableWidth, rightSize.Height);
            _graphics.DrawString(rightRtlText, font, Brushes.Black, rightRect, _rtlFormat);

            // Draw left side (LTR)
            RectangleF leftRect = new RectangleF(margin, _currentY, printableWidth, leftSize.Height);
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
            _currentY += 10;
            _graphics.DrawLine(Pens.Black, 15, _currentY, ReceiptWidth - 15, _currentY);
            _currentY += 15;
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
