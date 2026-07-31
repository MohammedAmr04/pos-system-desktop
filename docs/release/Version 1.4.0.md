# POS System Release Notes

# Version 1.4.0

Release Name

Advanced Pricing & Product Units

---

## Overview

This release introduces selling units with dual retail/wholesale pricing and
per-line control at the point of sale.

Each product can now define one or more selling units (e.g. Piece, Carton,
Kilogram). Every unit has its own retail and wholesale price, and a quantity
factor relative to the base unit. The base unit owns the product stock; selling
a pack automatically deducts the correct amount of stock.

This is a Business Logic update and requires a database migration.

---

# New Features

## Product Units

A product has one base unit (created automatically) and can have additional
selling units:

- Base unit is the stock owner and cannot be deleted.
- Non-base units define a quantity factor (how many base units they contain),
  e.g. Carton = 6, Kilogram = 1 (factor 0.25 means a quarter of the base unit).
- Decimal factors are allowed (0.25 kg, 1.5 kg, ...).
- Each unit has its own Retail Price and optional Wholesale Price.
- Prices of a non-base unit are per unit, not per contained base unit.

## Unit-Scoped Barcodes

Barcodes now belong to a unit, not to the product:

- A barcode scanned in the POS resolves the exact selling unit automatically.
- Scanning a pack barcode sells the pack at the pack price.
- The default barcode rule is unchanged: the first barcode of the base unit
  becomes the product's primary barcode.

## Retail / Wholesale Pricing

The POS screen now has a price-mode selector (Retail / Wholesale):

- Wholesale mode uses each unit's wholesale price when set, otherwise falls
  back to the retail price.
- Switching the mode re-prices every cart line.

## Per-Line Override and Discount

Each cart line can be adjusted independently:

- Unit price override (the original price is shown struck through).
- Line discount as percentage or fixed amount.
- Invoice-level discount (fixed or percentage) is distributed over the lines
  that allow discounts, proportionally to their totals.

## Profit Protection

The system still blocks selling below cost price, now computed per line after
all line and invoice discounts.

## Insufficient Stock Handling

Selling more than the available stock is rejected with a clear message instead
of silently not deducting stock.

---

# Database Changes

## New Table

### ProductUnit

- Id
- ProductId (FK -> Products)
- UnitName
- QuantityFactor (base unit = 1)
- RetailPrice
- WholesalePrice (nullable)
- IsBaseUnit
- CreatedAt

## Changed Tables

### ProductBarcode

- ProductId replaced by ProductUnitId (FK -> ProductUnit)
- Every barcode now belongs to a unit

### Product

Removed

- salePrice

Retail price moved to the base unit (retail_price).

### InvoiceDetail

Added columns

- ProductUnitId
- UnitName
- OriginalUnitPrice
- UnitPrice
- DiscountType
- DiscountValue
- LineSubtotal
- FinalTotal
- QuantityFactor

### Invoice

Added column

- PriceMode

## Migration Backfill

During migration 007:

- Every product gets a base unit (Piece) whose retail price is copied from the
  old salePrice, so no price data is lost.
- Every existing barcode is re-parented to the base unit.
- Existing invoices are backfilled: retail mode, original/unit price equal to
  the recorded price, line totals computed from existing amounts.

---

# Backend Improvements

- New endpoints:
  - POST/PUT/DELETE /api/products/{id}/units[/{unitId}]
  - POST/DELETE /api/products/{id}/units/{unitId}/barcodes[/{barcodeId}]
  - PUT /api/products/{id}/units/{unitId}/barcodes/{barcodeId}/default
- Invoice creation now accepts priceMode, unit-scoped items, per-line override
  and per-line discounts.
- Stock is deducted in base units using the quantity factor with an affected-
  rows guard, so an out-of-stock sale is rejected instead of silently no-oping.
- Receipts show the unit name next to the product and the final line total.

---

# Frontend Improvements

- POS: retail/wholesale selector, unit-aware search and barcode scanning,
  unit picker when a product has multiple units, editable unit price per line,
  per-line discount dialog, unit-based quantity.
- Products screen: Product Units section with add/edit/delete unit, per-unit
  retail/wholesale prices and barcode management.
- Product form: retail price, wholesale price, base unit name, decimal stock.
- Invoices screen: shows unit name, struck-through original price on override,
  and final line totals.

---

# Compatibility

Database Migration Required

Yes (007)

Frontend Update Required

Yes

Backend Update Required

Yes

API Changes

Major (barcode endpoints moved under units)

Breaking Changes

Frontend now uses the unit-scoped barcode/unit endpoints. The old flat
product barcode endpoints are removed.

---

# Technical Notes

Upgrade flow on existing installations:

1. Stop the old server.
2. Replace pos-server.exe, Migrations, and wwwroot with the new versions.
3. Keep the data folder (dev.db) - it must not be replaced.
4. Start the new server. Migration 007 runs automatically: products get a
   base unit "Piece", barcodes are re-parented to the base unit, and existing
   invoices are backfilled with retail-mode pricing.

Existing retail price data is preserved as the base unit retail price, so
day-to-day behavior is unchanged until the user adds new units or wholesale
prices.

---

# Future Ready

This structure prepares the system for:

- Purchase invoices in supplier units
- Cost history per unit
- Promotions / price lists per unit
- Batch pricing (wholesale tiers)
- Unit-level stock counting
