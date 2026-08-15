# FEATURES.md

> **Status:** Active
>
> This document represents the current Feature/Permission model of the POS application.
>
> Last reviewed: 2026-08-14

> ⚠️ **Implementation status:** This is the **live** feature model. Tenant features are
> fully implemented — seeded by `backend-cs/Database/Migrations/012_seed_tenant.sql`,
> read/written via `GET/PUT /api/tenant/features`, validated together with permissions by
> the backend controllers, and consumed by the frontend `src/features/auth/auth-context.tsx`
> and the `/settings/features` admin page.

## 1. Feature Overview

A **Feature** answers: *"Does this restaurant/tenant have this capability enabled?"*

- Features are associated with the **restaurant/tenant**, not the user.
- A Feature is **different from a Permission**:

```text
Feature:    product_discount  →  the restaurant HAS Product Discount capability.
Permission: discounts.product →  the current user IS ALLOWED to use Product Discount.
```

- Even if a user has the permission, the capability is unavailable when the tenant
  does not have the feature enabled — and vice versa.

## 2. Complete Feature Table

| Feature Key       | Name                  | Description                                              | Category  | Tenant Configurable? | Related Permissions        | Frontend Location | Backend Impact |
| ----------------- | --------------------- | -------------------------------------------------------- | --------- | -------------------- | -------------------------- | ----------------- | -------------- |
| multiple_units    | Multiple Selling Units| Piece / Pack / Carton support with quantity factors      | Products  | Yes                  | products.update            | Product form, POS unit picker | Yes |
| multiple_barcodes | Multiple Barcodes     | Multiple barcodes per selling unit                       | Products  | Yes                  | products.update            | Product form barcodes, POS link flow | Yes |
| wholesale_price   | Wholesale Price       | Retail/Wholesale selling modes                           | Pricing   | Yes                  | invoices.create            | POS price-mode toggle | Yes |
| product_discount  | Product Discount      | Apply discount to individual invoice lines               | Discounts | Yes                  | discounts.product          | POS line-edit dialog | Yes |
| invoice_discount  | Invoice Discount      | Apply discount to the entire invoice                     | Discounts | Yes                  | discounts.invoice          | POS checkout discount | Yes |
| price_override    | Price Override        | Cashier may override a line price during checkout        | Pricing   | Yes                  | price.override             | POS line-edit dialog | Yes |
| low_stock_report  | Low Stock Report      | Threshold-based low stock monitoring/report              | Reports   | Yes                  | reports.view               | `/low-stock` page | Yes |
| receipt_printing  | Receipt Printing      | Thermal receipt printing (image-mode ESC/POS)            | Printing  | Yes                  | printing.receipt           | POS "Save & Print" | Yes |
| barcode_printing  | Barcode Printing      | Barcode label printing                                   | Printing  | Yes                  | printing.barcode           | Products page print dialog | Yes |

## 3. Features Grouped by Module

### Products
- `multiple_units`
- `multiple_barcodes`

### Pricing
- `wholesale_price`
- `price_override`

### Discounts
- `product_discount`
- `invoice_discount`

### Reports
- `low_stock_report`

### Printing
- `receipt_printing`
- `barcode_printing`

## 4. Feature → Permission Relationship

| Feature | Required Permission(s) | Description |
|---|---|---|
| multiple_units | products.update | Configuring extra units is a product-edit action |
| multiple_barcodes | products.update | Linking/managing barcodes is a product-edit action |
| wholesale_price | invoices.create | Wholesale mode applies at sale time |
| product_discount | discounts.product | User must be allowed to apply product discounts |
| invoice_discount | discounts.invoice | User must be allowed to apply invoice discounts |
| price_override | price.override | User must be allowed to override prices |
| low_stock_report | reports.view | User must be allowed to view reports |
| receipt_printing | printing.receipt | User must be allowed to print receipts |
| barcode_printing | printing.barcode | User must be allowed to print barcode labels |

**Configuration-only note:** every feature above has a permission because each one
either mutates financial data, writes product data, or triggers a hardware action.
A feature without a permission (e.g. a pure display preference) would be documented
with `-` here.

## 5. Feature Access Flow

```text
Restaurant
    ↓
Feature Enabled?
    ↓ YES
User Permission
    ↓
Permission Granted?
    ↓ YES
Feature Available in UI
    ↓
Backend validates Feature + Permission again for sensitive operations
```

Example — product discount:

```text
product_discount
    ↓
Restaurant has product_discount?
    ↓ YES
User has discounts.product?
    ↓ YES
Show line-discount UI
    ↓
POST /api/invoices → backend re-checks feature + permission
```

## 6. Frontend Usage

### `multiple_units`
- Frontend: `src/app/[locale]/products/products-client.tsx` (unit dialogs), `src/app/[locale]/pos/pos-client.tsx` (unit picker).
- Access: `hasFeature(FEATURES.MULTIPLE_UNITS) && hasPermission(PERMISSIONS.PRODUCTS_UPDATE)`.
- Disabled behavior: unit management section and unit picker hidden; single base unit only.

### `multiple_barcodes`
- Frontend: `products-client.tsx` (barcode dialogs), `pos-client.tsx` (link-barcode flow).
- Access: `hasFeature(FEATURES.MULTIPLE_BARCODES) && hasPermission(PERMISSIONS.PRODUCTS_UPDATE)`.
- Disabled behavior: add/remove barcode actions hidden.

### `wholesale_price`
- Frontend: `pos-client.tsx` (Retail/Wholesale toggle).
- Access: `hasFeature(FEATURES.WHOLESALE_PRICE)`.
- Disabled behavior: toggle hidden; invoices forced to retail.

### `product_discount`
- Frontend: `pos-client.tsx` (line-edit dialog discount section).
- Access: `hasFeature(FEATURES.PRODUCT_DISCOUNT) && hasPermission(PERMISSIONS.DISCOUNTS_PRODUCT)`.
- Disabled behavior: line-discount inputs not rendered.

### `invoice_discount`
- Frontend: `pos-client.tsx` (checkout discount input).
- Access: `hasFeature(FEATURES.INVOICE_DISCOUNT) && hasPermission(PERMISSIONS.DISCOUNTS_INVOICE)`.
- Disabled behavior: discount input/toggle not rendered.

### `price_override`
- Frontend: `pos-client.tsx` (line-edit unit price + note).
- Access: `hasFeature(FEATURES.PRICE_OVERRIDE) && hasPermission(PERMISSIONS.PRICE_OVERRIDE)`.
- Disabled behavior: unit price field read-only.

### `low_stock_report`
- Frontend: `src/app/[locale]/low-stock/page.tsx`, sidebar link.
- Access: `hasFeature(FEATURES.LOW_STOCK_REPORT) && hasPermission(PERMISSIONS.REPORTS_VIEW)`.
- Disabled behavior: page/sidebar hidden.

### `receipt_printing`
- Frontend: `pos-client.tsx` (Save & Print button, F12).
- Access: `hasFeature(FEATURES.RECEIPT_PRINTING) && hasPermission(PERMISSIONS.PRINTING_RECEIPT)`.
- Disabled behavior: print action hidden (Save-only mode).

### `barcode_printing`
- Frontend: `products-client.tsx` (print-barcode dialog).
- Access: `hasFeature(FEATURES.BARCODE_PRINTING) && hasPermission(PERMISSIONS.PRINTING_BARCODE)`.
- Disabled behavior: print-barcode button hidden.

## 7. Backend Usage

| Feature | Endpoint / Service | Feature Check |
|---|---|---|
| multiple_units | `POST/PUT/DELETE /api/products/{id}/units` | Required |
| multiple_barcodes | barcode endpoints under `/api/products/{id}/units/{unitId}/barcodes` | Required |
| wholesale_price | `POST /api/invoices` (priceMode == wholesale) | Required |
| product_discount | `POST /api/invoices` (line discount present) | Required |
| invoice_discount | `POST /api/invoices` (invoice discount present) | Required |
| price_override | `POST /api/invoices` (unitPrice != original) | Required |
| low_stock_report | `GET /api/reports/low-stock` | Required |
| receipt_printing | `POST /api/printing/print` | Required |
| barcode_printing | `POST /api/printing/print-barcode` | Required |

## Changelog

| Date | Change |
|------|--------|
| 2026-08-14 | Initial Feature inventory from project audit (proposed model) |
| 2026-08-15 | Features live: tenant seeding (9), `/api/tenant/features` GET/PUT, `/settings/features` admin UI |
