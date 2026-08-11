# Changelog

All notable changes to the POS System are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.4.1] - 2026-08-08

Release name: Performance Fixes & Server-Side Tables

Brings the v1.0.1 performance and stability fixes into the main line (v1.4.0).
All v1.4.0 features are preserved: product units, retail/wholesale pricing,
unit-scoped barcodes, price-edit notes, per-line discounts, and the low-stock
screen.

### Fixed
- Products and Invoices pages could stay stuck on the loading spinner because
  a debounced search fired once on page mount even when nothing was typed.
  Tables now always settle to the fetched rows.
- The POS previously loaded every product into the browser at once. It now
  loads the list lazily on mount and searches through the server.

### Changed
- Product and invoice listing moved to server-side pagination and search
  instead of loading the full dataset into the browser.
- Product search now matches unit-scoped barcodes (via the owning
  `ProductUnit`) as well as product names.
- Products returned from `search` and `paged` include their units and barcodes.
- Invoice-number search runs within the selected date range.
- `page`/`pageSize` values are clamped and `LIKE` search input is escaped
  (`%`, `_`, `\`), so wildcard characters are treated literally.
- Invoice totals (revenue and discounts) are computed in the same query that
  returns the page, so summary cards stay consistent with the filter.

### Added
- New backend endpoints:
  - `GET /api/products/paged?page=&pageSize=&q=`
  - `GET /api/products/search?q=&limit=`
  - `GET /api/products/count`
  - `GET /api/invoices/paged?page=&pageSize=&from=&to=&q=`
  - `GET /api/invoices/{id}`
- Products screen: server-side table (20 rows per page) with debounced search
  and prev/next pagination.
- Invoices screen: search by invoice number, date range filters with quick
  presets (today / week / month / all), revenue and discount summary cards.
- POS screen: debounced server search, stale-query results discarded,
  out-of-stock / max-stock lines show a toast instead of failing silently.

### Database
None. Migrations remain `001_init` through `008_price_edit_note`.

## [1.4.0] - 2026-08-01

Release name: Advanced Pricing & Product Units

### Added
- **Product units** — each product has one base unit (the stock owner, cannot
  be deleted) plus any number of selling units (Piece, Carton, Kilogram, ...).
  - Non-base units define a quantity factor (decimal allowed, e.g. `0.25`).
  - Each unit has its own Retail Price and optional Wholesale Price.
- **Unit-scoped barcodes** — barcodes belong to a unit, not to a product.
  Scanning a pack barcode automatically sells the pack at the pack price.
- **Retail / Wholesale pricing** — price-mode selector on the POS screen;
  wholesale mode falls back to retail when no wholesale price is set, and
  switching mode re-prices every cart line.
- **Per-line override and discount** — a single "Edit line" dialog lets the
  cashier change the unit price (original shown struck through), add an
  optional reason note, and apply a percentage or fixed discount.
- **Price edit notes** — recorded on the invoice line when the price differs
  from the original; shown in invoice details only, never printed on receipts.
- **Discount control** — products flagged "No Discount" hide discount inputs;
  the invoice-level discount field is hidden when no line allows discounts.
- **Profit protection** — selling below cost price is blocked, computed per
  line after all line and invoice discounts.
- **Insufficient stock handling** — selling more than available stock is
  rejected with a clear message instead of silently not deducting stock.

### Changed
- Barcodes moved from `Product` to `ProductUnit` (API moved under units).
- Product `salePrice` removed; retail price now lives on the base unit.
- Invoice creation accepts `priceMode`, unit-scoped items, per-line overrides
  and per-line discounts.
- Stock is deducted in base units using the quantity factor with an
  affected-rows guard.
- Receipts show the unit name next to the product and the final line total.

### Added (backend)
- Endpoints:
  - `POST/PUT/DELETE /api/products/{id}/units[/{unitId}]`
  - `POST/DELETE /api/products/{id}/units/{unitId}/barcodes[/{barcodeId}]`
  - `PUT /api/products/{id}/units/{unitId}/barcodes/{barcodeId}/default`

### Database
- New table `ProductUnit`.
- Changed: `ProductBarcode` now references `ProductUnitId`.
- Changed: `Product` drops `salePrice`.
- Changed: `InvoiceDetail` gains unit, pricing and discount columns plus
  `PriceEditNote` (migration 008).
- Changed: `Invoice` gains `PriceMode`.
- Migration 007 backfills: each product gets a base unit "Piece" with its
  retail price copied from the old `salePrice`; existing barcodes are
  re-parented to the base unit; existing invoices are backfilled with
  retail-mode pricing.

## [1.3.0] - 2026-07-31

Release name: Multiple Barcodes for a Single Product

### Added
- **Multiple barcodes per product** — a product can have unlimited barcodes;
  each barcode stays unique across the system. The first barcode becomes the
  default.
- **Barcode management section** on the Product screen: primary barcode, other
  barcodes, add, delete secondary, and "Set As Default".
- **Duplicate barcode validation** — adding a barcode that belongs to another
  product is rejected.
- **Smart POS behavior** — scanning an unknown barcode opens a dialog with:
  1. Create New Product (barcode prefilled, product added to cart on save)
  2. Link To Existing Product (searchable picker, added to cart on link)
  3. Cancel
- Product creation without a barcode generates a unique 12-digit barcode.

### Changed
- Barcode lookup now searches `ProductBarcode` instead of `Products`.
- POS search matches all barcodes; scanning an exact barcode auto-adds the
  product to the cart.
- Server now exits on migration failure instead of serving a partially
  upgraded database.

### Database
- New table `ProductBarcode` (`ProductId`, `Barcode`, `IsDefault`, `CreatedAt`).
- `Product.barcode` removed; existing values are copied into `ProductBarcode`
  as the default barcode during migration.

### Added (backend)
- `POST /api/products/{id}/barcodes`
- `DELETE /api/products/{id}/barcodes/{barcodeId}`
- `PUT /api/products/{id}/barcodes/{barcodeId}/default`

## [1.2.0] - 2026-07-23

Release name: Smart Discount & Low Stock Management

### Added
- **Product discount eligibility** — each product can be marked "Allow
  Discount" or "Do Not Allow Discount". Invoice discounts ignore products that
  do not allow discounts.
- **Configurable low stock** — each product has its own `Low Stock Threshold`;
  the system no longer relies on hardcoded values.
- **Smart invoice discount engine** — percentage or fixed-amount discounts
  applied only to eligible products.
- **Internal discount distribution** — discounts are distributed
  proportionally across eligible invoice lines and stored on each line,
  improving returns, profit reports, invoice accuracy and future analytics.
- **Profit protection** — the system prevents selling products below cost
  price; validation exists in both backend and frontend and invoices violating
  the rule cannot be completed.
- **Low Stock Report** uses each product's own threshold.

### Added (backend)
- Product APIs and invoice APIs updated with the discount/validation logic.

### Frontend
- Product page: Allow Discount switch and Low Stock Threshold field.
- Invoice screen: smart discount validation, profit protection validation,
  better Arabic error messages.

### Database
- `Product` gains `allowDiscount` and `lowStockThreshold`.
- `Invoice` gains `discountType`, `discountValue`, `discountAmount`.
- `InvoiceDetail` gains `discountAmount`.

## [1.0.1] - 2026-07-07

Release name: Performance & Stability Fix

Patch release of v1.0.0.

### Fixed
- Infinite loading spinner on Products and Invoices screens (the debounced
  search effect fired once on page mount even when nothing was typed).

### Changed
- Product and invoice listing moved to server-side pagination and search.
- Invoice details dialog now shows the real invoice number.
- POS search is debounced and sent to the server; stale results are
  discarded; out-of-stock / max-stock lines show a toast.

### Added
- `GET /api/products/paged`, `GET /api/products/search`, `GET /api/products/count`
- `GET /api/invoices/paged`, `GET /api/invoices/{id}`
- Invoices screen: search-by-invoice-number, date range presets
  (today / week / month / all), revenue and discount summary cards.
- `page`/`pageSize` clamping and `LIKE` escape of search input.

### Database
None. Migrations remain `001_init` through `004_add_invoice_number`.

## [1.0.0] - 2026-07-06

Release name: Initial Release

### Added
- .NET Framework 4.8 OWIN self-host backend (port 3001) with SQLite (Dapper),
  custom SQL migrations, and REST API for products, invoices, licensing,
  printing, reports, and health.
- Product management: create, edit, delete, search, stock tracking, notes.
- Invoice processing: cart, discount, invoice persistence with auto-increment
  invoice numbers, stock deduction, and a dashboard with revenue / sales /
  discount / product-count cards.
- Receipt printing via ESC/POS thermal printers (winspool.drv P/Invoke),
  rendered as an image with native Arabic shaping and BiDi support, plus
  barcode label printing.
- Machine-based license activation with a lock screen gate.
- Localized Arabic-first RTL UI (Next.js static export, next-intl), served by
  the same backend from `wwwroot`.

[Unreleased]: https://github.com/MohammedAmr04/pos-system-desktop
[1.4.1]: https://github.com/MohammedAmr04/pos-system-desktop
[1.4.0]: https://github.com/MohammedAmr04/pos-system-desktop
[1.3.0]: https://github.com/MohammedAmr04/pos-system-desktop
[1.2.0]: https://github.com/MohammedAmr04/pos-system-desktop
[1.0.1]: https://github.com/MohammedAmr04/pos-system-desktop
[1.0.0]: https://github.com/MohammedAmr04/pos-system-desktop
