# Multi-Branch Offline POS — Execution Checklist

## Phase 1 — Foundation

- [ ] Audit current schema, auth, printing, and API contracts.
- [ ] Add `Branches` and `Terminals` migrations.
- [ ] Add `BranchId` to transactional entities and queries.
- [ ] Define sync envelope, cursor, idempotency, and retry contracts.
- [ ] Add secure branch installation configuration.

## Phase 2 — Sync core

- [ ] Add central branch administration.
- [ ] Enforce terminal-derived branch scope for cashier operations.
- [ ] Add durable `SyncOutbox` writes in the same transaction as local operations.
- [ ] Add central idempotent push endpoints.
- [ ] Add central change log and local pull cursor.

## Phase 3 — BranchAgent and printing

- [ ] Build auto-start Windows BranchAgent.
- [ ] Implement push and pull workers with retries and restart recovery.
- [ ] Implement durable local print queue.
- [ ] Add sync/printer health status.

## Phase 4 — Release

- [ ] Add VPS deployment, HTTPS, backups, and monitoring.
- [ ] Add branch installer, registration, printer setup, and upgrade procedure.
- [ ] Add branch backup/restore and audit/security hardening.
- [ ] Run two-branch pilot and outage/restart/retry tests.
