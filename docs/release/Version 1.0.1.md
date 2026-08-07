# POS System Release Notes

# Version 1.0.1

Release Name

Performance & Stability Fix

---

## Overview

This is a patch release of v1.0.0. It moves product and invoice listing to
server-side pagination and search so the tables no longer load the full
dataset into the browser, and it fixes the infinite-loading spinner on the
Products and Invoices screens.

The database schema is unchanged (migrations 001-004). No data migration is
required.

---

# Fixed Bugs

## Infinite Loading on Table Pages

Products and Invoices pages could stay stuck on the loading spinner. The
debounced search effect fired once on page mount even when nothing was typed,
which re-triggered the loading state without a corresponding data fetch. The
effect now skips when the search value is unchanged, so the table always
settles to the fetched rows.

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
- `page`/`pageSize` values are clamped (`pageSize` 1-100, `page` minimum 1).
- User input in `LIKE` searches is escaped (`%`, `_`, `\`), so wildcard
  characters in product names or invoice-number queries are treated literally.
- Invoice totals (revenue and discounts) are computed in the same query that
  returns the page, so the summary cards stay consistent with the filter.
- Invoice-number search runs within the selected date range; when no dates are
  given the default behaviour is unchanged (today only).

---

# Frontend Improvements

- Products screen: server-side table (20 rows per page) with debounced search
  and prev/next pagination; loading state no longer gets stuck.
- Invoices screen: server-side table with search-by-invoice-number, date range
  filters with quick presets (today / week / month / all), revenue and discount
  summary cards, and pagination.
- Invoice details dialog now shows the real invoice number.
- POS screen: search is debounced and sent to the server, stale results from a
  previous query are discarded, and out-of-stock / max-stock lines show a
  toast instead of silently failing.
- Dashboard product counter uses the new count endpoint.

---

# Database Changes

None. Migrations remain `001_init`, `002_settings`, `003_add_notes`,
`004_add_invoice_number`.

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
