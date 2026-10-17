# Task: First-run node setup (central vs branch) + terminal-scoped authorization (Task 6)

## Context

Repo: `E:\work\poc\pos-app` — V9 architecture: .NET Framework 4.8 OWIN backend in `backend-cs/`
(port 3001, SQLite + Dapper + custom SQL migrations), Next.js static-export frontend in `src/`
(client-only, Arabic RTL). Read `AGENTS.md` and the skills `pos-backend-patterns` and
`frontend-pos-patterns` before writing any code. No comments, no emojis, no unused imports.

Reference: `tasks/multi-branch-offline-plan.md` Task 6 and `tasks/multi-branch-offline-status.md`
(status board, keep it updated when done).

## Current state (verified, with file refs)

- `BranchRuntimeConfig` (single row `id=1`) holds `branchId`, `terminalId`, `nodeRole`
  (`branch`|`central`), `centralBaseUrl`, credentials —
  `backend-cs/Database/Migrations/042_multibranch_sync_foundation.sql:21-31`. Seeded as
  `nodeRole='branch'`, `centralBaseUrl=NULL` at `:85-90`.
- Read: `BranchRepository.GetRuntimeContext()`
  `backend-cs/Pos.Infrastructure/Persistence/BranchRepository.cs:15-38`; agent view
  `GetAgentConfig` `:219-244`. Write: `SaveRuntimeContext` `:197-212`, validated in
  `BranchService.Configure` `backend-cs/Pos.Application/Services/BranchService.cs:153-162`.
- Endpoints exist in `backend-cs/Controllers/BranchController.cs`:
  `GET api/branch/context` (`:182-194`), `GET api/branch/agent-config` (`:196-210`),
  `POST api/branch/credentials` (`:252-268`), `POST api/branch/runtime` (`:270-290`),
  `POST api/branch/switch` (`:292-320`).
- Agent config currently from env: `AgentConfig.FromEnvironment`
  `backend-cs/BranchAgent/AgentConfig.cs:30-68` (`POS_CENTRAL_URL`, `POS_SYNC_TOKEN`,
  `POS_BRANCH_ID`, `POS_BRANCH_KEY`, `POS_SYNC_POLL_SECONDS`). It already refreshes identity
  from `GET {local}/api/branch/agent-config` via `BranchSyncWorker.RefreshIdentity`
  `backend-cs/BranchAgent/BranchSyncWorker.cs:75-121`.
- `start.bat` starts the agent **only if** `POS_CENTRAL_URL` and `POS_SYNC_TOKEN` env vars are
  set — this is the main blocker.
- Branch identity for sync: `RequireBranchKeyAttribute`
  `backend-cs/Attributes/RequireBranchKeyAttribute.cs:21-71` (no-op on branch node, verifies
  `X-Branch-Id`/`X-Branch-Key` on central), `BranchAuthService.VerifyKey` `:35-48`,
  `EnsureOperationsBelongToBranch` `:64-75`.
- Cashier auth carries **no branch/terminal**: token payload is only `{sub, exp}`
  `backend-cs/Pos.Infrastructure/Security/TokenService.cs:11-17`; login bundle
  `AuthService.BuildBundle` `backend-cs/Pos.Application/Services/AuthService.cs:68-83`;
  authorization only checks userId + permission
  `backend-cs/Attributes/RequirePermissionAttribute.cs:18-61`. List/report endpoints accept an
  unenforced optional `branchId` filter (e.g.
  `backend-cs/Pos.Infrastructure/Persistence/InvoiceRepository.cs:80-83`,
  `ReportRepository.cs:13-16`).
- Frontend runtime card:
  `src/app/[locale]/settings/branches/_components/branch-runtime-card.tsx`; API
  `src/api/branches.ts:69-71`; action `src/actions/branches.actions.ts:61-65`; types
  `src/types/domain/domain.types.ts:198-204`; hook `src/hooks/use-branches.ts:49-51`. No
  first-run wizard exists.

## Workstream A — First-run node setup

Goal: on a fresh install, before the app is usable, the operator explicitly picks the node role
and, for a branch, enrolls it with central.

Requirements:

1. Add a persisted "setup complete" flag to `BranchRuntimeConfig` (new migration, next number
   after `052`), e.g. `setupCompletedAt`. Do not break existing installs: treat an install that
   already has non-default `nodeRole`/`centralBaseUrl`/credentials as configured, or backfill via
   migration.
2. Add `GET api/branch/setup-status` (unauthenticated-safe, local `nodeRole` only) returning
   whether first-run setup is complete and the current role. Add `POST api/branch/setup` accepting
   role + (for branch) `centralBaseUrl`, `branchId`, `branchKey`/`syncToken`, `terminalId`. Reuse
   `BranchService.Configure` + `SaveCredentials` rather than duplicating logic.
3. For a **branch**, verify connectivity **before** saving: call central `GET /api/health` (and
   validate the branch key if central-side validation is available) and return a clear error if
   unreachable or rejected. Only then persist runtime + credentials and mark setup complete.
4. For **central**, `centralBaseUrl` is optional (only needed if it must advertise its own public
   URL); do not require it. It must still mark setup complete and must not attempt agent sync.
5. Make the BranchAgent start **unconditionally** in `start.bat`; when role is `central` or the
   branch is not yet enrolled, the agent stays idle and logs a warning instead of exiting. The
   agent must obtain central URL/token/key from `GET /api/branch/agent-config` (DB), with env vars
   remaining as an optional override for backward compatibility. Keep `branch-agent.env` working.
6. Frontend: add a first-run setup screen (e.g. `/[locale]/setup`) with a role choice; branch path
   shows central URL / branch id / key / terminal, runs the connection test, shows inline errors,
   then saves. Gate the dashboard layout: if setup is not complete, redirect to `/setup`. Use
   existing patterns (react-hook-form + zod, TanStack Query, `useTranslations` with a new `Setup`
   namespace in `messages/ar.json`, RTL, `TooltipIconButton` where relevant).
7. Central-side: expose whatever minimal enrollment check is needed for the connection test (e.g.
   reuse the branch-key verification path); document it in the status board.

## Workstream B — Terminal-scoped authorization (Task 6)

Goal: the backend (not the UI) scopes cashier operations to the terminal's branch. Users are not
bound to a branch; the current terminal determines the operational branch. Admin/manager can view
all branches.

Requirements:

1. Introduce a per-request branch scope resolved server-side from the local `BranchRuntimeConfig`
   (terminal-derived), exposed to controllers/repositories as an ambient context (e.g.
   `IBranchContext` / `BranchScope`) rather than each repo re-reading `WHERE id=1`.
2. On cashier-facing write paths (invoices, sale returns, shifts, payments, expenses, stock
   movements, inventory adjustments): ignore any client-supplied `branchId`, force the terminal's
   branch and terminal id.
3. On cashier-facing read/list/report paths: force the `branchId` filter to the terminal's branch.
   Admin/manager (`branches.view`) may filter across branches as today. Decide and document the
   behavior when a non-admin supplies a different `branchId` (recommend: silently clamp to own
   branch, or reject with a clear error — pick one and be consistent).
4. Keep the central `sync/*` branch-key path unchanged (that authenticates a branch, not a user).
5. Add backend tests: cashier cannot read/write another branch; admin can filter cross-branch;
   sales are stamped with the terminal's branch even if the request omits or spoofs `branchId`.
   Follow existing test layout under `backend-cs/Pos.Tests/`.

## Acceptance criteria

- Fresh DB: app forces setup; choosing central completes without a URL; choosing a branch with a
  bad URL/key fails the test and does not persist; a good branch enrols and the agent starts
  syncing without any env var.
- `start.bat` always launches the agent; unconfigured/central nodes idle with a warning.
- A cashier token cannot read or write data outside its terminal's branch through any API.
- Admin cross-branch listing/reports still work.
- Migrations apply cleanly on both a fresh and an upgraded DB; migration coverage added if the
  repo has it (`backend-cs/Pos.Tests/Infrastructure/MultiBranchMigrationTests.cs`).

## Constraints

- All filtering/pagination stays server-side. All mutations go through TanStack Query with
  `invalidateQueries({queryKey: *Keys.all})`. Keep cache keys patterned (`branchesKeys`, etc.).
- Do not introduce new runtime dependencies; use what's already in the repo.
- Do not reassign historical transactions.

## Verification (must pass before done)

```
dotnet build backend-cs/pos-cs.csproj --configuration Release
dotnet build backend-cs/BranchAgent/BranchAgent.csproj --configuration Release
dotnet test  backend-cs/Pos.Tests/Pos.Tests.csproj --configuration Release
npx tsc --noEmit
npx eslint src --quiet
npm test
npm run build
```

Finally, update `tasks/multi-branch-offline-status.md` (HEAD, commit list, Task 6 status, known
gaps, next slices) and commit with a message matching the repo's `feat(...)`/`fix(...)` style. Do
not commit `deploy/`.
