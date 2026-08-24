using System;
using System.Collections.Generic;

namespace PosCs.Domain.Entities
{
    /// <summary>Printer & receipt configuration (plan Phase 12, spec §28). Persisted as
    /// key/value rows; missing keys fall back to <see cref="Defaults"/>.</summary>
    public class PrinterSettings
    {
        public string ReceiptPrinterName { get; set; } = "Xprinter";
        public string LabelPrinterName { get; set; } = "Xprinter";
        /// <summary>58 or 80 (mm) — drives the raster width (384 / 576 dots).</summary>
        public int PaperWidthMm { get; set; } = 80;
        public int Copies { get; set; } = 1;
        public bool AutoCut { get; set; } = true;
        /// <summary>Fire the ESC/POS cash-drawer kick after printing.</summary>
        public bool OpenCashDrawer { get; set; } = false;
        public bool ShowLogo { get; set; } = true;
        public string StoreName { get; set; }
        public string StorePhone { get; set; }
        public string StoreAddress { get; set; }
        /// <summary>Extra centered line under the store info.</summary>
        public string ReceiptHeader { get; set; }
        public string ReceiptFooter { get; set; } = "شكراً لزيارتكم!";

        /// <summary>Raster pixel width for the receipt image.</summary>
        public int RasterWidth => PaperWidthMm <= 58 ? 384 : 576;

        public static PrinterSettings Defaults() => new PrinterSettings();

        public void Normalize()
        {
            if (string.IsNullOrWhiteSpace(ReceiptPrinterName)) ReceiptPrinterName = "Xprinter";
            if (string.IsNullOrWhiteSpace(LabelPrinterName)) LabelPrinterName = "Xprinter";
            if (PaperWidthMm != 58 && PaperWidthMm != 80) PaperWidthMm = 80;
            if (Copies < 1) Copies = 1;
            if (Copies > 5) Copies = 5;
            if (string.IsNullOrWhiteSpace(ReceiptFooter)) ReceiptFooter = "شكراً لزيارتكم!";
            StoreName = StoreName?.Trim();
            StorePhone = StorePhone?.Trim();
            StoreAddress = StoreAddress?.Trim();
            ReceiptHeader = ReceiptHeader?.Trim();
            ReceiptFooter = ReceiptFooter?.Trim();
        }

        /// <summary>Key/value projection used by persistence and the API.</summary>
        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                ["receiptPrinterName"] = ReceiptPrinterName,
                ["labelPrinterName"] = LabelPrinterName,
                ["paperWidthMm"] = PaperWidthMm.ToString(),
                ["copies"] = Copies.ToString(),
                ["autoCut"] = AutoCut ? "1" : "0",
                ["openCashDrawer"] = OpenCashDrawer ? "1" : "0",
                ["showLogo"] = ShowLogo ? "1" : "0",
                ["storeName"] = StoreName ?? "",
                ["storePhone"] = StorePhone ?? "",
                ["storeAddress"] = StoreAddress ?? "",
                ["receiptHeader"] = ReceiptHeader ?? "",
                ["receiptFooter"] = ReceiptFooter ?? ""
            };
        }

        public static PrinterSettings FromDictionary(IReadOnlyDictionary<string, string> map)
        {
            var s = Defaults();
            if (map == null) return s;

            string Get(string key) => map.TryGetValue(key, out var v) ? v : null;

            s.ReceiptPrinterName = Get("receiptPrinterName") ?? s.ReceiptPrinterName;
            s.LabelPrinterName = Get("labelPrinterName") ?? s.LabelPrinterName;
            if (int.TryParse(Get("paperWidthMm"), out var w)) s.PaperWidthMm = w;
            if (int.TryParse(Get("copies"), out var c)) s.Copies = c;
            if (bool.TryParse(Get("autoCut"), out var cut)) s.AutoCut = cut;
            else if (Get("autoCut") == "0") s.AutoCut = false;
            else if (Get("autoCut") == "1") s.AutoCut = true;
            if (bool.TryParse(Get("openCashDrawer"), out var drawer)) s.OpenCashDrawer = drawer;
            else if (Get("openCashDrawer") == "0") s.OpenCashDrawer = false;
            else if (Get("openCashDrawer") == "1") s.OpenCashDrawer = true;
            if (bool.TryParse(Get("showLogo"), out var logo)) s.ShowLogo = logo;
            else if (Get("showLogo") == "0") s.ShowLogo = false;
            else if (Get("showLogo") == "1") s.ShowLogo = true;
            s.StoreName = NullIfEmpty(Get("storeName"));
            s.StorePhone = NullIfEmpty(Get("storePhone"));
            s.StoreAddress = NullIfEmpty(Get("storeAddress"));
            s.ReceiptHeader = NullIfEmpty(Get("receiptHeader"));
            s.ReceiptFooter = NullIfEmpty(Get("receiptFooter")) ?? s.ReceiptFooter;
            return s;
        }

        private static string NullIfEmpty(string value) =>
            string.IsNullOrEmpty(value) ? null : value;
    }
}
