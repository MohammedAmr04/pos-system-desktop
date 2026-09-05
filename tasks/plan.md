# Implementation Plan: Stabilization and Operational Modules

## Goal

Fix the confirmed correctness problems first, then add the operational controls that make the POS safer to run: test coverage, auditability, backup, and stock adjustments. New commercial modules follow only after that foundation is verified.

## Architecture Decisions to Confirm

- Date filtering must represent `all` explicitly; absence of dates cannot mean both "today" and "unbounded".
- Print APIs must report the real result of a print attempt. Legacy no-op success routes are not acceptable.
- Audit records should be append-only and written in the same application operation as the business event whenever practical.
- Stock corrections should use the existing stock-movement ledger, not mutate product quantity directly.
- Backup/restore must be restricted to a dedicated administrative permission and never be triggered from a regular cashier flow.

## Phase 1: Correctness and Release Safety

- [ ] BL-001: Fix invoice `all` filter.
- [ ] BL-002: Retire or correctly implement legacy print routes.
- [ ] BL-003: Make the release build self-contained.

### Checkpoint: Phase 1

- [ ] Frontend type check, lint, tests, and build pass in a documented Node environment.
- [ ] Backend test suite passes in a documented .NET SDK environment.
- [ ] Manual check confirms each invoice date preset returns the expected data.
- [ ] Manual check confirms printing displays success only after a real print operation succeeds.

## Phase 2: Regression Safety and Governance

- [ ] BL-004: Add POS and invoice regression tests.
- [ ] BL-005: Implement audit log phase 1.
- [ ] BL-006: Implement backup and restore phase 1.

### Checkpoint: Phase 2

- [ ] An administrator can trace a canceled invoice, a return, and a role change.
- [ ] A backup can be created, validated, and restored in a non-production test copy.
- [ ] Authorization tests deny sensitive actions to a cashier role.

## Phase 3: Inventory Operations

- [ ] BL-007: Implement inventory adjustments.
- [ ] BL-008: Implement alerts center, starting with low stock.
- [ ] BL-011: Implement cash drawer journal and end-of-day report.

### Checkpoint: Phase 3

- [ ] A stock adjustment appears consistently in the stock ledger and inventory reports.
- [ ] A shift close explains every cash movement and reconciles expected cash.
- [ ] Alert state does not alter inventory quantities or report values.

## Phase 4: Commercial Expansion

- [ ] BL-009: Implement purchase orders as two separate deliverables: lifecycle, then purchase conversion.
- [ ] BL-010: Implement customer and supplier aging reports.
- [ ] BL-012: Produce a data-model ADR for expiry/batch tracking before implementation.

## Suggested Delivery Order

1. BL-001, because it can hide historic financial data.
2. BL-002 and BL-003, because false print success and an unreliable release command harm day-to-day operations.
3. BL-004, so the next changes have a regression safety net.
4. BL-005 and BL-006, to make the system traceable and recoverable.
5. BL-007, then the P2 modules that rely on its operational data.

## Risks and Mitigations

| Risk | Impact | Mitigation |
| --- | --- | --- |
| Existing external client calls a legacy print route. | High | Search packaged/released clients and provide a migration response before removal. |
| Backup restore corrupts or replaces live data. | High | Require confirmation, create a safety backup, validate the selected file, and document a recovery procedure. |
| Audit logging slows critical POS operations. | Medium | Keep the event schema compact and write within the same database transaction when appropriate. |
| Inventory adjustments bypass FIFO/reporting rules. | High | Reuse the stock ledger and add reconciliation tests. |
| Scope expands into multi-warehouse too early. | Medium | Do not begin it until adjustments, auditing, and backup/restore meet their acceptance criteria. |

## Open Decisions for Product Review

- Who is allowed to restore a backup: owner only, or a dedicated administrator role?
- Is stock adjustment approval required, or can a manager post immediately?
- What retention period is required for audit logs and backups?
- Is expiry/batch tracking a legal requirement or a future optimization?

