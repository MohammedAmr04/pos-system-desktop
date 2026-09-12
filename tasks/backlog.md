# Product and Technical Backlog

## How to Use This Backlog

- `P0`: incorrect financial/operational result; fix before the next release.
- `P1`: important safety, reliability, or business-continuity work.
- `P2`: valuable operational improvement.
- Every completed item needs automated verification and a manual Arabic RTL check where it has a user interface.

## Now: Correctness and Release Safety

### BL-001 - Fix the invoice `all` date filter

**Priority:** P0  
**Dependencies:** None  
**Scope:** Small

**Problem:** Selecting `all` clears dates in the UI, but the service interprets missing dates as today.

**Acceptance criteria:**
- [ ] Selecting `all` returns invoices across the full retained history.
- [ ] Today/week/month/custom filters keep their present inclusive date behavior.
- [ ] The API contract distinguishes an unbounded range from a default date range.
- [ ] Unit or integration tests cover all five filter states.

**Likely files:**
- `src/app/[locale]/invoices/invoices-client.tsx`
- `src/api/invoices.ts`
- `backend-cs/Controllers/InvoicesController.cs`
- `backend-cs/Pos.Application/Services/InvoiceService.cs`
- `backend-cs/Pos.Tests/`

### BL-002 - Retire unsafe legacy print routes

**Priority:** P1  
**Dependencies:** Confirm whether another installed client calls the endpoints  
**Scope:** Small

**Acceptance criteria:**
- [ ] No endpoint returns a successful print response without attempting a print.
- [ ] Existing supported clients use `/api/printing/print` and `/api/printing/print-barcode`.
- [ ] Removed endpoints return a documented migration error, or remain only if fully implemented and tested.

**Likely files:**
- `backend-cs/Controllers/PrintingController.cs`
- `src/api/`
- `backend-cs/Pos.Tests/`

### BL-003 - Make the release build self-contained

**Priority:** P1  
**Dependencies:** Node version decision  
**Scope:** Small

**Acceptance criteria:**
- [ ] A fresh supported machine can build without relying on a global `npx` installation.
- [ ] The documented build command and `build-static.ps1` use the same toolchain.
- [ ] CI or a local clean-build check validates frontend and backend artifacts.

**Likely files:**
- `build-static.ps1`
- `package.json`
- `README.md`
- CI configuration, if introduced

## Next: Test Coverage and Operational Trust

### BL-004 - Add POS and invoice regression tests

**Priority:** P1  
**Dependencies:** BL-001  
**Scope:** Medium

**Acceptance criteria:**
- [ ] Invoice filter, draft/resume, posting, payment, discount, and canceled-invoice behaviors are covered.
- [ ] Failed API requests produce a clear recoverable UI state.
- [ ] Permission/tenant-feature gates are tested in both UI and API paths.

### BL-005 - Audit log, phase 1

**Priority:** P1  
**Dependencies:** role/user identity and database migration  
**Scope:** Medium

**Acceptance criteria:**
- [ ] The system records actor, action, entity, timestamp, and before/after summary where applicable.
- [ ] Invoice cancellation, returns, price overrides, role changes, and feature changes are recorded.
- [ ] Authorized administrators can filter and view the log in Arabic RTL.

### BL-006 - Backup and restore, phase 1

**Priority:** P1  
**Dependencies:** release packaging and authorization policy  
**Scope:** Medium

**Acceptance criteria:**
- [ ] An authorized user can create a timestamped database backup.
- [ ] Backup integrity is validated before reporting success.
- [ ] Restore requires explicit confirmation and creates a pre-restore safety backup.
- [ ] The action appears in the audit log.

### BL-007 - Inventory adjustments

**Priority:** P1  
**Dependencies:** stock movement ledger, permissions, audit log design  
**Scope:** Medium

**Acceptance criteria:**
- [ ] Authorized users can increase or decrease stock with a mandatory reason.
- [ ] Every adjustment creates an immutable stock movement and audit event.
- [ ] Inventory and profit reports reconcile after an adjustment.

## Later: High-Value Product Modules

### BL-008 - Alerts center

**Priority:** P2  
**Dependencies:** low-stock report and settings  
**Scope:** Medium

**Acceptance criteria:**
- [ ] Low-stock alerts are shown in an in-app list.
- [ ] Users can acknowledge an alert without changing stock data.
- [ ] Alert creation and acknowledgment are testable.

### BL-009 - Purchase orders

**Priority:** P2  
**Dependencies:** suppliers, products, purchases  
**Scope:** Large; split into order lifecycle and purchase conversion before implementation.

**Acceptance criteria:**
- [ ] User can create, edit, approve, and cancel a purchase order.
- [ ] Approved orders can be converted into a purchase invoice with quantity validation.
- [ ] Status changes are permission-protected and audited.

### BL-010 - Customer and supplier aging reports

**Priority:** P2  
**Dependencies:** payments, client/supplier statements  
**Scope:** Medium

**Acceptance criteria:**
- [ ] Receivables and payables are grouped into configurable aging buckets.
- [ ] Report totals reconcile with account statements.
- [ ] Export/print behavior uses the existing reporting conventions.

### BL-011 - Cash drawer journal and end-of-day report

**Priority:** P2  
**Dependencies:** shifts, expenses, payments, printer feature  
**Scope:** Medium

**Acceptance criteria:**
- [ ] Paid-in and paid-out entries require amount, reason, and actor.
- [ ] Shift close includes a readable movement timeline and expected-versus-actual cash.
- [ ] A printable end-of-day summary is available when printing is enabled.

### BL-012 - Expiry and batch tracking

**Priority:** P3  
**Dependencies:** purchase receipt model, inventory costing  
**Scope:** Large; require a data-model decision before implementation.

## Explicitly Deferred

- Multi-warehouse should wait until inventory adjustments are stable and auditable.
- Multi-branch should wait until backup/restore, auditing, and a concurrency strategy are established.
- Complex accounting/GL remains outside current product scope unless business requirements change.

