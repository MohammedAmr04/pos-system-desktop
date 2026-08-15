# POS — Authorization & Permissions Analysis

> **Status:** Implemented — RBAC shipped (migrations 009–012, guarded controllers,
> frontend auth + gating + admin UI). This document records the audit that drove the build.
>
> Last reviewed: 2026-08-15

This document is the deliverable of **Phase 1 (Audit)** and **Phase 2 (Architecture)** from `task.md`.
It reflects the **final** implemented design: tables/APIs/UI described below are live.

---

## 1. Architecture Summary

### 1.1 Current System (as-is)

**Backend** — C# .NET Framework 4.8, OWIN self-host on `http://localhost:3001`
(`backend-cs/`):

- Web API controllers (attribute-routed) + static file server for the exported SPA
- SQLite via Dapper + Microsoft.Data.Sqlite, custom SQL migration runner
  (`Database/Migrations/001..008`)
- Controllers:
  | Controller | Routes | Purpose |
  |---|---|---|
  | `HealthController` | `GET /health` | Liveness |
  | `LicenseController` | `GET /api/license`, `POST /api/license/unlock` | Machine activation gate |
  | `ProductsController` | `GET/POST/PUT/DELETE /api/products…` + units + barcodes | Product + unit + barcode CRUD |
  | `InvoicesController` | `GET /api/invoices*`, `POST /api/invoices` | Sales / invoice creation + discount engine |
  | `ReportsController` | `GET /api/reports/low-stock` | Low-stock report |
  | `PrintingController` | `POST /api/printing/print`, `print-barcode` | ESC/POS receipt + barcode label printing |

- **No authentication. No users. No roles. No permissions. No tenants/restaurants.**
- The only access control is the **machine-based license** (`Settings` table:
  `machineId` + `unlocked`), enforced only by the frontend `LicenseGate`.

**Frontend** — Next.js 16.2.9 static export, App Router, all client components,
single Arabic locale, served by the backend from `wwwroot/`:

- Pages: `/` (Dashboard), `/pos/`, `/products/`, `/invoices/`, `/low-stock/`
- `DashboardLayout` sidebar (5 nav items), `LicenseGate` in `[locale]/layout.tsx`
- API access via typed wrappers in `src/lib/api.ts` (no auth headers)
- POS state in a zustand store (`src/features/pos/store/usePOSStore.ts`)

### 1.2 What Exists Today (authorization-wise)

| Concept | Status |
|---|---|
| Users | None |
| Roles | None |
| Permissions | None |
| Authentication | None |
| Restaurants / Tenants | None (single machine, single business) |
| Feature flags | None (licensing only, machine-scoped) |
| Backend authorization | None (all endpoints open) |
| Business-rule validation | Present (discount engine, profit protection, stock) |

### 1.3 Proposed Target Architecture

```
Feature Flags (tenant capability)
  +  RBAC (user → role → permission)
  +  Backend Authorization (requirePermission / hasFeature)
  +  Frontend access helpers (hasPermission / hasFeature)
```

Per `task.md` §20, we deliberately do **NOT** introduce ABAC/ReBAC/policy engines/
role inheritance.

Proposed data model:

```
Tenant (restaurant)  ──►  TenantFeature (tenantId, featureKey, enabled)
    │
    └─►  User (tenantId, username, passwordHash, isActive)
              │
              ▼
           UserRole (userId, roleId)
              │
              ▼
            Role ──► RolePermission (roleId, permissionId)
              │
              ▼
          Permission (key = "resource.action")
```

---

## 2. Feature Inventory

Every real capability discovered in the codebase, classified per `task.md` §4.

| # | Capability | Location (code evidence) | Type |
|---|---|---|---|
| 1 | Product CRUD (create / view / update / delete) | `ProductsController`, `products-client.tsx`, `product-form.tsx` | Permission + Business Rule |
| 2 | Multiple selling units (Piece/Pack/Carton, qty factor) | `ProductUnit` table, `POST /api/products/{id}/units`, unit UI | **Feature** |
| 3 | Multiple barcodes per unit | `ProductBarcode` table, barcode endpoints, POS link-barcode flow | **Feature** |
| 4 | Retail vs Wholesale pricing (price mode) | `Invoice.priceMode`, POS retail/wholesale toggle | **Feature** + Business Rule |
| 5 | Line (product) discount | `InvoiceItemDto.DiscountType/DiscountValue`, line-edit dialog | Feature + Permission + Business Rule |
| 6 | Invoice discount | `CreateInvoiceDto.Discount…`, checkout discount input | Feature + Permission + Business Rule |
| 7 | Cashier price override (+ audit note) | `InvoiceItemDto.UnitPrice/PriceEditNote`, line-edit dialog | Feature + Permission + Business Rule |
| 8 | Discount engine validation (0–100%, ≤ totals) | `InvoicesController.Create` | Business Rule (backend) |
| 9 | Profit protection (sell ≥ buy price) | `InvoicesController.Create` (rejects below cost) | Business Rule (backend) |
| 10 | Stock management / decrement on sale / max qty | `InvoiceRepository.Create`, `usePOSStore` | Business Rule (backend) |
| 11 | Low-stock threshold + report | `Product.lowStockThreshold`, `GET /api/reports/low-stock`, low-stock page | Feature + Permission |
| 12 | Invoice history (list/filter by date, search by #, paged) | `InvoicesController`, `invoices-client.tsx` | Permission |
| 13 | Dashboard metrics (revenue, sales count, discounts, products) | `page.tsx` (dashboard) | Permission (reads invoices/products) |
| 14 | Receipt printing (image-based ESC/POS) | `PrintingController.Print`, `ReceiptService` | Permission + Business Rule (hardware) |
| 15 | Barcode label printing | `PrintingController.PrintBarcode` | Permission + Business Rule (hardware) |
| 16 | Unknown-barcode workflow (create / link product) | POS client dialogs | UI flow (wraps #1/#3) |
| 17 | Machine licensing / activation | `LicenseController`, `Settings` table, `LicenseGate` | Operational gate (not RBAC) |
| 18 | Server-side data tables, sorting, quick filters | `data-table.tsx`, table clients | UI-only |
| 19 | Responsive sheets / dialogs | `responsive-sheet.tsx` | UI-only |

### Classification summary

- **Feature (tenant capability):** multiple units, multiple barcodes, wholesale price,
  product discount, invoice discount, price override, low-stock report, receipt printing,
  barcode printing.
- **Permission (user capability):** products.*, invoices.*, discounts.*, price.override,
  reports.view, printing.*, license.view/manage, settings.*, users.manage, roles.manage.
- **UI-only:** table sorting/filtering/pagination, sheet/dialog behavior, quick-filter
  buttons. **No permissions created for these.**
- **Business rules (backend-enforced, non-negotiable):** discount math, profit protection,
  stock, price non-negativity, base-unit rules, barcode uniqueness, invoice totals.

---

## 3. Permission Inventory

Naming convention: `resource.action` (e.g. `products.delete`, `discounts.product`).

| Permission Key | Resource | Action | Description | Related Feature | Backend Enforcement |
|---|---|---|---|---|---|
| `products.view` | products | view | View/search/list products | — | Yes |
| `products.create` | products | create | Create products (incl. POS unknown-barcode flow) | — | Yes |
| `products.update` | products | update | Update products, units, barcodes | multiple_units / multiple_barcodes | Yes |
| `products.delete` | products | delete | Delete products | — | Yes |
| `invoices.view` | invoices | view | View invoices, invoice history, dashboard sales data | — | Yes |
| `invoices.create` | invoices | create | Ring up sales (create invoices) | — | Yes |
| `discounts.product` | discounts | product | Apply line/product-level discounts | product_discount | Yes |
| `discounts.invoice` | discounts | invoice | Apply invoice-level discounts | invoice_discount | Yes |
| `price.override` | price | override | Override a line unit price during checkout | price_override | Yes |
| `reports.view` | reports | view | View reports (currently low-stock) | low_stock_report | Yes |
| `reports.export` | reports | export | Export report data *(proposed — no export UI yet)* | reports | No (future) |
| `printing.receipt` | printing | receipt | Print receipts | receipt_printing | Yes |
| `printing.barcode` | printing | barcode | Print barcode labels | barcode_printing | Yes |
| `license.view` | license | view | Read license status | — | No (boot gate) |
| `license.manage` | license | manage | Unlock / manage license | — | Yes (`POST /api/license/unlock`) |
| `settings.view` | settings | view | View tenant settings / feature config | — | Yes |
| `settings.update` | settings | update | Change tenant settings / features | — | Yes |
| `users.manage` | users | manage | Manage users | — | Yes |
| `roles.manage` | roles | manage | Manage roles & role-permission assignments | — | Yes |

> Permissions are **system-defined** (seeded in a migration), not freely created by
> admins. No permission-CRUD API is proposed.

---

## 4. Role Matrix

Proposed seeded roles (all future-proof, system roles):

| Role | products.view | products.create | products.update | products.delete | invoices.view | invoices.create | discounts.product | discounts.invoice | price.override | reports.view | printing.receipt | printing.barcode | license.manage | settings.* | users.manage | roles.manage |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Admin | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Manager | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✗ | ✗ | ✗ | ✗ |
| Cashier | ✓ | ✗ | ✗ | ✗ | ✓ | ✓ | ✗ | ✗ | ✗ | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ |

- **Admin** — full access; only role that can manage users, roles, settings, license.
- **Manager** — all selling/product operations incl. discounts, price override, reports,
  printing; no administrative/security capabilities.
- **Cashier** — view products, view + create invoices, print receipts only.
  No discounts, no price override, no reports, no product mutation.

---

## 5. Feature Matrix (per tenant/restaurant)

| Feature Key | Name | Category | Tenant-configurable | Default | Required Permission | Backend impact |
|---|---|---|---|---|---|---|
| `multiple_units` | Multiple Selling Units | Products | Yes | On | products.update | Yes |
| `multiple_barcodes` | Multiple Barcodes | Products | Yes | On | products.update | Yes |
| `wholesale_price` | Wholesale Price | Pricing | Yes | On | invoices.create | Yes |
| `product_discount` | Product (line) Discount | Discounts | Yes | On | discounts.product | Yes |
| `invoice_discount` | Invoice Discount | Discounts | Yes | On | discounts.invoice | Yes |
| `price_override` | Price Override | Pricing | Yes | On | price.override | Yes |
| `low_stock_report` | Low Stock Report | Reports | Yes | On | reports.view | Yes |
| `receipt_printing` | Receipt Printing | Printing | Yes | On | printing.receipt | Yes |
| `barcode_printing` | Barcode Printing | Printing | Yes | On | printing.barcode | Yes |

**Rules:**
- Feature without permission = hidden/blocked. Permission without feature = hidden/blocked.
- Both must be true for the operation (UI + backend for sensitive operations).
- Features are tenant-scoped; every tenant has its own enabled set.

---

## 6. Database Design (proposed)

New tables (SQLite, following existing conventions — TEXT PK = `lower(hex(randomblob(16)))`,
`createdAt/updatedAt DATETIME`):

```sql
CREATE TABLE "Tenant" (
  id TEXT PRIMARY KEY, name TEXT NOT NULL, createdAt DATETIME DEFAULT CURRENT_TIMESTAMP, updatedAt DATETIME NOT NULL
);
CREATE TABLE "User" (
  id TEXT PRIMARY KEY, tenantId TEXT NOT NULL, name TEXT NOT NULL,
  username TEXT NOT NULL, passwordHash TEXT NOT NULL, isActive INTEGER NOT NULL DEFAULT 1,
  createdAt DATETIME DEFAULT CURRENT_TIMESTAMP, updatedAt DATETIME NOT NULL,
  FOREIGN KEY (tenantId) REFERENCES Tenant(id)
);
CREATE TABLE "Role" (
  id TEXT PRIMARY KEY, name TEXT NOT NULL UNIQUE, description TEXT,
  isSystem INTEGER NOT NULL DEFAULT 0, createdAt DATETIME DEFAULT CURRENT_TIMESTAMP, updatedAt DATETIME NOT NULL
);
CREATE TABLE "Permission" (
  id TEXT PRIMARY KEY, key TEXT NOT NULL UNIQUE, name TEXT NOT NULL, description TEXT,
  resource TEXT NOT NULL, action TEXT NOT NULL,
  createdAt DATETIME DEFAULT CURRENT_TIMESTAMP, updatedAt DATETIME NOT NULL
);
CREATE TABLE "UserRole" (
  userId TEXT NOT NULL, roleId TEXT NOT NULL,
  PRIMARY KEY (userId, roleId),
  FOREIGN KEY (userId) REFERENCES User(id) ON DELETE CASCADE,
  FOREIGN KEY (roleId) REFERENCES Role(id) ON DELETE CASCADE
);
CREATE TABLE "RolePermission" (
  roleId TEXT NOT NULL, permissionId TEXT NOT NULL,
  PRIMARY KEY (roleId, permissionId),
  FOREIGN KEY (roleId) REFERENCES Role(id) ON DELETE CASCADE,
  FOREIGN KEY (permissionId) REFERENCES Permission(id) ON DELETE CASCADE
);
CREATE TABLE "TenantFeature" (
  tenantId TEXT NOT NULL, featureKey TEXT NOT NULL, enabled INTEGER NOT NULL DEFAULT 1,
  PRIMARY KEY (tenantId, featureKey),
  FOREIGN KEY (tenantId) REFERENCES Tenant(id) ON DELETE CASCADE
);
```

Indexes:
- `User(tenantId)`, `UserRole(roleId)`, `RolePermission(permissionId)`,
  `TenantFeature(tenantId)`, `Permission(key)` (unique via constraint).

Design notes / adaptations:
- No existing tables are modified (backward-compatible).
- A **single default tenant** is seeded; multi-restaurant routing is **not** built now,
  but the schema supports it (test "Restaurant A vs B" is therefore N/A until multi-tenant
  exists).
- `User.passwordHash` holds a PBKDF2-SHA256 hash (see §11 security); login is
  username + password (local POS, no email). Migration `013_username_password.sql`
  added `username` (unique, backfilled from `name`) and renamed `pinHash` → `passwordHash`.
- Tokens: signed HMAC token issued at login; secret generated per install and stored in
  the `Settings` table (`tokenSecret` column added via migration) so a fresh DB creates a
  fresh secret.

---

## 7. API Design (proposed)

### New endpoints

| Method | Route | Purpose | Auth | Permission | Feature |
|---|---|---|---|---|---|
| POST | `/api/auth/login` | username+password login → access bundle `{token,user,roles,permissions,features}` | none (rate-limited) | — | — |
| GET | `/api/auth/me` | Current user's effective access | Bearer token | — | — |
| GET | `/api/permissions` | List system permissions (read-only) | token | roles.manage | — |
| GET | `/api/roles` | List roles | token | roles.manage | — |
| GET | `/api/roles/{id}` | Role detail | token | roles.manage | — |
| GET | `/api/roles/{id}/permissions` | Role's permission ids | token | roles.manage | — |
| PUT | `/api/roles/{id}/permissions` | Set role permissions `{permissionIds:[…]}` | token | roles.manage | — |
| POST | `/api/roles` | Create custom role | token | roles.manage | — |
| PUT | `/api/roles/{id}` | Update role (system roles read-only) | token | roles.manage | — |
| DELETE | `/api/roles/{id}` | Delete custom role | token | roles.manage | — |
| GET | `/api/users` | List users | token | users.manage | — |
| POST | `/api/users` | Create user (name, username, password, roleIds) | token | users.manage | — |
| PUT | `/api/users/{id}` | Update user / username / password / role / active | token | users.manage | — |
| GET | `/api/tenant/features` | Tenant feature flags | token | settings.view | — |
| PUT | `/api/tenant/features` | Set features `{features:[{key,enabled}]}` | token | settings.update | — |

### Modified endpoints (add authentication + permission checks)

| Method | Route | Required Permission | Feature check |
|---|---|---|---|
| GET | `/api/products*` | products.view | — |
| POST | `/api/products` | products.create | — |
| PUT | `/api/products/{id}` | products.update | — |
| DELETE | `/api/products/{id}` | products.delete | — |
| POST | `/api/products/{id}/units` | products.update | multiple_units |
| PUT/DELETE | `/api/products/{id}/units/{unitId}` | products.update | multiple_units |
| POST/DELETE/PUT | `/api/products/…/barcodes*` | products.update | multiple_barcodes |
| GET | `/api/invoices*` | invoices.view | — |
| POST | `/api/invoices` | invoices.create + **conditional** discounts.product / discounts.invoice / price.override | product_discount / invoice_discount / wholesale_price / price_override |
| GET | `/api/reports/low-stock` | reports.view | low_stock_report |
| POST | `/api/printing/print` | printing.receipt | receipt_printing |
| POST | `/api/printing/print-barcode` | printing.barcode | barcode_printing |
| GET | `/api/license` | open (boot gate) | — |
| POST | `/api/license/unlock` | **fix**: server-side secret, not "2004" client-side | — |

### Invoice-create conditional authorization (the money endpoint)

When creating an invoice, the backend inspects the payload:

```
items[].discountType/value present          → require feature product_discount AND permission discounts.product
invoice.discountType/value (or discount>0)  → require feature invoice_discount AND permission discounts.invoice
priceMode == "wholesale"                    → require feature wholesale_price
items[].unitPrice != originalUnitPrice      → require feature price_override AND permission price.override
```

Failure → `403 Forbidden` (or `403 FeatureDisabled`). This guarantees §19: the frontend
hiding is not the security boundary.

---

## 8. Frontend Architecture (proposed)

New files:
- `src/lib/auth/constants.ts` — `PERMISSIONS` and `FEATURES` `as const` objects.
- `src/features/auth/store.ts` — zustand store: `{ token, user, roles, permissions, features }`.
- `src/lib/auth/access.ts` — `hasPermission(p)`, `hasFeature(f)`, `can(p, f)` helpers.
- `src/components/auth/permission-guard.tsx` — `<PermissionGuard permission="…">`, `<FeatureGuard feature="…">`.
- `src/components/common/login-screen.tsx` — username+password login screen (used by `AuthGate`, `LicenseGate`).
- `src/lib/api.ts` — inject `Authorization: Bearer <token>`; add auth/roles/users/features wrappers.

Modified:
- `[locale]/layout.tsx` — auth provider + route guard around children.
- `dashboard-layout.tsx` — hide/disable nav items without permission; add Settings group.
- `pos-client.tsx` — gate wholesale toggle, line-discount editor, invoice-discount input, price override.
- `products-client.tsx` / `product-form.tsx` — gate create/edit/delete buttons + barcode/unit sections.
- `invoices-client.tsx` / `page.tsx` / dashboard — gate via `invoices.view`.

Access flow (matching `task.md` §13):

```ts
const canProductDiscount =
  hasFeature(FEATURES.PRODUCT_DISCOUNT) &&
  hasPermission(PERMISSIONS.DISCOUNTS_PRODUCT);
```

---

## 9. Roles & Permissions UI (proposed)

Under `Settings` (sidebar group):

1. **Users** — list, create, edit, activate/deactivate, assign role, set/reset password.
2. **Roles & Permissions** —
   - Roles list: name, description, user count, permission count, actions.
   - Role editor: name + description + permissions grouped by module (Orders/Products/Discounts/Reports/Printing…) with checkboxes.
   - System roles (Admin) locked from deletion; Admin role permission set immutable.
3. **Features** (tenant config) — checkbox list of features, independent of roles.

No permission-CRUD UI (permissions are system-defined seeds).

---

## 10. Migration / Seed Strategy

- `009_auth_schema.sql` — create `Tenant`, `User`, `Role`, `Permission`, `UserRole`,
  `RolePermission`, `TenantFeature` (+ `Settings.tokenSecret` column).
- `010_seed_permissions.sql` — insert all `Permission` rows (INSERT OR IGNORE, idempotent).
- `011_seed_roles.sql` — insert `Admin`, `Manager`, `Cashier` + `RolePermission` mappings
  (INSERT OR IGNORE).
- `012_seed_tenant.sql` — insert default tenant + all `TenantFeature` rows enabled +
  default Admin user (password hashed; default documented as e.g. `1234`, must be changed
  on first login).
- `013_username_password.sql` — add `User.username` (unique, backfilled from `name`,
  `user-admin` → `admin`) and rename `pinHash` → `passwordHash`; the 012 seed hash
  remains valid, so the default login is username `admin` / password `1234`.

Idempotency: use `INSERT OR IGNORE` on unique keys; the existing `MigrationRunner`
applies files in order and records them in `__Migrations`.

---

## 11. Security Risks Found (current project)

| # | Risk | Severity | Where | Proposed fix |
|---|---|---|---|---|
| 1 | **No authentication** — every API endpoint is open | High | all controllers | Bearer-token auth + `RequirePermission` |
| 2 | **License unlock has no real verification** — frontend hardcodes code `"2004"`; backend unlocks any submitted `machineId` | High | `features/license/actions.ts`, `LicenseController.Unlock` | server-side unlock code / admin-gated unlock; remove hardcoded code from bundle |
| 3 | **CORS `AllowAll`** — any website opened on this machine can call the APIs from the browser | High | `Startup.cs` | restrict CORS origins to local app (or drop CORS for API) |
| 4 | **No authorization** — a cashier can `DELETE /api/products/{id}` via direct request | High | `ProductsController` | `RequirePermission("products.delete")` |
| 5 | **Discounts / price override not permission-gated** — financial operations can be submitted with any discount via direct API | High | `InvoicesController.Create` | conditional permission+feature checks on the payload |
| 6 | Hardcoded `http://localhost:3001` fetch in `products-client.tsx` bypasses the api client | Low | `products-client.tsx:297` | use `api.printing.printBarcode` |
| 7 | Unlock code embedded in shipped JS bundle | Medium | `features/license/actions.ts` | move to backend secret |
| 8 | Brute-force protection on login is username-based | Medium | `AuthService.Login` | rate-limit / lockout after N attempts (implemented: 5 fails → 60s lockout per username) |
| 9 | No HTTPS / plain HTTP on localhost | Low | OWIN host | acceptable for LAN-only POS; document |
| 10 | `MachineId` derivable from environment values → license spoofable | Medium | `MachineId.cs` | out of scope for RBAC; noted |

---

## 12. Implementation Plan (ordered by dependency)

### Phase 3 — Backend
1. `009` migration: auth schema.
2. `010` seed permissions; `011` seed roles + role-permissions; `012` seed tenant +
   features + default admin user.
3. Models + repositories (`AuthRepository`, `RoleRepository`, `UserRepository`,
   `TenantFeatureRepository`).
4. `AuthService`: login, HMAC token issue/validate, current-access bundle; middleware
   `ApiAuthenticationHandler` in OWIN pipeline.
5. `RequirePermissionAttribute` (IAuthorizationFilter) + `FeatureService` (`HasFeature`).
6. Controllers: `AuthController`, `PermissionsController` (read-only), `RolesController`,
   `UsersController`, `TenantFeaturesController`.
7. Guard existing controllers (`Products`, `Invoices`, `Reports`, `Printing`).
8. Invoice-create conditional discount/price-mode/override authorization.

### Phase 4 — Frontend
9. `src/lib/auth/constants.ts`, `src/features/auth/store.ts`, `src/lib/auth/access.ts`,
   guards.
10. `api.ts` — auth header injection + new wrappers.
11. Login page (username+password) + layout auth provider/guard.
12. Gate POS discount/wholesale/price-override UI.
13. Gate products/invoices/report visibility + nav items.

### Phase 5 — Administration UI
14. Settings layout; Users page; Roles & Permissions page; Features page.

### Phase 6 — Testing
15. Build backend (`dotnet build backend-cs/pos-cs.csproj`) + frontend (`npm run build`).
16. Scenario tests per `task.md` §21 Phase 6:
    - Manager/Cashier × feature on/off
    - user without/with permission
    - direct API without/with permission (verify 403 on hidden actions)
    - multi-restaurant isolation — **N/A** (single-tenant now); schema ready.
17. Docs: `docs/PERMISSIONS.md`, `docs/FEATURES.md` kept in sync with any change.

---

## Summary Counts (actual)

| Metric | Count |
|---|---|
| Total Features | 9 |
| Total Permissions | 19 |
| Total Roles | 3 (Admin, Manager, Cashier) |
| Features requiring permissions | 9 (every feature gates a sensitive action) |
| Frontend-only features | 0 (all have backend impact) |
| Backend-enforced permissions | 17 (all except `license.view` boot gate + `reports.export` future) |
