# Module Audit - 2026-09-04

## Purpose

This is an evidence-based review of the current POS modules. It separates confirmed defects from delivery risks and proposed scope, so the backlog can be implemented without treating ideas as bugs.

## Review Scope

- Frontend: Next.js static export in `src/`.
- Backend: .NET Framework 4.8 OWIN API in `backend-cs/`.
- Data access: Dapper and SQLite.
- Review date: 2026-09-04.

## Confirmed Findings

| ID | Priority | Finding | Evidence | User impact | Recommended resolution |
| --- | --- | --- | --- | --- | --- |
| BUG-001 | P0 | The invoices quick filter labeled `all` only returns invoices from the current day. | The UI clears both dates in `src/app/[locale]/invoices/invoices-client.tsx`; `ResolveRange` defaults empty dates to today in `backend-cs/Pos.Application/Services/InvoiceService.cs`. | Finance and management views can silently omit historical invoices. | Define an explicit unbounded range contract (for example `range=all`) and add API/service/UI tests. Preserve the current default-to-today behavior only for callers that explicitly request it. |
| BUG-002 | P1 | Legacy printing endpoints return `200 OK` without printing anything. | `POST /api/printing/receipt` and `POST /api/printing/barcode` are stub methods in `backend-cs/Controllers/PrintingController.cs`. | An old client or integration may record a print as successful although no receipt or label was produced. | Remove the routes after checking consumers, or return `410 Gone` with a migration message. Do not report a successful print for a no-op. |

## Delivery Risks

| ID | Priority | Risk | Evidence | Recommended action |
| --- | --- | --- | --- | --- |
| RISK-001 | P1 | Frontend regression coverage is too narrow for a POS workflow. | The frontend currently has one focused test file: `src/app/[locale]/pos/_components/utils/pricing.test.ts`. | Add tests for checkout, invoice filters, permissions/features, drafts, returns, and error handling before expanding major functionality. |
| RISK-002 | P1 | The release build depends on globally available `npx`. | `build-static.ps1` invokes `npx next build`. In the review environment `npm` and `npx` were unavailable on PATH. | Use the project package script or resolve the local executable explicitly; document the supported Node/npm setup and add CI validation. |
| RISK-003 | P2 | Session tokens are persisted in browser `localStorage`. | `src/lib/auth-storage.ts` stores the whole session locally. | For the current local desktop deployment, document this as an accepted constraint. Before any network-accessible deployment, move to a protected/native store or a hardened cookie/session approach. |
| RISK-004 | P2 | Generated output is present in the working tree. | `.next/`, `out/`, `backend-cs/bin/`, and `backend-cs/obj/` exist locally, though they are not tracked by Git. | Keep release artifacts separate from source changes and add a clean-build verification step to release documentation. |

## Module Status

| Module | Status | Review notes | Next focus |
| --- | --- | --- | --- |
| Authentication and authorization | Strong foundation | Backend permission checks and tenant feature gates are present; passwords and signed expiry-based tokens are implemented. | Add authentication failure/session-expiry tests and choose an explicit token-storage policy. |
| Catalog: products, categories, brands, units | Ready for operations | Supports POS hiding, units, and multiple barcodes behind features. | Bulk import, scanner verification, and label-printing user flows. |
| POS sales and invoices | Core complete, needs fixes | Draft/post lifecycle, client selection, payment method, shifts, discounts, and FIFO integration exist. | Fix `BUG-001`, test checkout failures/retries, add stronger invoice search. |
| Purchases and suppliers | Core complete | Purchase lifecycle and supplier statements are documented as shipped. | Add purchase-order workflow and receiving variance handling. |
| Returns | Core complete | Sales and purchase returns are linked to posted originals, with stock/cost reversal. | Add tests for partial returns and reporting reconciliation. |
| Inventory and costing | Good foundation | Stock ledger and FIFO cost layers are established. | Inventory adjustments with approvals/reasons, expiry/batch tracking if required. |
| Shifts, expenses, and payments | Core complete | Active shift enforcement occurs on the server; cash computation is available at close. | Cash-drawer journal and end-of-day printable close report. |
| Reports | Operational baseline | Sales, purchases, inventory, profit, returns, expenses, cash, and low-stock reports are available. | Dashboard widgets, aging reports, scheduled/exportable reporting. |
| Printing | Modern path exists, legacy path unsafe | Feature-gated receipt and barcode print APIs exist, alongside the no-op legacy endpoints. | Resolve `BUG-002`, add printer/offline/error contract tests. |
| Tenant features, roles, and users | Good foundation | Feature switches are separated from user permissions and managed by tenant settings. | Audit trail for configuration/role changes. |
| Licensing and health | Present | Controllers are available for both concerns. | Add monitoring/diagnostics around startup, database availability, and printing. |

## Proposed Modules

These are candidates, not committed scope. The recommended order is based on operational value and dependency risk.

| Module | Priority | Value | Main dependencies | Initial vertical slice |
| --- | --- | --- | --- | --- |
| Audit log | P1 | Makes financial, stock, permission, and configuration changes traceable. | User identity, database migration, permissions. | Record and view invoice cancellation, return, price override, and role changes. |
| Backup and restore | P1 | Protects the business from device loss, corruption, and failed upgrades. | SQLite file lifecycle, authorization, release packaging. | Create a timestamped backup with validation and restore confirmation. |
| Inventory adjustments | P1 | Allows controlled correction of physical versus recorded stock. | Stock movement ledger, roles, reports. | Create approved adjustment with reason; write an immutable stock movement. |
| Alerts center | P2 | Surfaces low stock, expiring items, failed printing, and backup failures. | Reports, settings, optional scheduler. | In-app low-stock alerts with acknowledgment state. |
| Purchase orders | P2 | Separates ordering from receiving and helps manage suppliers. | Suppliers, products, purchases. | Draft and approve a purchase order, then convert it to a purchase invoice. |
| Customer and supplier aging | P2 | Exposes overdue balances for collections and payment planning. | Clients, suppliers, payments, reports. | Receivables aging report by 0-30, 31-60, 61-90, 90+ days. |
| Cash drawer journal | P2 | Improves cashier accountability during a shift. | Shifts, expenses, payments, users. | Record paid-in/paid-out with reason and show a shift timeline. |
| Expiry and batch tracking | P3 | Reduces waste and supports regulated inventory. | Purchases, FIFO, stock movements, barcode model. | Capture batch and expiry at purchase receipt and show an expiry report. |
| Multi-warehouse | P3 | Supports stock by location once a single-location workflow is stable. | Inventory schema, all stock transaction flows, reports. | Allocate stock to one additional warehouse with transfer movements. |

## Review Limitations

- This was a static code review; no real printer, scanner, or production database was available.
- Frontend verification could not run because `npm`/`npx` were not available on PATH in the review environment.
- Backend test execution was blocked by local SDK directory permissions, not by an observed test failure.

