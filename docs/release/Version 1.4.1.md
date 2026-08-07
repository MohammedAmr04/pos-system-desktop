# POS System Release Notes

# Version 1.4.1

Release Name

Performance Fixes & Server-Side Tables

---

## Overview

This release brings the v1.0.1 performance and stability fixes into the main
line (v1.4.0). Product and invoice listing now use server-side pagination and
search instead of loading the full dataset into the browser, which also fixes
the infinite-loading spinner on the Products and Invoices screens.

All v1.4.0 features are preserved: product units, retail/wholesale pricing,
unit-scoped barcodes, price-edit notes, per-line discounts, and the low-stock
screen.

No new database migration is required.

---

# Fixed Bugs

## Infinite Loading on Table Pages

Products and Invoices pages could stay stuck on the loading spinner because a
debounced search fired once on page mount even when nothing was typed. The
tables now always settle to the fetched rows.

## POS Product List Scale

The POS previously loaded every product into the browser at once. It now loads
the list lazily on mount and searches through the server, keeping scanning and
search fast on large catalogs.

---

# Backend Improvements

- New endpoints:
  - `GET /api/products/paged?page=&pageSize=&q=` - server-side pagination
    with optional search, returns `{ items, total, page, pageSize }`
  - `GET /api/products/search?q=&limit=` - relevance search used by the POS
    screen (barcode exact, barcode prefix, then name contains)
  - `GET /api/products/count` - total product count for the dashboard
  - `GET /api/invoices/paged?page=&pageSize=&from=&to=&q=` - server-side
    pagination with optional date range and invoice-number search, returns
    `{ items, total, page, pageSize, totals }`
  - `GET /api/invoices/{id}` - single invoice with line details
- Product search now matches against the `ProductBarcode` table (via the
  owning `ProductUnit`) as well as the product name, so unit-scoped barcodes
  resolve correctly after migration 006 dropped the old flat `barcode` column.
- Products returned from `search` and `paged` include their units and barcodes,
  so the POS unit picker and the products screen work from server data.
- Invoice-number search runs within the selected date range; when no dates are
  given the default behaviour is unchanged (today only).
- `page`/`pageSize` are clamped and `LIKE` search input is escaped
  (`%`, `_`, `\`), so wildcard characters in product names or invoice-number
  queries are treated literally.
- Invoice totals (revenue and discounts) are computed in the same query that
  returns the page, so the summary cards stay consistent with the filter.

---

# Frontend Improvements

- Products screen: server-side table (20 rows per page) with debounced search
  and prev/next pagination; loading state no longer gets stuck. The units,
  barcodes, and per-unit pricing sections are unchanged.
- Invoices screen: server-side table with search-by-invoice-number, date range
  filters with quick presets (today / week / month / all), revenue and discount
  summary cards, and pagination. Invoice details still show the real invoice
  number, unit names, struck-through override prices, and price-edit notes.
- POS screen: search is debounced and sent to the server, stale results from a
  previous query are discarded, and out-of-stock / max-stock lines show a toast
  instead of silently failing. The unit picker, retail/wholesale selector, and
  price-override dialog are unchanged.
- Dashboard product counter uses the new count endpoint.

---

# Database Changes

None. Migrations remain `001_init` through `008_price_edit_note`.
`InvoiceNumber` was already introduced by migration `004_add_invoice_number`.

---

# Compatibility

Database Migration Required

No

Frontend Update Required

Yes

Backend Update Required

Yes

API Changes

Additive (new endpoints only; existing endpoints unchanged)

Breaking Changes

None. Existing clients keep working against the unchanged endpoints.
