# POS System Release Notes

# Version 1.5.0

Release Name

Roles, Permissions & Feature-Based Access Control

---

## Overview

This release introduces a full authorization architecture to the POS:

* **Authentication** - username + password login (replaces the PIN code).
* **Roles** - Admin, Manager, Cashier, plus custom roles.
* **Permissions** - 19 permission keys enforced on both backend and frontend.
* **Tenant features** - 9 per-restaurant capability switches combined with
  permissions (`AND`) so a capability is usable only when both are granted.
* **Settings screens** - Users, Roles, Permissions and Features management.

The default login is **`admin` / `1234`**; change it after the first login.

---

# Added Features

## Authentication (username + password)

The PIN-code login is replaced with username + password authentication.

* Passwords are hashed with PBKDF2 (10 000 iterations, 16-byte salt).
* Login attempts are rate-limited: 5 failures lock the account for 60 seconds.
* Sessions are bearer tokens; every API call is authenticated by middleware
  and permission-checked per endpoint.
* Migration `013_username_password` migrates existing PIN data and seeds
  `admin` / `1234`.

## Roles

Three system roles are seeded (cannot be renamed or deleted):

| Role    | Capabilities                                                             |
| ------- | ------------------------------------------------------------------------ |
| Admin   | Everything, incl. users, roles, settings, license, features              |
| Manager | All selling and product operations, discounts, price override, reports   |
| Cashier | View products/invoices, create invoices, print receipts                  |

New custom roles can be created and deleted from Settings > Roles, with a
per-permission matrix.

## Permissions

19 permission keys across 10 resources. Each is enforced server-side by the
`RequirePermission` attribute and mirrored in the UI (menus, buttons, sections
are hidden without the permission). See `docs/PERMISSIONS.md` for the full list.

## Tenant Features

9 feature switches controlled in Settings > Features. Features are tenant-wide;
they combine with permissions (`AND`). Disabling a feature hides the related UI
and blocks the endpoint server-side even for users with the permission. See
`docs/FEATURES.md` for the full catalog.

## Settings Screens

* **Users** - create/edit users, assign roles, activate/deactivate accounts.
* **Roles** - create/edit/delete roles and assign permissions.
* **Permissions** - grouped reference of all permission keys.
* **Features** - per-tenant feature toggles.

Changes refresh the active session immediately (no re-login required).

---

# Backend Improvements

* New endpoints: `POST /api/auth/login`, `GET /api/auth/me`, and CRUD for
  `/api/users`, `/api/roles`, `/api/roles/{id}/permissions`,
  `/api/permissions`, and `GET/PUT /api/tenant/features`.
* Existing endpoints are now permission-gated (e.g. `products.update` +
  `multiple_units` for unit management, `printing.receipt` + `receipt_printing`
  for printing).
* License unlock requires `license.manage` and logs the acting user.
* CORS is restricted to localhost development origins (production is
  same-origin).

---

# Frontend Improvements

* Login screen with username + password and clear error messages.
* Sidebar navigation filtered by permission, so users only see pages they can
  use.
* Products screen: units/barcodes sections hidden when their feature is off;
  actions gated by `products.*` permissions.
* POS screen: line-edit button gated by price-override/discount permissions;
  unit picker and barcode linking gated by their features.
* All new settings screens fully translated to Arabic (RTL).

---

# Database Changes

Migrations `009_auth_schema.sql` through `013_username_password.sql`:

* `User`, `Role`, `Permission`, `RolePermission`, `UserRole`,
  `Tenant`, `TenantFeature` tables.
* Seeds for the 3 system roles, 19 permissions, and 9 tenant features.
* Replaces `pinHash` with `username` + `passwordHash` (PBKDF2) and seeds the
  default `admin` account.

---

# Compatibility

Database Migration Required

Yes (`009` through `013` run automatically on startup)

Frontend Update Required

Yes

Backend Update Required

Yes

API Changes

Additive plus authenticated endpoints (bearer token required except
`/api/auth/login` and `/api/license`)

Breaking Changes

- PIN login removed in favor of username + password (`admin` / `1234`).
- All API endpoints (except public ones above) now require a bearer token.
