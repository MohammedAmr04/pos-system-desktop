# Implementation Plan: Multi-Branch Offline-First POS

## Overview

Extend the current POS so a central VPS manages all branches while each branch keeps a local copy of the backend and database. Cashier operations are written locally first, printed locally, and synchronized with the central server whenever connectivity is available. Manager and admin operations are performed centrally and flow down to branches.

## Architecture Decisions

- Use an offline-first model: the POS always writes to the branch Local Backend; the VPS is synchronized in the background.
- Install one immutable `BranchId` and unique `TerminalId` per branch terminal. Users are not permanently assigned to a branch; the current terminal determines the operational branch.
- Run a `BranchAgent` on each branch machine (or one always-on machine per branch) for synchronization, printing, retries, and health reporting.
- Synchronize through authenticated APIs and durable queues, never by copying SQLite files.
- Use globally unique operation/entity IDs and idempotent central endpoints so retries cannot create duplicate invoices.
- Keep central/master data (products, prices, users, permissions, settings) pullable by branches; push branch transactions (sales, returns, payments, stock movements, shifts, expenses) to the center.
- Keep branch-scoped data explicit with `BranchId`; treat global reference data separately from per-branch stock/prices where needed.

## Task List

### Phase 1: Contracts and foundation

- [ ] Task 1: Inventory current schema, auth, printing, and API boundaries; document which existing tables and endpoints require branch scope.
- [ ] Task 2: Add branch/terminal domain model and migrations (`Branches`, `Terminals`, branch fields on transactional tables, and any branch-scoped stock/price tables).
- [ ] Task 3: Define sync contracts: operation envelope, entity IDs, cursor/version model, statuses, idempotency rules, authentication, and retry policy.
- [ ] Task 4: Add installation/configuration model for `BranchId`, `TerminalId`, central URL, and branch credentials without exposing secrets in the frontend.

### Checkpoint: Foundation

- [ ] Existing single-branch behavior still works with a default branch.
- [ ] Backend builds and migrations apply on a clean database and an upgraded database.
- [ ] Sync contracts are reviewed before implementation of push/pull APIs.

### Phase 2: Central and local branch data paths

- [ ] Task 5: Implement central branch/terminal administration and admin-only APIs.
- [ ] Task 6: Make authentication and authorization branch-aware: cashier uses the terminal branch, admin can view all branches, and the backend—not only the UI—enforces scope.
- [ ] Task 7: Implement durable local `SyncOutbox` and transactionally enqueue sales, returns, payments, stock movements, shifts, and expenses.
- [ ] Task 8: Implement central push endpoints with idempotency, acknowledgements, validation, and conflict/error responses.
- [ ] Task 9: Implement central change log plus local pull cursor for products, prices, users, permissions, and settings.

### Checkpoint: Core synchronization

- [ ] A sale created online appears centrally exactly once.
- [ ] A sale created offline remains locally visible and is uploaded after reconnect/restart.
- [ ] A central product/price change reaches the branch after the next pull cycle.
- [ ] Duplicate retries do not duplicate invoices or stock movements.

### Phase 3: BranchAgent and printing

- [ ] Task 10: Create the Windows `BranchAgent` service/app with automatic startup, branch configuration, health status, and structured logs.
- [ ] Task 11: Add push worker with retry/backoff, batching, resumable progress, and safe recovery after process/device restart.
- [ ] Task 12: Add pull worker with cursor advancement only after successful local application and validation.
- [ ] Task 13: Move/route printing through a durable local `PrintQueue`; print locally and mark jobs complete only after a successful printer response.
- [ ] Task 14: Add an operator status screen/log export for connectivity, pending sync count, last successful push/pull, and printer errors.

### Checkpoint: Offline operations

- [ ] Disable internet and complete sale, return, shift close, and receipt print.
- [ ] Restart the branch computer with pending operations; all eligible operations resume automatically.
- [ ] Restore internet and verify convergence with the central database.
- [ ] Simulate a printer outage without losing the invoice or creating duplicate receipts.

### Phase 4: Deployment, backup, and hardening

- [ ] Task 15: Package VPS central deployment with HTTPS, database backup, migrations, monitoring, and admin seed procedure.
- [ ] Task 16: Package branch installation with local database initialization, branch registration, agent auto-start, printer setup, and upgrade/rollback procedure.
- [ ] Task 17: Add branch backup/restore, retention policy, UPS/restart guidance, and recovery validation.
- [ ] Task 18: Add audit logs, secret rotation, rate limits, and protection against a branch submitting data for another branch.
- [ ] Task 19: Run two-branch pilot and production readiness test using realistic outage, restart, duplicate retry, and permission scenarios.

### Checkpoint: Release

- [ ] Typecheck, lint, backend tests, frontend tests, and production builds pass.
- [ ] Both branches can operate for a defined offline window and converge after reconnect.
- [ ] Runbook exists for installation, backup, printer replacement, sync failure, and database recovery.

## Risks and Mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| Local machine failure before sync | High | Automatic local backups, UPS, and visible pending-sync warnings |
| Duplicate retry creates duplicate invoice | High | Idempotency key/entity ID uniqueness at the central database/API |
| Conflicting stock or price edits | High | Immutable stock movements, central authority for master data, explicit conflict status |
| Revoked user continues offline | Medium | Short-lived local sessions, cached permission expiry, and clear offline policy |
| Printer fails after sale | Medium | Durable print queue and separate invoice/print status |
| Mixed application versions | Medium | Agent/Backend protocol versioning and blocked incompatible upgrades |

## Open Questions

- How long may a branch operate offline before management must be alerted or sales blocked?
- Is there one POS terminal per branch initially, or multiple terminals sharing one local backend?
- Which products/prices are global and which can differ by branch?
- Should offline cashier login use cached credentials, a local PIN, or both?
- Which operations require manager approval while offline (voids, discounts, returns)?
