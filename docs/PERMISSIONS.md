# PERMISSIONS.md

> **Status:** Active
>
> This document represents the current Feature/Permission model of the POS application.
>
> Last reviewed: 2026-08-14

> ⚠️ **Implementation status:** This is the **live** authorization model. RBAC is fully
> implemented — permissions seeded by `backend-cs/Database/Migrations/010_seed_permissions.sql`,
> roles by `011_seed_roles.sql`, enforced by `[RequirePermission]` attributes and
> `AuthorizationService.HasPermission` in the backend controllers, and consumed by the
> frontend `src/features/auth/auth-context.tsx`.

## 1. Permission Overview

A **Permission** answers: *"Is this user/role allowed to perform this action?"*

- Permissions belong to **Roles**. Users receive permissions through their assigned roles.
- Naming convention: `resource.action` (e.g. `products.delete`, `discounts.product`).
- Permissions are **system-defined** and seeded via migration — restaurant admins do not
  invent custom permissions.
- A Permission is **different from a Feature**:
  - Feature = capability the restaurant/tenant has (`product_discount`).
  - Permission = capability the current user/role has (`discounts.product`).
  - For sensitive operations both must be satisfied (Feature **AND** Permission),
    enforced on the backend — frontend hiding is never the security boundary.

## 2. Complete Permission Table

| Permission Key    | Resource | Action   | Name                  | Description                                   | Related Feature  | Sensitive? |
| ----------------- | -------- | -------- | --------------------- | --------------------------------------------- | ---------------- | ---------- |
| products.view     | products | view     | View Products         | View, search and list products                | —                | No         |
| products.create   | products | create   | Create Products       | Create products (incl. POS unknown-barcode)   | —                | Yes        |
| products.update   | products | update   | Update Products       | Update products, units and barcodes           | multiple_units, multiple_barcodes | Yes |
| products.delete   | products | delete   | Delete Products       | Delete products                               | —                | Yes        |
| invoices.view     | invoices | view     | View Invoices         | View invoices and sales history               | —                | No         |
| invoices.create   | invoices | create   | Create Invoices       | Ring up sales / create invoices               | —                | Yes        |
| discounts.product | discounts | product | Product Discount      | Apply line/product-level discounts            | product_discount | Yes        |
| discounts.invoice | discounts | invoice  | Invoice Discount      | Apply invoice-level discounts                 | invoice_discount | Yes        |
| price.override    | price     | override | Price Override        | Override a line unit price during checkout    | price_override   | Yes        |
| reports.view      | reports   | view     | View Reports          | View reports (currently low-stock)            | low_stock_report | No         |
| reports.export    | reports   | export   | Export Reports        | Export report data *(future — no UI yet)*      | reports          | Yes        |
| printing.receipt  | printing  | receipt  | Print Receipts        | Print thermal receipts                        | receipt_printing | No         |
| printing.barcode  | printing  | barcode  | Print Barcode Labels  | Print barcode labels                          | barcode_printing | No         |
| license.view      | license   | view     | View License          | Read license status (boot gate)               | —                | No         |
| license.manage    | license   | manage   | Manage License        | Unlock / manage license                       | —                | Yes        |
| settings.view     | settings  | view     | View Settings         | View tenant settings / feature config         | —                | No         |
| settings.update   | settings  | update   | Update Settings       | Change tenant settings / features             | —                | Yes        |
| users.manage      | users     | manage   | Manage Users          | Manage users and their roles                  | —                | Yes        |
| roles.manage      | roles     | manage   | Manage Roles          | Manage roles & role-permission assignments    | —                | Yes        |

## 3. Permissions Grouped by Module

### Products
- `products.view`
- `products.create`
- `products.update`
- `products.delete`

### Invoices / Orders
- `invoices.view`
- `invoices.create`

### Discounts
- `discounts.product`
- `discounts.invoice`

### Pricing
- `price.override`

### Reports
- `reports.view`
- `reports.export`

### Printing
- `printing.receipt`
- `printing.barcode`

### Licensing
- `license.view`
- `license.manage`

### Settings / Administration
- `settings.view`
- `settings.update`
- `users.manage`
- `roles.manage`

## 4. Role → Permission Matrix

| Permission        | Admin | Manager | Cashier |
| ----------------- | ----: | ------: | ------: |
| products.view     |     ✓ |       ✓ |       ✓ |
| products.create   |     ✓ |       ✓ |       ✗ |
| products.update   |     ✓ |       ✓ |       ✗ |
| products.delete   |     ✓ |       ✓ |       ✗ |
| invoices.view     |     ✓ |       ✓ |       ✓ |
| invoices.create   |     ✓ |       ✓ |       ✓ |
| discounts.product |     ✓ |       ✓ |       ✗ |
| discounts.invoice |     ✓ |       ✓ |       ✗ |
| price.override    |     ✓ |       ✓ |       ✗ |
| reports.view      |     ✓ |       ✓ |       ✗ |
| reports.export    |     ✓ |       ✗ |       ✗ |
| printing.receipt  |     ✓ |       ✓ |       ✓ |
| printing.barcode  |     ✓ |       ✓ |       ✗ |
| license.view      |     ✓ |       ✓ |       ✓ |
| license.manage    |     ✓ |       ✗ |       ✗ |
| settings.view     |     ✓ |       ✗ |       ✗ |
| settings.update   |     ✓ |       ✗ |       ✗ |
| users.manage      |     ✓ |       ✗ |       ✗ |
| roles.manage      |     ✓ |       ✗ |       ✗ |

## 5. Backend Enforcement

Every sensitive permission is enforced on the backend — the frontend is UX only.

| Permission | Endpoint / Service | Authorization Mechanism |
|---|---|---|
| products.delete | `DELETE /api/products/{id}` | RequirePermission("products.delete") |
| products.create | `POST /api/products` | RequirePermission("products.create") |
| products.update | `PUT /api/products/{id}`, unit/barcode endpoints | RequirePermission("products.update") + feature (units/barcodes) |
| invoices.create | `POST /api/invoices` | RequirePermission("invoices.create") |
| discounts.product | `POST /api/invoices` (line discount in payload) | Permission + Feature check |
| discounts.invoice | `POST /api/invoices` (invoice discount in payload) | Permission + Feature check |
| price.override | `POST /api/invoices` (unitPrice ≠ original) | Permission + Feature check |
| reports.view | `GET /api/reports/low-stock` | RequirePermission("reports.view") + feature |
| printing.receipt | `POST /api/printing/print` | RequirePermission("printing.receipt") + feature |
| printing.barcode | `POST /api/printing/print-barcode` | RequirePermission("printing.barcode") + feature |
| roles.manage | `GET/POST/PUT/DELETE /api/roles*` | RequirePermission("roles.manage") |
| users.manage | `GET/POST/PUT /api/users*` | RequirePermission("users.manage") |
| settings.update | `PUT /api/tenant/features` | RequirePermission("settings.update") |

## 6. Frontend Usage

| Permission | Frontend usage |
|---|---|
| products.view | `src/app/[locale]/products/products-client.tsx` (table), POS search `src/app/[locale]/pos/pos-client.tsx`, low-stock page |
| products.create | `src/app/[locale]/products/product-form.tsx` (submit), POS unknown-barcode dialog `pos-client.tsx` |
| products.update | `products-client.tsx` (edit unit/barcode dialogs) |
| products.delete | `products-client.tsx` (delete button) |
| invoices.view | `src/app/[locale]/invoices/invoices-client.tsx`, dashboard `src/app/[locale]/page.tsx` |
| invoices.create | `pos-client.tsx` (checkout buttons F11/F12) |
| discounts.product | `pos-client.tsx` line-edit dialog (line discount section) |
| discounts.invoice | `pos-client.tsx` checkout discount input (F2 / ^Space) |
| price.override | `pos-client.tsx` line-edit dialog (unit price field) |
| reports.view | `src/app/[locale]/low-stock/page.tsx` |
| printing.receipt | `pos-client.tsx` "Save & Print" (F12) |
| printing.barcode | `products-client.tsx` print-barcode dialog |
| license.view | `src/components/common/license-lock.tsx` |
| license.manage | `src/features/license/actions.ts` + `POST /api/license/unlock` |
| settings.view | `src/app/[locale]/settings/features/page.tsx` |
| settings.update | `src/app/[locale]/settings/features/features-client.tsx` (save button) |
| users.manage | `src/app/[locale]/settings/users/*` |
| roles.manage | `src/app/[locale]/settings/roles/*` |

## Changelog

| Date | Change |
|------|--------|
| 2026-08-14 | Initial Permission inventory from project audit (proposed model) |
| 2026-08-15 | RBAC live: seeded permissions (19), roles (3), backend enforcement (17) + admin UI |
| 2026-08-15 | Login converted from PIN to username+password (`User.username` unique, `pinHash`→`passwordHash`, migration `013_username_password.sql`) |
| 2026-08-15 | Dedicated Role Permissions screen (`/settings/permissions/`, select role → edit checkboxes) + `refreshAccess()` re-validates session after permission/role/user/feature changes |
