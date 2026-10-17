# Multi-Branch Offline POS — Branch Status

Living status board for the multi-branch / offline-first work. It combines the full task list
from `tasks/multi-branch-offline-plan.md` with the implemented state of the branch as of the
latest commit.

Status legend: `[x]` done · `[~]` partial / in progress · `[ ]` not started.

## Branch

| Field | Value |
|---|---|
| Branch | `feature/multibranch-offline-sync` |
| HEAD | `a60ab0f` — feat(sync): add retry backoff and dead-letter for outbox operations |
| Base | branched from `main` after `06171eb` |
| Commits ahead of `main` | 9 |
| Remote | `https://github.com/MohammedAmr04/pos-system-desktop.git` |
| Signed commits | yes (GPG) |
| Frontend build output | `backend-cs/bin/Release/net48/wwwroot` (static export served by the backend) |

### Commit history (feature branch)

| Commit | Description |
|---|---|
| `098cb2f` | Add multi-branch offline sync foundation |
| `bc5d291` | Complete POS purchase and multibranch updates |
| `8ebc9fa` | Start branch agent and retry queued receipts |
| `a1c36a8` | feat(sync): complete branch-to-central push for cash and stock operations |
| `37a0199` | feat(sync): push sales returns with detail lines to central |
| `0ba1791` | feat(sync): add operator sync status screen |
| `66c2dd1` | fix(sync): keep failed central applies unacknowledged |
| `5560627` | fix(sync): propagate sale return status to central invoice |
| `a60ab0f` | feat(sync): add retry backoff and dead-letter for outbox operations |

## Verification (current HEAD)

| Check | Command | Result |
|---|---|---|
| Backend build | `dotnet build backend-cs/pos-cs.csproj --configuration Release` | pass (0/0) |
| Backend build (agent) | `dotnet build backend-cs/BranchAgent/BranchAgent.csproj --configuration Release` | pass |
| Backend tests | `dotnet test backend-cs/Pos.Tests/Pos.Tests.csproj --configuration Release` | 270/270 pass |
| Frontend typecheck | `npx tsc --noEmit` | clean |
| Frontend lint (source) | `npx eslint src --quiet` | 0 errors |
| Frontend tests | `npm test` | 15/15 pass |
| Frontend build | `npm run build` | pass (`/ar/setup`, `/ar/settings/branches`, `/ar/settings/terminals`, `/ar/settings/sync` emitted) |

> Note: full `npm run lint` reports errors from the untracked generated `deploy/` folder. Use
> `npx eslint src --quiet` to check source only. `deploy/` is deliberately left unstaged on this branch.
>
> Uncommitted work (Tasks 5 & 6) sits on top of `a60ab0f`: central branch/terminal admin, safe branch
> switching, per-list/report `branchId` filters, terminal-scoped authorization, the first-run setup
> wizard + setup/health APIs, and the Branches/Terminals settings screens. All checks above were run
> against this working tree.

## Database migrations

| Migration | Purpose |
|---|---|
| `042_multibranch_sync_foundation.sql` | `Branch`, `Terminal`, `BranchRuntimeConfig`, `SyncOutbox`, `SyncChangeLog`, `SyncInbox`; `branchId`/`terminalId` on transactional tables; branch-context triggers |
| `043_sync_masterdata_print_queue.sql` | Payload-carrying change-log triggers for Product/Category/Brand/ProductUnit; `PrintQueue` table |
| `044_sync_outbox_payloads.sql` | Payload triggers for Payment/Shift/Expense/StockMovement, branch-node-only guarded |
| `045_drop_sale_return_outbox_trigger.sql` | Removes empty SaleReturn trigger (now written by `SyncOutboxWriter`) |
| `046_sync_outbox_retry.sql` | Adds `SyncOutbox.nextAttemptAt` + retry index |
| `047_inventory_adjustment_branch.sql` | `branchId`/`terminalId` on `InventoryAdjustment` + branch-context trigger |
| `048_composite_document_numbers.sql` | Rebuilds `Shift`, `SaleReturn`, `PurchaseReturn`, `InventoryAdjustment` with `UNIQUE(number, branchId)`; recreates their indexes and branch/sync triggers |
| `049_stock_movement_branch_product_index.sql` | Index `(branchId, productId)` on `StockMovement` for central per-branch stock aggregation |
| `050_branch_secret.sql` | `secretHash` on `Branch` for per-branch sync key enrollment |
| `051_branch_admin.sql` | `Terminal.lastSeenAt`/`notes`; `BranchRuntimeConfig.centralToken`/`centralBranchKey`/`status`; `BranchAssignmentHistory` audit table + indexes |
| `052_branch_permissions.sql` | Seeds `branches.view` / `branches.manage` permissions and grants them to the admin/manager roles |
| `053_first_run_setup.sql` | Adds `BranchRuntimeConfig.setupCompletedAt` and backfills existing installs (mark complete when central URL/token/branch key present, `nodeRole='central'`, or any invoice exists) |

## What is implemented

### Backend (sync core)
- `SyncOutbox` journal with triggers enqueuing sales, returns, payments, stock movements, shifts,
  and expenses; payloads carry full entity JSON. `SyncOutboxWriter` is the explicit enqueue path
  for entities that need detail lines (invoices, sale returns, **purchase invoices, purchase
  returns, inventory adjustments**) and is a no-op on the central node.
- Purchases, purchase returns, and posted inventory adjustments now push to central:
  `PurchaseRepository.Save`/`Cancel` and `PurchaseReturnRepository.Create` enqueue full
  header+detail payloads; `OperationsRepository.PostAdjustment` enqueues the posted count.
  Central applies them via `SyncRepository.ApplyReceived` (purchase header/items upserted so
  cancellation and edits converge, purchase-return detail lines and the purchase `returnStatus`,
  inventory-adjustment header/lines).
- Central ingest: `api/sync/receive` writes `SyncInbox` idempotently and applies operations via
  `SyncRepository.ApplyReceived` (Invoice, Payment, Shift, Expense, StockMovement, SaleReturn +
  details, plus central `Invoice.returnStatus` propagation). Missing/omitted results fail closed.
- Branch-key handshake: a branch proves identity with a long-lived key whose PBKDF2 hash lives in
  `Branch.secretHash` (migration 050). `RequireBranchKeyAttribute` enforces `X-Branch-Id`/`X-Branch-Key`
  on central `sync/receive` and `sync/changes`, and central rejects any operation claiming a branch
  other than the authenticated one (`BranchAuthService.EnsureOperationsBelongToBranch`). Enrollment is
  backward-compatible: a branch with no key keeps authenticated-user trust; the branch agent sends the
  key from `POS_BRANCH_KEY`. An admin endpoint `POST api/branch/{id}/secret` (`branches.manage`) sets
  or rotates the key.
- Per-branch stock aggregation: central derives a branch's stock as the signed sum of its
  `StockMovement` ledger (`ReportRepository.GetBranchStock`) instead of a mutable counter, exposed
  at `GET api/reports/branch-stock?branchId=`. Zero-net products are omitted.
- Per-branch document numbering: `Shift`, `SaleReturn`, `PurchaseReturn`, and
  `InventoryAdjustment` are unique on `(number, branchId)` (migration 048), so each branch keeps
  its own `1..N` sequence and central convergence no longer drops the colliding row.
- Change feed: `api/sync/changes` reads `SyncChangeLog` for master data (products, categories,
  brands, units, prices); the agent pulls with a persisted cursor.
- Status: `api/sync/status` returns pending/failed/**dead** counts and last push/pull timestamps.
- Retry policy: failed outbox operations back off exponentially (`nextAttemptAt`, 10s doubling,
  capped 30m) and move to `dead` after 8 attempts so they stop retrying.
- `BranchController`: branch runtime context, agent config, branch list/paged/get, terminals, CRUD,
  activate/deactivate, delete, secret, credentials, runtime configuration, and branch switch
  (`POST api/branch/switch`, `GET api/branch/switch/{requestId}`) — all permission-gated
  (`branches.view`/`branches.manage`).
- `TerminalController` (`api/terminals`): paged list/status/history + terminal CRUD and
  activate/deactivate, gated by `branches.view`/`branches.manage`.
- Central branch/terminal administration: `BranchService`/`TerminalService` enforce unique branch
  codes, block disabling/deleting branches that still own terminals or are referenced by transactions,
  and require an active target branch for terminals.
- Safe branch switching: `BranchAssignmentService` drains the outbox first (fails closed), calls central
  via `BranchSwitchClient` (TLS1.2, `POST /api/branch/switch`), records an idempotent
  `BranchAssignmentHistory` row keyed by `requestId`, verifies the optional target-branch key against
  `Branch.secretHash`, and only then applies the new local runtime context. `Reconcile` recovers a lost
  response by querying central; historical transactions are never reassigned. `BranchRuntimeConfig.status`
  exposes an explicit `switching` state. The agent refreshes its branch identity from
  `GET api/branch/agent-config` each loop so a switch propagates without a restart.
- Branch-scoped listing/reports: optional `branchId` filter added to invoices, payments, purchases,
  shifts, expenses, sale returns, purchase returns, and every report aggregation (server-side
  `(@branchId IS NULL OR ...)` pattern).
- First-run setup: `BranchService.GetSetupStatus()` reports whether the node is configured
  (`BranchRuntimeConfig.setupCompletedAt`, migration 053) and its role; `POST api/branch/setup`
  (`branches.manage`) completes setup for a central node (runtime context + mark complete) or a
  branch node (verifies the central via `CentralEnrollmentClient` before persisting anything, then
  ensures branch/terminal rows, saves credentials, and marks complete). `GET api/branch/setup-status`
  is public; `GET api/branch/enroll` uses `[RequireBranchKey]`.
- Terminal-scoped authorization (Task 6): `ApiAuthMiddleware` derives the operational branch from the
  local `BranchRuntimeConfig` after authentication and installs an ambient `IBranchContext`
  (`BranchScope`/`AmbientBranchContext`). Cashier-facing reads, lists, and reports are silently clamped
  to the terminal's branch; only `branches.view` holders may cross branches. Invoice list/today/filter
  and get-by-id, payments, purchases, shifts, expenses, sale/purchase returns, and all report
  aggregations honor the scope. Invoice creation stamps `BranchId`/`TerminalId` from the ambient scope,
  falling back to runtime config, so a spoofed branch on a write is overridden.
- Health alias: `GET /api/health` mirrors `GET /health` (shared `Ping()`); `/api/health` is on the
  unauthenticated allow-list for load balancers and the enrollment client.
- Agent config: `AgentConfig.FromEnvironment` no longer throws on a fresh install (empty defaults,
  optional env overrides, `NodeRole`); `BranchSyncWorker` idles (warn-once) when the node is central or
  unenrolled instead of failing, and refreshes its role each loop.

### BranchAgent (`backend-cs/BranchAgent`)
- Console process (`pos-branch-agent.exe`) with push, pull, and print-retry loops.
- Config via env vars (`POS_CENTRAL_URL`, `POS_SYNC_TOKEN`, `POS_BRANCH_LOCAL_URL`,
  `POS_BRANCH_ID`, `POS_SYNC_POLL_SECONDS`); pull cursor persisted in `data/sync-cursor.txt`.
- Selective acknowledgement: only applied operations are acked; failures are marked with their
  error; the pull cursor advances only after a fully successful apply.

### Frontend (`src/`)
- Settings → Sync screen (`/ar/settings/sync`, `SyncStatus` namespace) showing branch sync and print
  queue status, including a "needs attention" hint when dead-lettered operations exist.
- `src/api/sync.ts`, `src/hooks/use-sync.ts`, `SyncStatus`/`PrintQueueStatus` domain types.
- Settings → Branches screen (`/ar/settings/branches`, `BranchesAdmin` namespace): branch table with
  search/pagination, create/edit, activate/deactivate, delete, per-branch sync-key enrollment, and a
  runtime card for branch identification (node role, branch, terminal, central URL, credentials).
- Settings → Terminals screen (`/ar/settings/terminals`, `TerminalsAdmin` namespace): terminal table
  with branch filter, create/edit, activate/deactivate, an assignment history dialog, and a safe
  branch-switch dialog that blocks while the outbox is not drained and shows the central-confirmed
  result. `src/api/branches.ts`/`terminals.ts`, hooks `use-branches`/`use-terminals`, and actions
  `branches.actions`/`terminals.actions` back both screens.
- `PERMISSIONS.BRANCHES_VIEW`/`BRANCHES_MANAGE` constants (+ labels), sidebar entries, and `errors`
  namespace entries for the new backend messages.
- First-run setup wizard (`/ar/setup`, `Setup` namespace): a central/branch chooser
  (`central-setup-form` / `branch-setup-form`) that calls `getSetupStatus`/`completeSetup`.
  `SetupGate` sits between `AuthGate` and `LicenseGate` in the locale layout and redirects to `/setup/`
  when setup is incomplete (fails open on error); `dashboard-layout` bypasses app chrome on `/setup/`.

## Task board

### Phase 1 — Contracts and foundation

- [x] Task 1: Inventory schema, auth, printing, and API boundaries; document branch scope.
- [x] Task 2: Branch/terminal domain model and migrations (`Branches`, `Terminals`, branch fields, triggers).
- [~] Task 3: Sync contracts — envelope, entity IDs, cursor/version, statuses, idempotency defined;
      authentication/retry policy partly done (retry policy now exists; auth handshake pending).
- [~] Task 4: Installation/config model for `BranchId`, `TerminalId`, central URL, credentials —
      runtime config + agent env exist, the branch-key handshake (`Branch.secretHash` + `POS_BRANCH_KEY`)
      is implemented, and the first-run setup wizard + `setup-status`/`setup`/`enroll` APIs capture the
      node role, branch/terminal identity, and central credentials (migration 053); key
      rotation/revocation tooling is not.

### Checkpoint — Foundation

- [x] Existing single-branch behavior still works with a default branch.
- [x] Backend builds and migrations apply on a clean and an upgraded database.
- [~] Sync contracts reviewed before implementation of push/pull APIs.

### Phase 2 — Central and local branch data paths

- [x] Task 5: Central branch/terminal administration and admin-only APIs — full branch/terminal CRUD,
      runtime context/credentials, secret enrollment, and safe branch switching via `BranchController`
      (`api/branch/*`) and `TerminalController` (`api/terminals/*`), gated by `branches.view`/`branches.manage`;
      Branches/Terminals settings screens.
- [x] Task 6: Branch-aware authentication/authorization — central proves branch identity, rejects
      cross-branch sync submissions, supports a central-confirmed, outbox-gated, idempotent terminal
      branch switch, and derives the operational branch from the terminal to scope all cashier-facing
      reads, lists, reports, and writes (only `branches.view` holders cross branches).
- [x] Task 7: Durable local `SyncOutbox` with transactional enqueue for sales, returns, payments,
      stock movements, shifts, expenses, purchases, purchase returns, and posted inventory counts.
- [x] Task 8: Central push endpoints with idempotency, acknowledgements, validation, and error responses.
- [x] Task 9: Central change log plus local pull cursor for products, prices, users, permissions, settings
      (master data implemented; users/permissions/settings coverage to confirm).

### Checkpoint — Core synchronization

- [~] A sale created online appears centrally exactly once (logic in place; not yet end-to-end verified).
- [~] A sale created offline remains locally visible and uploads after reconnect/restart.
- [x] A central product/price change reaches the branch after the next pull cycle.
- [x] Duplicate retries do not duplicate invoices or stock movements (idempotent outbox id + `SyncInbox`).

### Phase 3 — BranchAgent and printing

- [~] Task 10: Windows `BranchAgent` — console app with config/logs exists; no Windows service /
      auto-start install, no health endpoint yet.
- [~] Task 11: Push worker — batching (limit=50), persistence/resumability, and retry/backoff with
      dead-letter done; restart-recovery coverage and jitter still open.
- [x] Task 12: Pull worker advances the cursor only after successful local application.
- [x] Task 13: Durable local `PrintQueue`; jobs marked complete only after a successful print.
- [x] Task 14: Operator status screen/log export for pending counts, last push/pull, printer errors.

### Checkpoint — Offline operations

- [ ] Disable internet and complete sale, return, shift close, and receipt print.
- [~] Restart the branch computer with pending operations; eligible operations resume (outbox is durable;
      needs an explicit test).
- [ ] Restore internet and verify convergence with the central database.
- [ ] Simulate a printer outage without losing the invoice or creating duplicate receipts.

### Phase 4 — Deployment, backup, and hardening

- [ ] Task 15: Package VPS central deployment (HTTPS, backup, migrations, monitoring, admin seed).
- [ ] Task 16: Package branch installation (DB init, registration, agent auto-start, printer setup, upgrade/rollback).
- [ ] Task 17: Branch backup/restore, retention, UPS/restart guidance, recovery validation.
- [~] Task 18: Audit logs, secret rotation, rate limits, cross-branch submission protection —
      cross-branch submission protection done; rotation UI, rate limits, and audit logging open.
- [ ] Task 19: Two-branch pilot and production-readiness test.

### Checkpoint — Release

- [~] Typecheck, lint, backend tests, frontend tests, and production builds pass (currently green).
- [ ] Both branches operate for a defined offline window and converge after reconnect.
- [ ] Runbook for installation, backup, printer replacement, sync failure, and database recovery.

## Known gaps and deferred risks

- **Cross-branch `number` collisions — resolved.** Migration 048 rebuilds `Shift`, `SaleReturn`,
  `PurchaseReturn`, and `InventoryAdjustment` with composite `UNIQUE(number, branchId)` and recreates
  their indexes/triggers; each branch keeps its own `1..N` sequence. `Invoice.invoiceNumber` remains
  non-unique (duplicates are allowed and no rows are lost).
- **Per-branch stock aggregation — addressed.** Central now reports stock per branch by summing the
  immutable `StockMovement` ledger (`GET api/reports/branch-stock`). `Product.stockQuantity` remains a
  single global column used for master-data sync; central's authoritative per-branch view is the ledger.
- **Branch-scoped auth (Task 6) — addressed.** The terminal's branch is derived server-side from
  `BranchRuntimeConfig` and enforced through an ambient `IBranchContext`: cashier reads, lists, reports,
  and writes are clamped to that branch, and a user must hold `branches.view` to cross branches. Central
  still verifies branch identity via a per-branch key (migration 050) and rejects cross-branch sync
  submissions. Still open (Task 18): key rotation/revocation UI, request signing/rate limits, and audit
  logging of auth failures.
- **Agent deployment (Task 10):** no Windows service/auto-start or health endpoint yet.

## Recommended next slices

1. Agent Windows service / auto-start install (Task 10).
2. Surface per-branch stock and sync health in the operator UI.
3. End-to-end offline pilot: disable internet, complete sale/return/shift/print, restart, reconnect, verify convergence.

## How to verify locally

```bash
dotnet build backend-cs/pos-cs.csproj --configuration Release
dotnet build backend-cs/BranchAgent/BranchAgent.csproj --configuration Release
dotnet test  backend-cs/Pos.Tests/Pos.Tests.csproj --configuration Release

npx tsc --noEmit
npx eslint src --quiet
npm test
npm run build
```
