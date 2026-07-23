# Printing Feature — Complete Technical Analysis

> **Project:** POS Application (V8 — C# Backend + Next.js)
> **Backend:** .NET Framework 4.8 OWIN self-host (port 3001)
> **Date:** July 2026

---

## Table of Contents

1. [Architecture](#1-architecture)
2. [Printing Components](#2-printing-components)
3. [Receipt Generation](#3-receipt-generation)
4. [Printer Communication](#4-printer-communication)
5. [Native Windows APIs](#5-native-windows-apis)
6. [Printer Configuration](#6-printer-configuration)
7. [ESC/POS Commands](#7-escpos-commands)
8. [Receipt Types](#8-receipt-types)
9. [Error Handling](#9-error-handling)
10. [Dependencies](#10-dependencies)
11. [External Libraries](#11-external-libraries)
12. [Printing Flow Diagram](#12-printing-flow-diagram)
13. [Important Files](#13-important-files)
14. [Possible Weaknesses](#14-possible-weaknesses)
15. [Final Summary](#15-final-summary)

---

## 1. Architecture

This project has **two independent printing pipelines** that share the same low-level transport layer.

### Pipeline A — Receipt Printing (Image Mode)

```
Frontend (POS checkout)
   │  POST /api/printing/print
   ▼
PrintingController.Print()
   │
   ├─► ReceiptBuilder          (GDI+ bitmap rendering, Arabic BiDi)
   │
   ├─► ImagePrinter            (Bitmap → ESC/POS raster bytes)
   │
   └─► PrinterService          (Win32 winspool.drv raw byte transport)
         │
         ▼
      Physical Printer
```

### Pipeline B — Barcode Label Printing (Text/ESC/POS Mode)

```
Frontend (Products page)
   │  POST /api/printing/print-barcode
   ▼
PrintingController.PrintBarcode()
   │
   ├─► ReceiptService.BuildEscPosBarcode()   (ESC/POS byte generation)
   │
   └─► PrinterService                        (Win32 winspool.drv raw byte transport)
         │
         ▼
      Physical Printer
```

### Trigger Points (Frontend)

| Trigger | File | Line | Action |
|---------|------|------|--------|
| POS Checkout "Save and Print" (F12) | `src/app/[locale]/pos/pos-client.tsx` | 407 | Calls `createInvoice(cartItems, discount)` with `printInvoice=true` |
| POS Checkout "Save Only" (F11) | `src/app/[locale]/pos/pos-client.tsx` | 399 | Calls `createInvoice(cartItems, discount, false)` — no print |
| Invoice creation action | `src/features/invoices/actions.ts:12-33` | 25-30 | After `api.invoices.create()`, calls `api.printing.print(invoice)` if `printInvoice=true` |
| Barcode print button | `src/app/[locale]/products/products-client.tsx` | — | Opens dialog → `POST /api/printing/print-barcode` via `fetch()` |

---

## 2. Printing Components

### 2.1 `PrinterService` — Low-Level Printer Transport

| Property | Detail |
|----------|--------|
| **File** | `backend-cs/Services/PrinterService.cs` (97 lines) |
| **Purpose** | Sends raw byte arrays to a named Windows printer via Win32 P/Invoke |
| **Interface** | `IPrinterService` — `bool PrintBytes(string printerName, byte[] bytes, string documentName = "POS Receipt")` |
| **Dependencies** | `winspool.drv` (7 P/Invoke functions), `System.Runtime.InteropServices` |
| **Called by** | `PrintingController.Print()`, `PrintingController.PrintBarcode()` |

**Key Method — `PrintBytes()`:**

1. Calls `OpenPrinter()` to get a handle to the named printer
2. Creates `DOCINFO` struct with `pDataType = "RAW"` (direct byte passthrough)
3. Calls `StartDocPrinter()` to begin a print document
4. Calls `StartPagePrinter()` to begin a page
5. Allocates unmanaged memory via `Marshal.AllocHGlobal()`
6. Copies managed byte array to unmanaged memory via `Marshal.Copy()`
7. Calls `WritePrinter()` to send bytes to the printer
8. Frees unmanaged memory via `Marshal.FreeHGlobal()`
9. Calls `EndPagePrinter()` and `EndDocPrinter()`
10. Calls `ClosePrinter()` to release the handle

All cleanup is wrapped in `try/finally` blocks.

---

### 2.2 `ReceiptService` — Receipt Text/ESC/POS Generator

| Property | Detail |
|----------|--------|
| **File** | `backend-cs/Services/ReceiptService.cs` (253 lines) |
| **Purpose** | Builds receipt content as plain text or ESC/POS binary sequences |
| **Dependencies** | None (standalone) |
| **Called by** | `PrintingController.PrintBarcode()` (only `BuildEscPosBarcode()` is actively used) |

**Public Methods:**

| Method | Signature | Description | Status |
|--------|-----------|-------------|--------|
| `BuildReceiptText()` | `string BuildReceiptText(List<ReceiptItem>, double, double, bool)` | Dispatches to Arabic or English plain-text formatter | **Unused** |
| `FormatEnglishReceipt()` | `string FormatEnglishReceipt(List<ReceiptItem>, double, double)` | 42-char wide English plain-text receipt | **Unused** |
| `FormatArabicReceipt()` | `string FormatArabicReceipt(List<ReceiptItem>, double, double)` | 42-char wide Arabic plain-text receipt | **Unused** |
| `BuildEscPosReceipt()` | `string BuildEscPosReceipt(List<ReceiptItem>, double, double, string, DateTime?)` | Full ESC/POS binary receipt with English text | **Unused** |
| `BuildEscPosBarcode()` | `string BuildEscPosBarcode(string, string, double?, int)` | ESC/POS barcode label with Code128, product name, price, multi-copy | **Active** |

**Private Helpers:**

| Method | Purpose |
|--------|---------|
| `FormatTableRow(string, string, string)` | Formats a 32-char table row with left/center/right columns |
| `FormatAmountLine(string, double)` | Formats label + `$amount` in 32 chars |
| `AddLine(List<byte>, string)` | Appends ASCII text + newline to byte list |

**DTO Defined Here:** `ReceiptItem` — `Name` (string), `Quantity` (int), `SalePrice` (double)

---

### 2.3 `ReceiptBuilder` — GDI+ Image-Based Receipt Renderer

| Property | Detail |
|----------|--------|
| **File** | `backend-cs/Builders/ReceiptBuilder.cs` (217 lines) |
| **Purpose** | Renders a receipt as a `System.Drawing.Bitmap` using GDI+ with native Arabic BiDi text shaping |
| **Implements** | `IDisposable` |
| **Dependencies** | `System.Drawing` (Bitmap, Graphics, Font, StringFormat, Brushes, Pens), `PosCs.Formatters.TextFormatter` |
| **Called by** | `PrintingController.Print()` |

**Constructor:** `ReceiptBuilder(int width = 576)` — Creates a 576×4000 Bitmap canvas, initializes 5 Tahoma fonts (16/18/20/28pt), 3 StringFormat instances (RTL, Center, LTR).

**Public Methods:**

| Method | Description |
|--------|-------------|
| `AddLogo(string logoPath)` | Draws JPEG logo centered at 60% paper width |
| `AddHeader(ReceiptInvoiceModel)` | Draws Arabic invoice number "رقم الفاتورة" and date |
| `AddItems(IEnumerable<ReceiptItemModel>)` | Draws each item with RTL Arabic name on right + LTR price on left |
| `AddTotals(ReceiptInvoiceModel)` | Draws subtotal, discount (if > 0), and bold/large final total |
| `AddFooter()` | Draws "شكراً لزيارتكم!" and "Software by brazilyy" |
| `GetFinishedReceipt()` | Crops bitmap to actual used height and returns final image |
| `Dispose()` | Disposes all GDI+ resources (bitmap, graphics, fonts, formats) |

**Private Helpers:**

| Method | Description |
|--------|-------------|
| `DrawItemLine(string rightRtlText, string leftLtrText, Font)` | Two-column rendering: RTL Arabic on right, LTR numbers on left |
| `DrawStringCenter(string text, Font)` | Centered text rendering |
| `DrawLine()` | Horizontal separator line at current Y position |

**DTOs Defined Here:**
- `ReceiptItemModel` — `Name`, `Quantity`, `SalePrice`
- `ReceiptInvoiceModel` — `Id`, `CreatedAt`, `InvoiceNumber`, `Discount`, `TotalAmount`

---

### 2.4 `ImagePrinter` — Bitmap to ESC/POS Raster Converter

| Property | Detail |
|----------|--------|
| **File** | `backend-cs/Printers/ImagePrinter.cs` (54 lines) |
| **Purpose** | Converts a GDI+ `Bitmap` into ESC/POS raster image byte data (`GS v 0`) |
| **Method** | `static byte[] GetImageBytes(Bitmap bitmap, int idealWidth = 576)` |
| **Dependencies** | `System.Drawing.Bitmap` |
| **Called by** | `PrintingController.Print()` |

**Conversion Process:**

1. Resizes bitmap to 576px width (maintaining aspect ratio)
2. For each row of pixels:
   - Groups 8 pixels into one byte (MSB = leftmost pixel)
   - Converts each pixel to monochrome: `luminance = R*0.3 + G*0.59 + B*0.11`
   - If `luminance < 128` and `alpha > 128` → pixel is black (bit = 1)
3. Prepends `GS v 0` ESC/POS command header with width/height dimensions (little-endian 16-bit)

---

### 2.5 `BarcodePrinter` — ESC/POS Barcode Byte Generator

| Property | Detail |
|----------|--------|
| **File** | `backend-cs/Printers/BarcodePrinter.cs` (31 lines) |
| **Purpose** | Generates ESC/POS byte arrays for Code128 barcode printing |
| **Method** | `static byte[] GetBarcodeBytes(string barcode)` |
| **Dependencies** | None |
| **Called by** | **Nothing** — appears to be dead code, superseded by `ReceiptService.BuildEscPosBarcode()` |

---

### 2.6 `TextFormatter` — Currency and Text Formatting

| Property | Detail |
|----------|--------|
| **File** | `backend-cs/Formatters/TextFormatter.cs` (24 lines) |
| **Purpose** | Formats currency values with Egyptian pound symbol and aligns item lines |
| **Dependencies** | None |
| **Called by** | `ReceiptBuilder.AddTotals()` |

**Methods:**

| Method | Returns |
|--------|---------|
| `FormatCurrency(double amount)` | `"{amount:F2} ج.م"` (Egyptian pounds, 2 decimal places) |
| `FormatItemLine(string itemName, string priceText, int totalWidth = 42)` | Item name + padding + price text, padded to fixed width |

---

## 3. Receipt Generation

### 3.1 Image-Mode Receipt (Primary — `ReceiptBuilder`)

| Section | Method | Rendering Details |
|---------|--------|-------------------|
| **Logo** | `AddLogo()` | JPEG loaded from disk, resized to 60% paper width via `Graphics.DrawImage()`, centered horizontally |
| **Header** | `AddHeader()` | Centered Arabic text: "رقم الفاتورة: #{number}" (18pt bold Tahoma) and "التاريخ: {date}" (16pt regular Tahoma) with horizontal separator lines |
| **Items** | `AddItems()` | Each item rendered via `DrawItemLine()`: RTL Arabic name on right side, LTR numeric price on left side. Item format: `"{name} : {quantity}"` |
| **Totals** | `AddTotals()` | If discount > 0: "الإجمالي قبل الخصم:" + "الخصم:" (16pt regular). Then "الإجمالي النهائي:" (20pt bold). All formatted as `"{amount:F2} ج.م"` |
| **Footer** | `AddFooter()` | "شكراً لزيارتكم!" (16pt bold) + "Software by brazilyy" (16pt regular), centered. 40px extra padding at bottom for tearing. |

### 3.2 RTL/Arabic Support

Handled natively by GDI+ `StringFormat` with `StringFormatFlags.DirectionRightToLeft`.

From `PrintingController.cs:41-43`:
> "We no longer use IBM864 or manual shaping because they are unreliable.
> We render the entire receipt as an image using System.Drawing (via ReceiptBuilder)
> which flawlessly supports Arabic shaping and BiDi out of the box via Windows GDI+."

**Three StringFormat instances in ReceiptBuilder:**

| Format | Flags | Alignment | Usage |
|--------|-------|-----------|-------|
| `_rtlFormat` | `DirectionRightToLeft` | `StringAlignment.Near` | Arabic text (item names, labels) |
| `_ltrFormat` | (none — default LTR) | `StringAlignment.Near` | Numeric values (prices, quantities) |
| `_centerFormat` | (none) | `StringAlignment.Center` | Headers, footer, separators |

### 3.3 Alignment and Column Layout

**`DrawItemLine()` (ReceiptBuilder:156-176):**

Both RTL and LTR strings are drawn across the full `printableWidth` (paper width minus 15px margin on each side = 546px). RTL renders right-aligned via `_rtlFormat`, LTR renders left-aligned via `_ltrFormat`. Both occupy the same vertical space, creating a visual two-column layout where Arabic text appears on the right and prices on the left.

**`FormatTableRow()` (ReceiptService:167-178) — for text mode:**

Fixed 32-character total width. Left column: 16 chars. Center column: 6 chars. Right column: 10 chars. Uses `PadRight()` and `PadLeft()` for alignment.

### 3.4 Separators

**Image mode:** `DrawLine()` draws a horizontal black line from x=15 to x=(width-15) at the current Y position, with 10px spacing above and 15px below.

**Text mode:** `new string('-', 40)` or `new string('=', 40)` (English) / 32 chars (ESC/POS binary).

### 3.5 Totals Formatting

- Currency formatted via `TextFormatter.FormatCurrency()`: `"{amount:F2} ج.م"`
- In text-mode ESC/POS: `"$" + amount.ToString("F2")` (dollar sign, Egyptian pounds not used)
- Discount calculated as: `totalBeforeDiscount = invoice.TotalAmount + invoice.Discount`
- Final total in image mode rendered with `_totalFont` (20pt bold Tahoma, vs 16pt for other text)

### 3.6 Line Wrapping

**Image mode:** Not implemented. Items are rendered at their full length without truncation. Long Arabic item names will extend beyond the printable area.

**Text mode (receipt):** Item names truncated to 18 characters (`item.Name.Substring(0, Math.Min(item.Name.Length, 18))`).

**Text mode (barcode):** Product names truncated to 20 characters.

### 3.7 Paper Size

Default: **576 pixels** wide — standard for **80mm thermal printers**. The bitmap canvas is initially 4000px tall and cropped to actual used height by `GetFinishedReceipt()`.

---

## 4. Printer Communication

The application sends data to the printer using **Win32 raw printing** via the `winspool.drv` DLL. This is a direct P/Invoke approach that bypasses the Windows print spooler GDI transforms.

**Data Type:** `"RAW"` — specified in `DOCINFO.pDataType` at `PrinterService.cs:55`. The byte data is sent directly to the printer driver without interpretation, which is essential for ESC/POS commands that the printer firmware interprets directly.

**Memory Management:**
1. `Marshal.AllocHGlobal(bytes.Length)` — allocates unmanaged memory
2. `Marshal.Copy(bytes, 0, pBytes, bytes.Length)` — copies managed byte array to unmanaged memory
3. `WritePrinter(hPrinter, pBytes, bytes.Length, out int written)` — sends data to printer
4. `Marshal.FreeHGlobal(pBytes)` — releases unmanaged memory

All wrapped in `try/finally` to ensure cleanup.

---

## 5. Native Windows APIs

All P/Invoke declarations are in `backend-cs/Services/PrinterService.cs:13-32`.

**DLL:** `winspool.drv`

| Function | Line | Purpose |
|----------|------|---------|
| `OpenPrinter` | 13-14 | Opens a handle to a named printer. Required before any printing operation. Returns printer handle via `out IntPtr phPrinter`. |
| `ClosePrinter` | 16-17 | Closes the printer handle, releasing the system resource. Always called in `finally` block. |
| `StartDocPrinter` | 19-20 | Begins a new print document. The `DOCINFO` struct specifies document name ("POS Receipt") and data type ("RAW"). Level 1 = DOCINFO struct. |
| `EndDocPrinter` | 22-23 | Ends the current print document. |
| `StartPagePrinter` | 25-26 | Begins a new page in the current document. Required before `WritePrinter`. |
| `EndPagePrinter` | 28-29 | Ends the current page. |
| `WritePrinter` | 31-32 | Writes raw bytes to the printer. Reports bytes written via `out int dwWritten`. |

**Struct — `DOCINFO` (lines 34-40):**

```csharp
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
private struct DOCINFO
{
    [MarshalAs(UnmanagedType.LPStr)] public string pDocName;   // "POS Receipt"
    [MarshalAs(UnmanagedType.LPStr)] public string pOutputFile; // null
    [MarshalAs(UnmanagedType.LPStr)] public string pDataType;   // "RAW"
}
```

---

## 6. Printer Configuration

| Setting | Source | Default Value | Location |
|---------|--------|---------------|----------|
| **Printer name** | `Environment.GetEnvironmentVariable("PRINTER_NAME")` | `"Xprinter"` | `PrintingController.cs:18` |
| **Paper width (image)** | `ReceiptBuilder` constructor parameter | `576` dots (80mm) | `ReceiptBuilder.cs:42` |
| **Paper width (raster)** | `ImagePrinter.GetImageBytes()` parameter | `576` dots | `ImagePrinter.cs:9` |
| **Logo path** | `Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "logo.jpeg")` | Hardcoded | `PrintingController.cs:21,48` |
| **Font family** | `ReceiptBuilder` constructor | `Tahoma` | `ReceiptBuilder.cs:51-55` |
| **Font sizes** | `ReceiptBuilder` constructor | 16pt (regular), 18pt (header), 20pt (total), 28pt (store name) | `ReceiptBuilder.cs:51-55` |
| **Character encoding** | `Encoding.ASCII` | ASCII | `ReceiptService.cs:164,192,224`, `PrintingController.cs:123` |
| **Code page** | Not explicitly set | System default | — |
| **DPI** | Not configured | System default (~96 DPI screen, printer physical DPI) | — |
| **Text receipt line width** | `ReceiptService.LineWidth` constant | 42 chars (text mode), 32 chars (ESC/POS binary) | `ReceiptService.cs:10` |
| **Monochrome threshold** | `ImagePrinter.GetImageBytes()` | Luminance < 128, alpha > 128 | `ImagePrinter.cs:39-42` |
| **Logo width ratio** | `ReceiptBuilder.AddLogo()` | 60% of paper width | `ReceiptBuilder.cs:85` |
| **Barcode height** | `BarcodePrinter` / `ReceiptService` | 80 dots | `BarcodePrinter.cs:15` |
| **Barcode width module** | `BarcodePrinter` | 3 | `BarcodePrinter.cs:18` |
| **Barcode HRI position** | `BarcodePrinter` | Below barcode (2) | `BarcodePrinter.cs:21` |

**No database-stored printer settings exist.** There is no `appsettings.json`, no XML config file, and no database table for printer settings. Everything is environment variables or hardcoded constants.

---

## 7. ESC/POS Commands

The project uses extensive ESC/POS commands across three source files.

### 7.1 Printer Lifecycle Commands

| Hex | Command | Name | Used In | Line(s) | Purpose |
|-----|---------|------|---------|---------|---------|
| `0x1B 0x40` | `ESC @` | Initialize/Reset | `ReceiptService.cs` | 94 | Resets the printer to default state |
| `0x1B 0x40` | `ESC @` | Initialize/Reset | `PrintingController.cs` | 76 | Resets printer before image data |

### 7.2 Text Formatting Commands

| Hex | Command | Name | Used In | Line(s) | Purpose |
|-----|---------|------|---------|---------|---------|
| `0x1B 0x45 0x01` | `ESC E 1` | Bold ON | `ReceiptService.cs` | 100, 146, 212 | Enables bold text |
| `0x1B 0x45 0x00` | `ESC E 0` | Bold OFF | `ReceiptService.cs` | 109, 150, 217 | Disables bold text |
| `0x1D 0x21 0x11` | `GS ! 0x11` | Double Size | `ReceiptService.cs` | 103, 147, 213 | Double-height + double-width text |
| `0x1D 0x21 0x22` | `GS ! 0x22` | Double Size (price) | `ReceiptService.cs` | 234 | Double-height + double-width for price display |
| `0x1D 0x21 0x00` | `GS ! 0x00` | Normal Size | `ReceiptService.cs` | 108, 149, 216, 236 | Resets to normal text size |

### 7.3 Alignment Commands

| Hex | Command | Name | Used In | Line(s) | Purpose |
|-----|---------|------|---------|---------|---------|
| `0x1B 0x61 0x00` | `ESC a 0` | Left Align | `ReceiptService.cs` | 115 | Left-aligns text |
| `0x1B 0x61 0x01` | `ESC a 1` | Center Align | `ReceiptService.cs` | 97, 153, 207 | Center-aligns text |
| `0x1B 0x61 0x01` | `ESC a 1` | Center Align | `BarcodePrinter.cs` | 12 | Center-aligns barcode |

### 7.4 Paper Control Commands

| Hex | Command | Name | Used In | Line(s) | Purpose |
|-----|---------|------|---------|---------|---------|
| `0x1B 0x6D` | `ESC m` | Partial Cut | `ReceiptService.cs` | 160, 240 | Partial paper cut |
| `0x1B 0x64 0x05` | `ESC d 5` | Feed 5 Lines | `ReceiptService.cs` | 162 | Feeds paper 5 lines after cut |
| `0x1D 0x56 0x42 0x00` | `GS V B 0` | Full Cut | `PrintingController.cs` | 82 | Full paper cut after image receipt |

### 7.5 Barcode Commands

| Hex | Command | Name | Used In | Line(s) | Purpose |
|-----|---------|------|---------|---------|---------|
| `0x1D 0x6B 0x49` | `GS k I` | Code128 Barcode | `ReceiptService.cs` | 223 | Prints Code128 barcode (function I) |
| `0x1D 0x6B 0x49` | `GS k I` | Code128 Barcode | `BarcodePrinter.cs` | 24 | Prints Code128 barcode (function I) |
| `0x7B 0x42` | `{B` | Start Code B | `ReceiptService.cs` | 227-228 | Code128 subset B prefix |
| `0x1D 0x68 0x50` | `GS h 80` | Barcode Height | `BarcodePrinter.cs` | 15 | Sets barcode height to 80 dots |
| `0x1D 0x77 0x03` | `GS w 3` | Barcode Width | `BarcodePrinter.cs` | 18 | Sets barcode module width to 3 |
| `0x1D 0x48 0x02` | `GS H 2` | HRI Position | `BarcodePrinter.cs` | 21 | Prints human-readable text below barcode |

### 7.6 Image Commands

| Hex | Command | Name | Used In | Line(s) | Purpose |
|-----|---------|------|---------|---------|---------|
| `0x1D 0x76 0x30 0x00` | `GS v 0` | Raster Image | `ImagePrinter.cs` | 22 | Prints raster bitmap image (mode 0 = normal) |

**Image data format after `GS v 0`:**
- `byteWidth % 256` (low byte of width in bytes)
- `byteWidth / 256` (high byte of width in bytes)
- `height % 256` (low byte of height in pixels)
- `height / 256` (high byte of height in pixels)
- Raw bitmap data (1 bit per pixel, MSB first, 8 pixels per byte)

---

## 8. Receipt Types

| Type | Status | Endpoint | Controller Method | Description |
|------|--------|----------|-------------------|-------------|
| **Customer Receipt (Image)** | **Active** | `POST /api/printing/print` | `PrintingController.Print()` | Full Arabic receipt rendered as GDI+ bitmap → ESC/POS raster. Includes logo, invoice number, date, items with Arabic names, totals in Egyptian pounds, and "thank you" footer. |
| **Barcode Label** | **Active** | `POST /api/printing/print-barcode` | `PrintingController.PrintBarcode()` | ESC/POS text-mode barcode label. Includes product name (bold/double), Code128 barcode, optional price (double-size). Supports multi-copy printing via `count` parameter. |
| **Text Receipt (English)** | **Unused** | N/A | `ReceiptService.FormatEnglishReceipt()` | Plain-text English receipt at 42 chars wide. Not called from any endpoint. |
| **Text Receipt (Arabic)** | **Unused** | N/A | `ReceiptService.FormatArabicReceipt()` | Plain-text Arabic receipt at 42 chars wide. Not called from any endpoint. |
| **ESC/POS Receipt** | **Unused** | N/A | `ReceiptService.BuildEscPosReceipt()` | ESC/POS binary receipt with English text, bold headers, totals. Not called from any endpoint. |
| **Text Receipt (stub)** | **Stub** | `POST /api/printing/receipt` | `PrintingController.ReceiptText()` | Returns 200 OK with no body. No implementation. |
| **Barcode Text (stub)** | **Stub** | `POST /api/printing/barcode` | `PrintingController.BarcodeText()` | Returns 200 OK with no body. No implementation. |

**Not implemented:** Kitchen Receipt, Delivery Receipt, Hold Receipt, Invoice (printed), Refund Receipt.

---

## 9. Error Handling

### 9.1 `PrinterService.PrintBytes()`

| Failure | Response | Logging |
|---------|----------|---------|
| `OpenPrinter()` fails | Returns `false` | `[PRINT] Failed to open printer '{name}'` to stderr |
| `StartDocPrinter()` fails | Returns `false` | `[PRINT] StartDocPrinter failed. Win32 Error = {error}` to stderr |
| `StartPagePrinter()` fails | Returns `false` | `[PRINT] StartPagePrinter failed. Win32 Error = {error}` to stderr |
| `WritePrinter()` fails | Returns `false` | `[PRINT] WritePrinter failed` to stderr |
| `EndPagePrinter()` fails | **Return value not checked** | None |
| `EndDocPrinter()` fails | **Return value not checked** | None |

**Cleanup:** `ClosePrinter()` is always called in `finally`. `Marshal.FreeHGlobal()` is always called in nested `finally`.

### 9.2 `PrintingController.Print()`

| Failure | HTTP Response | Body |
|---------|---------------|------|
| Null/missing DTO | 400 Bad Request | `{ success: false, message: "Missing invoice data" }` |
| `PrinterService` returns `false` | 500 Internal Server Error | `{ success: false, message: "Failed to send data to the printer." }` |
| Any exception | 500 Internal Server Error | `{ success: false, message: "Print failed", detail: ex.Message }` |

Logging: `Console.Error.WriteLine($"[API ERR] Print receipt failed: {ex}")`

### 9.3 `PrintingController.PrintBarcode()`

| Failure | HTTP Response | Body |
|---------|---------------|------|
| Missing barcode | 400 Bad Request | `{ success: false, message: "Missing barcode" }` |
| `PrinterService` returns `false` | **200 OK** (inconsistent) | `{ success: false, message: "Barcode print failed: could not open printer '{name}'" }` |
| Any exception | 500 Internal Server Error | `{ success: false, message: "Barcode print failed", detail: ex.Message }` |

### 9.4 Frontend Error Handling

In `src/features/invoices/actions.ts:28-30`:
```typescript
try {
    await api.printing.print(invoice)
} catch (e) {
    console.error("Silent printing failed, printer API might be offline")
}
```

Print errors are **silently caught**. The user is not notified of print failure during checkout.

### 9.5 Issues

- `EndPagePrinter()` and `EndDocPrinter()` return values are not checked
- No retry mechanism anywhere in the stack
- `PrintBarcode()` returns HTTP 200 even on failure
- Frontend silently swallows print errors
- No logging framework — all output via `Console.WriteLine`/`Console.Error.WriteLine`
- No structured logging (no log levels, no timestamps in output, no file output)

---

## 10. Dependencies

### NuGet Packages (from `pos-cs.csproj`)

| Package | Version | Printing Relevance |
|---------|---------|-------------------|
| `Microsoft.AspNet.WebApi.OwinSelfHost` | 5.3.0 | Hosts the Web API that exposes printing endpoints |
| `Microsoft.Data.Sqlite` | 9.0.3 | None directly — database access for invoice/product data |
| `Microsoft.Owin.Cors` | 4.2.2 | Enables CORS for frontend to call printing endpoints |
| `Dapper` | 2.1.35 | None directly — ORM for loading invoice data from database |
| `Microsoft.Owin.StaticFiles` | 4.2.2 | Serves `logo.jpeg` from `wwwroot/` |
| `Newtonsoft.Json` | 13.0.3 | Serializes/deserializes printing DTOs in API requests |

### Transitive Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `SQLitePCLRaw.lib.e_sqlite3` | 2.1.10 | Native SQLite binary (copied to output via build target) |

### Framework References

| Reference | Printing Relevance |
|-----------|-------------------|
| `System.Drawing` | Core — `ReceiptBuilder` uses Bitmap, Graphics, Font, StringFormat |
| `System.Runtime.InteropServices` | Core — `PrinterService` uses Marshal, DllImport, IntPtr |
| `System.Management` | None — only used for WMI hardware ID in licensing |

**No dedicated printing NuGet packages are used.** The entire printing stack is built on native Win32 APIs and `System.Drawing`.

---

## 11. External Libraries

| Library | Used? | Details |
|---------|-------|---------|
| **`winspool.drv` P/Invoke** | **Yes** | `PrinterService.cs` — 7 Win32 functions imported directly |
| **`System.Drawing` GDI+** | **Yes** | `ReceiptBuilder.cs`, `ImagePrinter.cs` — Bitmap rendering, pixel manipulation, font rendering |
| `RawPrinterHelper` | No | Custom implementation exists via `PrinterService` |
| `ESCPOS.NET` | No | ESC/POS commands are hand-built as byte arrays |
| `Microsoft Print APIs` | No | |
| `PdfSharp` | No | |
| `QuestPDF` | No | |
| `System.Drawing.Printing` | No | Only `System.Drawing` core is used, not the `Printing` namespace |
| `Windows Forms Printing` | No | |
| `WPF Printing` | No | |

---

## 12. Printing Flow Diagram

### Pipeline A: Receipt Printing (Image Mode)

```
  Browser (POS Client)
      │
      │  POST /api/invoices       ← create invoice in database
      │  POST /api/printing/print  ← send invoice data for printing
      │
      ▼
  PrintingController.Print(PrintDto)
      │
      │  dto.Invoice
      │
      ├── ExtractItems(invoice)               ← normalizes item data from DTO
      │      (handles both Items and InvoiceDetail paths)
      │
      ├── new ReceiptBuilder(576)             ← creates 576×4000 Bitmap canvas
      │     │
      │     │  Set up GDI+:
      │     │  - Graphics from Bitmap
      │     │  - TextRenderingHint = SingleBitPerPixelGridFit
      │     │  - 5 Tahoma fonts (16/18/20/28pt)
      │     │  - 3 StringFormat (RTL, Center, LTR)
      │     │
      │     ├── AddLogo("wwwroot/logo.jpeg")
      │     │     └── DrawImage at 60% width, centered
      │     │
      │     ├── AddHeader({ Id, CreatedAt, InvoiceNumber })
      │     │     ├── DrawLine()
      │     │     ├── DrawStringCenter("رقم الفاتورة: #N", 18pt bold)
      │     │     ├── DrawStringCenter("التاريخ: YYYY-MM-DD HH:mm", 16pt)
      │     │     └── DrawLine()
      │     │
      │     ├── AddItems([{ Name, Quantity, SalePrice }])
      │     │     └── For each item:
      │     │           ├── Calculate itemTotal = Quantity × SalePrice
      │     │           ├── DrawItemLine("{Name} : {Qty}", "{Total:F2}", 16pt)
      │     │           │     ├── MeasureString RTL for Arabic name
      │     │           │     ├── MeasureString LTR for price
      │     │           │     ├── DrawString RTL (right-aligned Arabic)
      │     │           │     └── DrawString LTR (left-aligned numbers)
      │     │           └── _currentY += max height + 5px
      │     │     └── DrawLine()
      │     │
      │     ├── AddTotals({ Discount, TotalAmount })
      │     │     ├── DrawLine()
      │     │     ├── If discount > 0:
      │     │     │     ├── DrawItemLine("الإجمالي قبل الخصم:", "X ج.م", 16pt)
      │     │     │     ├── DrawItemLine("الخصم:", "X ج.م", 16pt)
      │     │     │     └── DrawLine()
      │     │     ├── DrawItemLine("الإجمالي النهائي:", "X ج.م", 20pt BOLD)
      │     │     └── DrawLine()
      │     │
      │     ├── AddFooter()
      │     │     ├── DrawStringCenter("شكراً لزيارتكم!", 16pt bold)
      │     │     ├── DrawStringCenter("Software by brazilyy", 16pt)
      │     │     └── _currentY += 40px (tearing space)
      │     │
      │     └── GetFinishedReceipt()
      │           └── Crop bitmap from 576×4000 to 576×_currentY
      │           └── Return final Bitmap
      │
      ├── ImagePrinter.GetImageBytes(finalReceipt)
      │     │
      │     ├── Resize bitmap to 576×(calculated height)
      │     ├── Calculate byteWidth = 576 / 8 = 72 bytes per row
      │     ├── Build GS v 0 header:
      │     │     [0x1D, 0x76, 0x30, 0x00, 72, 0, height_low, height_high]
      │     ├── For each pixel row:
      │     │     For each byte (8 pixels):
      │     │       luminance = R×0.3 + G×0.59 + B×0.11
      │     │       if luminance < 128 && alpha > 128 → set bit
      │     └── Return byte[] (complete ESC/POS image command)
      │
      ├── Assemble final byte payload:
      │     [ESC @]          ← 0x1B 0x40 (initialize printer)
      │     [Image bytes]    ← from ImagePrinter
      │     [GS V B 0]       ← 0x1D 0x56 0x42 0x00 (full cut)
      │
      └── new PrinterService().PrintBytes("Xprinter", bytes)
            │
            ├── OpenPrinter("Xprinter")        ← winspool.drv
            │     └── Get IntPtr hPrinter
            │
            ├── StartDocPrinter(hPrinter, 1, DOCINFO)
            │     └── pDocName = "POS Receipt"
            │     └── pDataType = "RAW"
            │
            ├── StartPagePrinter(hPrinter)     ← winspool.drv
            │
            ├── Marshal.AllocHGlobal(bytes.Length)
            ├── Marshal.Copy(bytes → pBytes)
            ├── WritePrinter(hPrinter, pBytes, bytes.Length)
            │     └── Raw bytes sent to printer driver
            ├── Marshal.FreeHGlobal(pBytes)
            │
            ├── EndPagePrinter(hPrinter)       ← winspool.drv
            ├── EndDocPrinter(hPrinter)        ← winspool.drv
            └── ClosePrinter(hPrinter)         ← winspool.drv
                  │
                  ▼
            Physical Thermal Printer receives ESC/POS commands
            → Initializes → Renders raster image → Cuts paper
```

### Pipeline B: Barcode Label Printing (Text/ESC/POS Mode)

```
  Browser (Products Page)
      │
      │  POST /api/printing/print-barcode
      │  Body: { barcode, name, price, count }
      │
      ▼
  PrintingController.PrintBarcode(PrintBarcodeDto)
      │
      ├── Validate: barcode is not null/empty
      │
      ├── ReceiptService.BuildEscPosBarcode(barcode, name, price, count)
      │     │
      │     │  For each copy (i = 1..count):
      │     │
      │     ├── If i > 0: append "\n" (separator between labels)
      │     │
      │     ├── [0x1B 0x61 0x01]        ← ESC a 1: Center alignment
      │     │
      │     ├── If productName is not empty:
      │     │     ├── [0x1B 0x45 0x01]  ← ESC E 1: Bold ON
      │     │     ├── [0x1D 0x21 0x11]  ← GS ! 0x11: Double size
      │     │     ├── "{name}" (truncated to 20 chars) + "\n"
      │     │     ├── [0x1D 0x21 0x00]  ← GS ! 0x00: Normal size
      │     │     ├── [0x1B 0x45 0x00]  ← ESC E 0: Bold OFF
      │     │     └── "\n"
      │     │
      │     ├── [0x1D 0x6B 0x49]        ← GS k I: Code128 barcode
      │     │     ├── byte length = barcode.Length + 2
      │     │     ├── [0x7B] [0x42]      ← {B: Start Code B (Code128 subset)
      │     │     └── barcode bytes (ASCII)
      │     │
      │     ├── If price has value:
      │     │     ├── "\n\n"
      │     │     ├── [0x1D 0x21 0x22]  ← GS ! 0x22: Double H + Double W
      │     │     ├── "${price:F2}" (ASCII)
      │     │     └── [0x1D 0x21 0x00]  ← GS ! 0x00: Normal size
      │     │
      │     ├── "\n"
      │     └── [0x1B 0x6D]            ← ESC m: Partial cut
      │
      │     Return: string (ASCII-encoded ESC/POS)
      │
      ├── Encoding.ASCII.GetBytes(escPos)
      │
      └── new PrinterService().PrintBytes(PrinterName, asciiBytes)
            │
            └── (Same winspool.drv flow as Pipeline A)
                  │
                  ▼
            Physical Thermal Printer
            → Centers text → Prints bold name → Prints barcode → Prints price → Cuts
```

---

## 13. Important Files

| File | Path | Lines | Purpose | Importance |
|------|------|-------|---------|------------|
| **PrintingController.cs** | `backend-cs/Controllers/PrintingController.cs` | 227 | API endpoints, DTOs, print orchestration | **Critical** |
| **PrinterService.cs** | `backend-cs/Services/PrinterService.cs` | 97 | Win32 P/Invoke raw printer transport | **Critical** |
| **ReceiptBuilder.cs** | `backend-cs/Builders/ReceiptBuilder.cs` | 217 | GDI+ image-based receipt rendering with Arabic BiDi | **Critical** |
| **ImagePrinter.cs** | `backend-cs/Printers/ImagePrinter.cs` | 54 | Bitmap → ESC/POS raster byte conversion | **Critical** |
| **ReceiptService.cs** | `backend-cs/Services/ReceiptService.cs` | 253 | ESC/POS barcode generation + legacy text receipts | **High** |
| **TextFormatter.cs** | `backend-cs/Formatters/TextFormatter.cs` | 24 | Egyptian pound currency formatting | **Medium** |
| **BarcodePrinter.cs** | `backend-cs/Printers/BarcodePrinter.cs` | 31 | ESC/POS barcode byte generation (dead code) | **Low** |
| **logo.jpeg** | `backend-cs/wwwroot/logo.jpeg` | — | Store logo printed on every receipt | **Medium** |
| **api.ts** | `src/lib/api.ts` | 102 | Frontend API client with printing section (lines 89-101) | **High** |
| **actions.ts** | `src/features/invoices/actions.ts` | 34 | Invoice creation + conditional print trigger | **High** |
| **pos-client.tsx** | `src/app/[locale]/pos/pos-client.tsx` | 425 | POS checkout UI with Save & Print / Save Only | **Medium** |
| **products-client.tsx** | `src/app/[locale]/products/products-client.tsx` | 194 | Products page with barcode print dialog | **Medium** |
| **ar.json** | `messages/ar.json` | 123 | Arabic i18n strings for print-related UI | **Low** |

---

## 14. Possible Weaknesses

### 14.1 Code Duplication

- **Barcode generation is duplicated.** `BarcodePrinter.GetBarcodeBytes()` generates ESC/POS barcode bytes, but `ReceiptService.BuildEscPosBarcode()` does the same thing and more. `BarcodePrinter` is never called — it is dead code.
- **DTO triplication.** `ReceiptItem` (in `ReceiptService.cs`), `ReceiptItemModel` (in `ReceiptBuilder.cs`), and `ReceiptItemData` (in `PrintingController.cs`) all have identical fields (`Name`, `Quantity`, `SalePrice`). They are manually mapped between each other in `PrintingController.ExtractItems()`.
- **Logo path duplicated** in `PrintingController.cs:21` and `PrintingController.cs:48`.
- **`PrinterService` instantiated per-request** at `PrintingController.cs:85,123` rather than being injected or reused.

### 14.2 Tight Coupling

- `PrintingController` directly instantiates `new ReceiptBuilder()`, `new PrinterService()`, and calls static methods on `ImagePrinter`. No dependency injection is used despite `IPrinterService` being defined.
- `_receiptService` is an instance field in the controller, but `PrinterService` is created inline — inconsistent pattern.
- Receipt rendering is tightly coupled to `System.Drawing` GDI+, which is Windows-only and not available on .NET Core/5+ without additional packages.

### 14.3 Maintainability Issues

- **No logging framework.** All logging is via `Console.WriteLine`/`Console.Error.WriteLine`. No structured logging, no log levels, no file output, no timestamps.
- **No configuration file.** Printer name, paper width, font sizes, logo path are all hardcoded or environment-variable-only.
- **4 dead endpoints** (`/receipt`, `/barcode` stubs, unused `ReceiptService` text methods) that return empty 200 OK.
- **Two different line widths** for text receipts: 42 chars in `FormatEnglishReceipt()`/`FormatArabicReceipt()` vs 32 chars in `BuildEscPosReceipt()`.
- **Mixed-language comments** — Arabic comments at `PrintingController.cs:116`, `ReceiptBuilder.cs:144`.

### 14.4 Missing Abstractions

- **No `IReceiptBuilder` interface** — `ReceiptBuilder` is a concrete class, impossible to mock or swap for testing.
- **No `IImagePrinter` interface** — `ImagePrinter` uses static methods only.
- **No printer abstraction** — the system is tightly coupled to thermal receipt printers via ESC/POS and `winspool.drv`.
- **No print job abstraction** — no concept of a print job, print queue, or print history.
- **No `ITextFormatter` interface.**
- **No dependency injection container** — all dependencies are manually created.

### 14.5 Thread Safety Concerns

- `PrintingController` creates a new `ReceiptBuilder` and `PrinterService` per request — safe for concurrent requests.
- `ReceiptBuilder` maintains mutable state (`_currentY`, `_bitmap`, `_graphics`) — not thread-safe, but acceptable since it's created per-request.
- `PrinterService` is stateless and thread-safe (all state is on the stack via P/Invoke).
- The `_receiptService` instance field in the controller is shared across requests — but its methods are stateless, so this is safe.
- **No issues identified** with current architecture.

### 14.6 Encoding Risks

- **ASCII-only encoding** in text-mode barcode labels. `ReceiptService.BuildEscPosBarcode()` uses `Encoding.ASCII.GetBytes()` for all text. Arabic characters in product names will be **lost or corrupted** since they cannot be represented in ASCII.
- **Image mode works around this** by rendering Arabic as pixels (GDI+ handles Unicode → glyph mapping), but the text-mode barcode path does not.
- The controller comment at line 41 explicitly says: "We no longer use IBM864 or manual shaping because they are unreliable."
- No `Encoding.GetEncoding(864)` (IBM864 for Arabic) is used anywhere.

### 14.7 Printer Compatibility Issues

- **Hardcoded to `Xprinter`** default name — won't work with other printer brands without setting the `PRINTER_NAME` environment variable.
- **576-pixel width assumption** — works for 80mm printers but not 58mm (typically 384 pixels).
- **`ESC m` for partial cut** — not universally supported; some printers use `ESC V` or `GS V` for cutting.
- **`GS v 0` raster mode** — widely supported but some older printers may not handle large images well (memory limitations). A 576px-wide receipt with many items could produce a very large bitmap.
- **No `GS V 0` (full cut) fallback** — if the printer doesn't support `GS V B 0`, printing will silently fail or produce garbage after the image.
- **`BarcodePrinter` uses `GS h 80`** — 80 dots is quite short for a barcode on many printers.

### 14.8 Performance Concerns

- **`ImagePrinter.GetImageBytes()` uses `GetPixel()`** in a nested loop — this is the slowest way to read pixel data in GDI+. For a 576×N bitmap, this iterates 576×N pixels individually. Using `LockBits()` and direct memory access would be significantly faster.
- **Entire receipt is rendered as a single raster image** — this means the printer's firmware cannot optimize text rendering. A text-based approach would produce smaller data and print faster, but at the cost of Arabic support.
- **`Bitmap` canvas is initially 4000px tall** — allocated for every print request, then cropped. This allocates ~2.3 MB of bitmap memory unnecessarily.

---

## 15. Final Summary

### Architecture

A **dual-pipeline** POS printing system for a .NET Framework 4.8 OWIN self-hosted backend. The primary pipeline renders customer receipts as **GDI+ bitmap images** (solving Arabic BiDi text shaping), converts them to **ESC/POS raster data** (`GS v 0`), and sends them as **raw bytes** to a thermal printer via Win32 P/Invoke (`winspool.drv`). A secondary pipeline generates ESC/POS text-based Code128 barcode labels.

### Technologies

- **Language:** C# on .NET Framework 4.8
- **Web Framework:** ASP.NET Web API via OWIN self-host
- **Image Rendering:** `System.Drawing` (GDI+) with Tahoma font, RTL/LTR mixed-mode rendering
- **Barcode Format:** Code128 (subset B)
- **Database:** SQLite via Dapper (for invoice/product data, not for printing)

### Libraries

- **Zero** external printing NuGet packages
- All printing logic is **hand-written** (~600 lines across 6 backend files)

### Native APIs

- **`winspool.drv`** — 7 P/Invoke functions for raw Windows printer access
- **`System.Drawing`** (GDI+) — Bitmap rendering, font rendering, pixel manipulation

### Data Flow

```
Frontend POST → Controller → ReceiptBuilder (GDI+) → ImagePrinter (raster) → PrinterService (Win32) → Printer
```

### Strengths

| Strength | Detail |
|----------|--------|
| **Arabic text is solved** | GDI+ bitmap approach avoids all BiDi/shaping complexity — Arabic renders perfectly |
| **Clean separation** | Receipt content (ReceiptBuilder), image conversion (ImagePrinter), and transport (PrinterService) are in separate classes |
| **Zero external dependencies** | No printing NuGet packages — reduces supply chain risk |
| **Reliable transport** | Win32 raw printing is the most reliable method for thermal printers on Windows |
| **Proper resource cleanup** | `IDisposable` pattern and `try/finally` blocks ensure GDI+ and P/Invoke resources are released |
| **Interface defined** | `IPrinterService` exists (though not yet used for DI) |

### Weaknesses

| Weakness | Severity | Detail |
|----------|----------|--------|
| **Dead code** | Medium | `BarcodePrinter`, `BuildEscPosReceipt()`, `FormatEnglishReceipt()`, `FormatArabicReceipt()`, 2 stub endpoints |
| **No dependency injection** | Medium | `IPrinterService` interface exists but is unused; concrete types are created inline |
| **ASCII encoding breaks Arabic** | High | Text-mode barcode labels lose Arabic characters in product names |
| **No retry/queue/history** | High | Printing is fire-and-forget with silent error swallowing |
| **Hardcoded assumptions** | Medium | 576px width, Xprinter default, Tahoma font, Egyptian pounds, 42/32 char widths |
| **No structured logging** | Medium | All output via `Console.WriteLine` — no levels, timestamps, or file output |
| **No tests** | High | Zero test coverage for any printing code |
| **Inconsistent error HTTP codes** | Low | Barcode failure returns 200 instead of 500 |
| **`GetPixel()` performance** | Medium | ImagePrinter uses slow per-pixel access instead of `LockBits()` |
| **DTO triplication** | Low | Three identical DTOs manually mapped between each other |
| **Windows-only** | High | `System.Drawing` GDI+ and `winspool.drv` are Windows-only; blocks future cross-platform migration |
