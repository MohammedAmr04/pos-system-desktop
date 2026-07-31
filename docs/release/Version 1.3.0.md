# POS System Release Notes

# Version 1.3.0

Release Name

Multiple Barcodes for a Single Product

---

## Overview

This release adds full support for assigning multiple barcodes to the same
product, eliminating duplicate products created when suppliers provide
different barcodes for identical items.

This is a Business Logic update and is fully backward compatible.

---

# New Features

## Multiple Barcodes Per Product

A product can now have unlimited barcodes, but each barcode remains unique
across the system.

- The first barcode automatically becomes the primary (default) barcode.
- Any barcode scan always resolves to the same product.
- Search works across all barcodes, not just the primary one.

---

## Barcode Management Section

The Product screen now includes a dedicated Barcodes section:

- Primary Barcode
- Other Barcodes
- Add Barcode
- Delete secondary barcode
- Set As Default

---

## Add Barcode Validation

Adding a barcode that already belongs to another product is rejected:

"This barcode is already assigned to another product."

---

## Smart POS Behavior

When the cashier scans an unknown barcode, a dialog appears instead of the old
"Barcode not found" message.

The dialog offers:

1. Create New Product - opens the product form with the scanned barcode
   prefilled; the new product is added to the cart automatically.
2. Link To Existing Product - searchable picker by name or barcode; after
   linking, the product is added to the cart automatically.
3. Cancel

---

# Database Changes

## New Table

### ProductBarcode

- Id
- ProductId (FK -> Products)
- Barcode (unique across the system)
- IsDefault
- CreatedAt

---

## Product

Removed

- barcode

Existing barcode values are automatically copied into ProductBarcode during
migration (each becomes the default barcode), so no data is lost.

---

# Backend Improvements

- Barcode lookup now searches ProductBarcode instead of Products.
- New API endpoints:
  - POST /api/products/{id}/barcodes
  - DELETE /api/products/{id}/barcodes/{barcodeId}
  - PUT /api/products/{id}/barcodes/{barcodeId}/default
- Product creation without a barcode still generates a unique 12-digit barcode.
- The server now exits on migration failure instead of serving a partially
  upgraded database.

---

# Frontend Improvements

- Product edit screen: Barcodes management section.
- POS screen: unknown-barcode dialog with create/link/cancel actions.
- POS search matches all barcodes.
- Scanning an exact barcode auto-adds the product to the cart.

---

# Compatibility

Database Migration Required

Yes

Frontend Update Required

Yes

Backend Update Required

Yes

API Changes

Minor (additive endpoints)

Breaking Changes

None

---

# Technical Notes

Upgrade flow on existing installations:

1. Stop the old server.
2. Replace pos-server.exe, Migrations, and wwwroot with the new versions.
3. Keep the data folder (dev.db) - it must not be replaced.
4. Start the new server. Migration 006 runs automatically and backfills all
   existing barcodes.

Existing barcodes are migrated as the default barcode of their product, so the
application behaves exactly as before until the user adds new barcodes.

---

# Future Ready

This structure prepares the system for:

- Purchase Invoices
- Supplier-specific purchasing
- Barcode printing
- Packaging variations
- Product aliases
- Inventory batches
- Cost history
