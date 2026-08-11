# POS System (Desktop)

A bilingual (Arabic/English), offline-first point-of-sale desktop application for
retail stores, supermarkets, groceries, wholesalers and pharmacies. It pairs a
**C# / .NET self-host backend** with a **Next.js static frontend** served from the
same process — no cloud, no internet required.

The backend runs on **port 3001** and serves both the REST API (`/api/*`) and the
built frontend. The database is a single SQLite file stored in the `data/` folder.

---

## Features

### Point of Sale (POS)
- Fast product search and barcode scanning (exact barcode auto-adds to cart).
- **Retail / Wholesale price mode** — switching mode re-prices every cart line.
- **Multiple selling units** — products can be sold as Piece, Pack, Carton, etc.,
  each with its own barcode and price; stock is always counted in the base unit.
- **Per-line edit dialog**: override unit price (original shown struck through),
  optional cashier reason note, percentage or fixed discount, quantity changes.
- **Smart discounts** — invoice-level discount (percentage or fixed) applied only
  to products that allow discounts, distributed proportionally across lines.
- **Profit protection** — selling below cost price is blocked on both frontend
  and backend.
- **Insufficient stock protection** — over-selling is rejected with a clear
  message instead of silently failing.
- **Unknown-barcode dialog** — create a new product or link to an existing one
  right from the checkout screen.

### Products
- Product catalog with search, server-side paging, low-stock thresholds, notes,
  and per-product "allow discount" flag.
- **Product Units section** — add/edit/delete selling units with quantity factor,
  retail and wholesale prices, and unit-scoped barcodes (multiple barcodes per
  unit, one default).
- Auto-generated 12-digit barcode when none is provided.
- Low Stock Report screen using each product's own threshold.

### Invoices
- Auto-incrementing invoice numbers and full history with server-side paging.
- Search by invoice number, date-range filters with quick presets
  (today / week / month / all), revenue and discount summary cards.
- Invoice details show real invoice number, unit names, struck-through override
  prices, discounts and price-edit notes.

### Printing
- ESC/POS thermal receipt printing with native **Arabic shaping and BiDi**
  (the receipt is rendered as an image via Windows GDI+, so Arabic text is
  flawless).
- Barcode label printing.
- Printer name configurable via the `PRINTER_NAME` environment variable
  (default `Xprinter`).

### License & Activation
- Machine-ID-based activation with a lock screen gate on first boot.

### Dashboard
- Today's revenue, sales count, discounts given, and total product count with a
  quick action to open the POS.

---

## Tech Stack

| Layer     | Technology |
|-----------|------------|
| Backend   | C# / .NET Framework 4.8, OWIN self-host, ASP.NET Web API |
| Database  | SQLite via Dapper (`Microsoft.Data.Sqlite`), SQL migration files |
| Printing  | winspool.drv P/Invoke (RawPrinterHelper), System.Drawing receipt rendering |
| Frontend  | Next.js 16 (static export), React 19, TypeScript, Tailwind CSS 4 |
| UI        | shadcn/ui, @base-ui/react, @tanstack/react-table, lucide-react |
| State     | Zustand |
| i18n      | next-intl (Arabic-first, RTL) |

---

## Project Structure

```
backend-cs/                  # .NET Framework 4.8 backend
  Controllers/               # Products, Invoices, License, Printing, Reports, Health
  Database/Migrations/       # 001_init .. 008_price_edit_note (auto-applied on start)
  Models/                    # Product, ProductUnit, ProductBarcode, Invoice, Settings
  Repositories/              # Dapper data access
  Services/                  # Receipt, Printer, Barcode
  Printers/                  # ESC/POS image + barcode output
  Builders/ReceiptBuilder.cs # Receipt image construction
src/                         # Next.js frontend (client components only)
  app/[locale]/              # Pages: /, /pos, /products, /invoices, /low-stock
  components/                # UI components, layouts, common dialogs
  features/                  # pos store, invoices, products, license actions
  lib/api.ts                 # Typed API client
messages/                    # next-intl translations (ar.json)
docs/                        # Feature specs and release notes
CHANGELOG.md                 # Version history
```

---

## Quick Start (Development)

### 1. Run the backend

Requires .NET Framework 4.8 targeting (Windows).

```powershell
dotnet build backend-cs/pos-cs.csproj --configuration Release
# run the produced executable, e.g.:
& .\backend-cs\bin\Release\net48\pos-server.exe
```

The server listens on `http://localhost:3001`. The database is created in
`data/` next to the executable and migrations are applied automatically.

Optional environment variables:

| Variable        | Default    | Purpose |
|-----------------|------------|---------|
| `API_PORT`      | `3001`     | Server port |
| `PRINTER_NAME`  | `Xprinter` | Thermal printer name for receipts/barcodes |

### 2. Run the frontend (dev mode)

```powershell
npm install
npm run dev
```

Open http://localhost:3000 (dev) or http://localhost:3001 (production). The API
base URL defaults to `http://localhost:3001` and can be overridden with
`NEXT_PUBLIC_API_URL`.

---

## Build & Release

The backend serves the static frontend, so there is no separate web server.

1. Build the frontend static export:

   ```powershell
   npm run build
   ```

2. Copy the contents of `out/` (e.g. the `ar/` folder and assets) into
   `backend-cs/wwwroot/`.

3. Build the backend:

   ```powershell
   dotnet build backend-cs/pos-cs.csproj --configuration Release
   ```

4. Package `pos-server.exe`, `Migrations/`, `wwwroot/` and `start.bat` together.
   `start.bat` launches the server and opens `http://localhost:3001`.

> **Upgrade note:** when updating an existing installation, replace the
> executable, `Migrations/` and `wwwroot/` but **keep the `data/` folder** — it
> contains the database. Migrations run automatically on start.

---

## Commands

| Command | Description |
|---------|-------------|
| `npm run dev` | Start Next.js dev server |
| `npm run build` | Static export build (outputs to `out/`) |
| `npm run lint` | Run ESLint |
| `dotnet build backend-cs/pos-cs.csproj --configuration Release` | Build the backend |

---

## Database

SQLite with versioned SQL migrations in `backend-cs/Database/Migrations/`,
applied automatically on server start. The server **exits on migration failure**
to avoid serving a partially upgraded database.

| Migration | Change |
|-----------|--------|
| `001_init` | Product, Invoice, InvoiceDetail |
| `002_settings` | Settings (machine ID / license) |
| `003_add_notes` | Product.notes |
| `004_add_invoice_number` | Invoice.invoiceNumber + auto-increment trigger |
| `005_add_discount_lowstock` | Discount eligibility, thresholds, discount columns |
| `006_product_barcodes` | ProductBarcode table, drop Product.barcode |
| `007_product_units` | ProductUnit table, unit-scoped barcodes, price mode |
| `008_price_edit_note` | InvoiceDetail.PriceEditNote |

---

## API Overview

All endpoints live under `http://localhost:3001/api`.

| Area      | Endpoints |
|-----------|-----------|
| Products  | `GET /products`, `GET /products/{id}`, `POST /products`, `PUT /products/{id}`, `DELETE /products/{id}`, `GET /products/paged`, `GET /products/search`, `GET /products/count` |
| Units     | `POST/PUT/DELETE /products/{id}/units[/{unitId}]`, `POST/DELETE /products/{id}/units/{unitId}/barcodes[/{barcodeId}]`, `PUT .../barcodes/{barcodeId}/default` |
| Invoices  | `GET /invoices`, `POST /invoices`, `GET /invoices/{id}`, `GET /invoices/paged`, `GET /invoices/filter` |
| Reports   | `GET /reports/low-stock` |
| Printing  | `POST /printing/print`, `POST /printing/print-barcode` |
| License   | `GET /license`, `POST /license/unlock` |
| Health    | `GET /health` |

---

## Localization

The UI is **Arabic-first with RTL** layout. Translations live in `messages/`
(`ar.json`) and use next-intl. Only the `ar` locale is built for production;
additional locales can be added by adding a new message file and a static param.

---

## Documentation

- `docs/features/` — feature specs (multiple barcodes, advanced pricing & units).
- `docs/release/` — per-version release notes.
- `CHANGELOG.md` — full version history.
- `PRINTING-ANALYSIS.md` — receipt-printing implementation analysis.

---

## License

Proprietary. The application is activated per machine via a license key flow
implemented in the `License` API and the license lock screen.
