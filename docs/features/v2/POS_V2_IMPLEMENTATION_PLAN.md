# POS V2 — Implementation Plan

> **Status:** Approved Plan / Ready for Implementation
> **Source spec:** `docs/features/v2/POS_ADVANCED_INVENTORY_PURCHASING_SPEC.md`
> **Delivery mode:** Sequential phases — each phase is built, tested, and delivered independently in the order below.

---

## 1. Decisions Locked With Stakeholders

| # | Decision | Outcome |
|---|---|---|
| 1 | Sales Returns data modeling | **Separate tables** (`SalesReturn` + `SalesReturnItem`) with their own transaction number and `OriginalInvoiceId` — never mixed into the sales `Invoice` table |
| 2 | Sales Draft support | **Full Draft in v2** — POS can save drafts, list them, resume, and post later |
| 3 | Shifts / Cash sessions | **Included in v2** — v1 has no shift concept; a full open/close shift + Expected Cash feature will be built (Phase 9) |
| 4 | Delivery strategy | **Sequential phases** — no big-bang release |

---

## 2. Current State (v1) vs Target (v2) — Gap Analysis

| Area | v1 (current) | v2 (target) | Gap |
|---|---|---|---|
| Products | Product + ProductUnit (free-text `unitName`) + unit-scoped barcodes | Same foundation + link to a shared Unit master | Modify |
| Category / Brand | Not present | Master data, nullable FK on Product, deactivate-only semantics | New |
| Units | Free-text names per product, quantity factors, per-unit retail/wholesale prices | Independent Unit master; ProductUnit references it | Modify + New |
| Supplier / Client | Not present | Master data + derived balances | New |
| Purchases | Not present (stock edited manually on the product) | Purchase Invoices: Draft/Posted/Cancelled | Entirely new |
| FIFO / Costing | Single mutable `Product.buyPrice`; flat snapshot in `InvoiceDetail.buyPrice` | Cost Layers + FIFO consumption + historical `TotalCost` per sales line | Entirely new |
| Sales lifecycle | Immediate final insert, no status | Draft/Posted/Cancelled + Returns | Major change |
| Payments | None (implicit cash) | Independent Payment transactions + balances + derived payment status | New |
| Returns | None | Sales Returns + Purchase Returns (Full/Partial) | New |
| Expenses | None | Expenses + categories + shift linkage | New |
| Shifts | None | Open/Close shift, Expected Cash, cashier cash accountability | Entirely new |
| Reports | Low-stock only | Sales / Purchases / Inventory / Profit / Returns / Expenses / Cash | Major expansion |
| Printer Settings | Hardcoded in backend | Configurable settings screen | New |

### Architectural notes discovered during review

1. **`InvoiceDetail.quantityFactor` is not persisted** — it is only used at write time for stock math. It must become a DB column so returns and historical FIFO reconstruction can convert quantities back to base units.
2. **Stock decrement currently lives inline inside `InvoiceRepository.Create`** — it must be extracted into a unified **Stock Movement ledger** so sales, purchases, both return types, and adjustments all share one inventory mechanism.
3. Migrations are numbered `.sql` files applied at startup by `MigrationRunner`; v2 continues from `014_`.
4. Per the spec (§39): **no Reports before the transaction + costing model is stable** — the phase order below enforces this.

---

## 3. Phase Plan

Every phase ships: migrations + backend (Domain → Application → Infrastructure → Controller → CompositionRoot) + frontend screens + permission seeds, and ends with full verification.

### Phase 1 — Master Data: Categories, Brands, Units
- **DB:** `014_categories.sql`, `015_brands.sql`, `016_units.sql`
  - New tables: `Category`, `Brand`, `Unit` (id, name, …, isActive, timestamps)
  - `Product.categoryId` / `Product.brandId` (nullable — NULL means genuinely unassigned; no fake "Other" rows)
  - `ProductUnit.unitId` + backfill of the Unit master from existing free-text `unitName` values
  - Permission seeds + feature keys: `categories`, `brands`
- **Backend rules:** Categories can never be deleted (deactivate only); Brands deletable only when unreferenced; used Units cannot be deleted; Base Unit protected. Deactivation never clears `Product.categoryId/brandId` — the UI resolves inactive/missing references to "Other". Reactivation restores the original relationship automatically.
- **Frontend:** 3 management screens + Product form pickers (Category/Brand/Unit from master) + "Other" display for null/inactive.

### Phase 2 — Suppliers & Clients
- **DB:** `017_suppliers_clients.sql` + permission seeds
- **Backend:** CRUD for both; statement/balance endpoint per party (becomes fully meaningful after Phase 5). Supplier required for credit purchases; Client required for credit sales; both optional for cash. Referenced parties are deactivated, not deleted.
- **Frontend:** Supplier and Client management screens + account-statement view per party.

### Phase 3 — Stock Movement Ledger + Purchase Invoices
- **DB:** `018_stock_movements.sql`, `019_purchases.sql`
  - `StockMovement` ledger (productId, quantity in base units, type, reference, timestamp)
  - `PurchaseInvoice` + `PurchaseInvoiceItem`; lifecycle columns on invoices (`status`, `type`, `userId`, …)
- **Backend:**
  - Extract inventory mutation out of `InvoiceRepository.Create` into the shared StockMovement mechanism (sale/purchase/returns/adjustments all pass through it)
  - Purchase flow: **Draft** (editable, zero side-effects) → **Posted** (stock in, FIFO layers — wired in Phase 4, selling prices updated if entered, supplier balance impact) → **Cancelled** (safe reversal, nothing deleted)
  - Editing a Posted purchase = transactional reversal + re-application — never a direct line update
  - Supplier optional for cash purchases, required for credit
- **Frontend:** purchases list (status filter), create/edit screen (lines, units, purchase cost, retail/wholesale selling prices per unit, optional supplier), details view.

### Phase 4 — FIFO Cost Layers + Sales Integration
- **DB:** `020_cost_layers.sql`
  - `CostLayer` (productId, sourcePurchaseId, quantityReceived, quantityRemaining, unitCost, createdAt)
  - `SaleCostAllocation` (audit detail of which layers each sales line consumed)
  - `InvoiceDetail.totalCost` + persist `InvoiceDetail.quantityFactor`
- **Backend:** pure domain rule `FifoAllocator` (+ xUnit tests): convert sold quantity to base units before consumption; one sales line may consume multiple layers; store historical `TotalCost` on the line. Old invoices never change when the current product cost changes.
- **Frontend:** no new screens — cost/profit visibility in invoice details.

### Phase 5 — Payments & Balances
- **DB:** `021_payments.sql` + `payments.*` permission seeds
- **Backend:** independent `Payment` entity (amount, method, date, nullable invoiceId/clientId/supplierId, reference, notes). Payment status (Paid / Partially Paid / Unpaid) is **derived from totals**, never a mutable status field. Supplier and Client balances derived from invoices + payments + returns.
- **Frontend:** record-payment dialog (reachable from invoice / supplier / client), payment-status badges in lists, payments list screen.

### Phase 6 — Sales Invoice Lifecycle + Full Draft
- **DB:** `022_sales_lifecycle.sql` — `Invoice.status` (`draft`/`posted`/`cancelled`; existing rows backfilled to `posted`), `clientId`, `paymentMethod`, `userId`
- **Backend:** Draft = editable, no stock/FIFO/revenue/client-balance effects; Posted = full pipeline (convert → consume FIFO → store TotalCost → deduct stock → payment/client balance); Cancelled = safe reversal. Client mandatory for credit sales, optional for cash.
- **Frontend POS changes:** save-draft / resume-draft / drafts list, client picker, payment method selection, decimal quantities, status badges in the invoices list.

### Phase 7 — Sales Returns
- **DB:** `023_sales_returns.sql` — separate `SalesReturn` + `SalesReturnItem` tables, own numbering, `OriginalInvoiceId` mandatory
- **Backend:** validate returnable quantity (sold − already returned); restore stock via base-unit conversion; **restore the original FIFO cost allocation** (not current cost); refund using the original payment method; original invoice transitions to Partially Returned / Fully Returned and is never modified or deleted; dedicated permission `returns.sales`.
- **Frontend:** create-return flow launched from invoice details (pick lines + quantities) + returns list screen.

### Phase 8 — Purchase Returns
- **DB:** `024_purchase_returns.sql` — `PurchaseReturn` + `PurchaseReturnItem`, `OriginalPurchaseInvoiceId` mandatory
- **Backend:** reduce stock, reverse the appropriate cost-layer effect, adjust Supplier balance, preserve the original purchase reference, dedicated permission `returns.purchase`.
- **Frontend:** create-return flow from purchase details + returns list screen.

### Phase 9 — Shifts & Cash Sessions (entirely new in v2)
- **DB:** `025_shifts.sql` — `Shift` (openedBy, openingCash, openedAt, closedAt, closingCash, expectedCash, status)
- **Backend:** open shift (with opening cash), close shift (compute Expected Cash = opening + cash sales + other cash in − cash returns − cash expenses), cash sales/payments linked to the active shift; POS selling requires an open shift; permission seeds `shifts.*`.
- **Frontend:** `/shifts/` list + shift details/report, open/close shift flows, shift indicator in POS.

### Phase 10 — Expenses
- **DB:** `026_expenses.sql` — `Expense` + `ExpenseCategory`; `Expense.shiftId` links to the **active shift**
- **Backend:** cashier creates expenses per permission; expense immediately affects Expected Cash of its shift; expenses stay strictly separate from purchases.
- **Frontend:** expenses screen + expense-category management.

### Phase 11 — Reports
- **Backend:** `/api/reports/*` aggregation endpoints over historical snapshot data only: sales (by date/product/category/brand/unit/cashier/payment method), purchases, inventory (current stock, movement, valuation), profit (`Revenue − FIFO COGS`, gross margin), returns, expenses, cash/shift reports.
- **Frontend:** reports hub `/reports/` + individual report screens with date-range filters (reusing the existing invoices date-filter pattern); `/low-stock/` moves under the reports section.

### Phase 12 — Printer Settings
- **DB:** `027_printer_settings.sql`
- **Backend:** move printer configuration out of hardcoded values (printer names per role, paper width, copies, auto-cut, cash drawer, receipt header/footer/store info).
- **Frontend:** `/settings/printing/` settings screen.

### Dependency highlights
- Phase 4 must land before any profit report.
- Phase 5 must land before account statements are useful.
- Phase 9 must land before Phase 10 (expenses attach to the active shift).
- Phase 11 comes last among data features, per spec §39.

---

## 4. Screens

### 4.1 New screens (16)

| Route | Screen | Phase |
|---|---|---|
| `/categories/` | Category management (no delete — activate/deactivate) | 1 |
| `/brands/` | Brand management | 1 |
| `/units/` | Unit management | 1 |
| `/suppliers/` + `/suppliers/[id]/` | Suppliers + account statement | 2 + 5 |
| `/clients/` + `/clients/[id]/` | Clients + account statement | 2 + 5 |
| `/purchases/` | Purchase invoice list (status filter) | 3 |
| `/purchases/new/` + `/purchases/[id]/` | Create/edit/details — lines, cost, selling prices, payment | 3 + 5 |
| `/payments/` | Payments list | 5 |
| `/sales-returns/` + create-from-invoice | Sales returns | 7 |
| `/purchase-returns/` + create-from-purchase | Purchase returns | 8 |
| `/shifts/` + `/shifts/[id]/` | Shifts (open/close + Expected Cash report) | 9 |
| `/expenses/` (+ category management) | Expenses | 10 |
| `/reports/` | Reports hub | 11 |
| `/reports/{sales,purchases,inventory,profit,returns,expenses,cash}/` | Individual reports | 11 |
| `/settings/printing/` | Printer & receipt settings | 12 |

### 4.2 Modified screens (6)

| Screen | Changes |
|---|---|
| `/products/` (form + table) | Category/Brand/Unit pickers from master data; "Other" display for null/inactive references |
| `/pos/` | Decimal quantities, client picker (mandatory for credit), payment method, save/resume drafts, open-shift guard |
| `/invoices/` + details dialog | Status/payment badges, return action, record payment, cost/profit display |
| `/settings/features/` | Toggles for `categories`, `brands` features |
| `DashboardLayout` | Sidebar regrouped into sections: Main / Inventory / Purchasing / Sales / Cash / Reports / Settings (11+ new items don't fit the current flat structure) |
| `/low-stock/` | Relocated under the Reports/Inventory nav section |

### 4.3 New permission keys
Seeded via migrations, mirrored in `PERMISSIONS`/`FEATURES` (`src/lib/constants.ts`), and granted to Admin/Manager roles:

`categories.*` · `brands.*` · `units.*` · `suppliers.*` · `clients.*` · `purchases.view|create|update|post|cancel` · `payments.*` · `returns.sales` · `returns.purchase` · `shifts.*` · `expenses.*`

New feature keys: `categories`, `brands` (added to `FeatureCatalog` backend-side as well).

---

## 5. Non-Negotiable Rules During Implementation

1. **Deactivate ≠ Delete ≠ Detach.** FK values are never cleared on deactivation or feature-disable; the UI renders missing/inactive references as "Other".
2. All stock is stored in the product's **Base Unit**; every stock change (sale, purchase, return, adjustment) passes through the unified StockMovement ledger.
3. **Historical snapshots are sacred:** sale price, total cost, unit info, and `quantityFactor` are stored at transaction time — never recomputed from current master data.
4. Every multi-step write is a **single transactional port method** (per backend conventions).
5. Every list screen uses **server-side filter/sort/pagination** through `src/lib/api.ts` (per frontend conventions) — no client-side slicing/filtering.
6. Payments are independent transactions; balances and payment statuses are always derived, never stored as mutable truth.
7. Sensitive actions (price override, returns, cancellations, payments, expenses, posted-purchase edits) write audit-log entries and re-check permissions inside the service layer.

---

## 6. Per-Phase Verification

Backend:
```
dotnet build backend-cs/pos-cs.csproj --configuration Release
```
+ xUnit tests for new domain rules (FifoAllocator, payment-status derivation, return validation, shift cash math).

Frontend:
```
npx tsc --noEmit && npm run lint && npm test && npm run build
```

---

## 7. Explicitly Out of Scope (per spec §38)

Bundle/composite products · automatic price/margin calculation · customer-specific pricing · supplier pricing rules · complex accounting/GL · multi-warehouse · manual returns without an original invoice.
