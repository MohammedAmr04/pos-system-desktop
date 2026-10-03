# Deployment Audit (MonsterASP.NET / IIS) - 2026-10-03

## Purpose

Evidence-based review of the complete POS application to determine whether it can be deployed to a shared MonsterASP.NET / IIS host, and what must change first. This review is read-only: no source files were modified while producing it.

## Review Scope

- Backend: .NET Framework 4.8 OWIN Web API 2 in `backend-cs/`.
- Frontend: Next.js 16 static export in `src/`, exported through `build-static.ps1`.
- Data access: Dapper + `Microsoft.Data.Sqlite` + SQLitePCLRaw on a single SQLite file.
- Runtime devices: local ESC/POS printing, machine-bound licensing.
- Release model: `V8-Version-8.zip` with `start.bat` launching `backend\pos-server.exe`.

## Build and Repository State (verified)

| Item | Result |
| --- | --- |
| Branch | `main`, 1 commit ahead of `origin/main` (`06171eb Fix atomic bundle creation`) |
| Unpushed work | `feature/multibranch-offline-sync` has 1 unpushed commit |
| Working tree | `backend-cs/pos-cs.csproj` modified (pre-existing, adds `BranchAgent\**` exclusion to fix duplicate assembly attributes `CS0579`) |
| Backend build | `dotnet build backend-cs/pos-cs.csproj --configuration Release` → 0 warnings, 0 errors |
| Backend output | `backend-cs/bin/Release/net48/pos-server.exe` |
| Frontend build | `npm run build` → Next.js 16.2.9, 35/35 static pages, 400 files |
| Static output | `out/` copied to `backend-cs/bin/Release/net48/wwwroot` |
| Build artifacts in tree | `.next/`, `out/`, `backend-cs/bin/`, `backend-cs/obj/` are untracked |

## Current Architecture

| Concern | Current implementation | Evidence |
| --- | --- | --- |
| Hosting model | OWIN Self-Host executable, `http://localhost:{API_PORT|3001}` | `backend-cs/Startup.cs`, `backend-cs/Program.cs` |
| Target framework | .NET Framework 4.8, SDK-style project, AnyCPU | `backend-cs/pos-cs.csproj` |
| Bind address | Loopback only | `WebApp.Start` options in `backend-cs/Startup.cs` |
| Static files | `FileServer` from `wwwroot` plus SPA fallback to `index.html` | `backend-cs/Startup.cs` |
| Database | SQLite at `{AppDomain.CurrentDomain.BaseDirectory}/data/dev.db`, no override | `backend-cs/Pos.Infrastructure/Persistence/DbConnectionFactory.cs` |
| Migrations | Auto-run at startup, embedded SQL from `Database/Migrations/` | `backend-cs/Pos.Infrastructure/Data/MigrationRunner.cs` |
| Migrations on failure | `Environment.Exit(1)` | `backend-cs/Pos.Infrastructure/Data/MigrationRunner.cs` |
| Configuration | `.env` file loaded only by the self-host entry point | `backend-cs/EnvironmentFile.cs`, `backend-cs/Program.cs` |
| CORS | Permissive localhost origins for the SPA | `backend-cs/Startup.cs` |
| Authentication | Custom HMAC-SHA256 bearer token, client-side `localStorage` | `backend-cs/Pos.Infrastructure/Security/TokenService.cs`, `src/lib/auth-storage.ts` |
| Login throttling | In-memory, per process | `backend-cs/Pos.Application/Services/LoginThrottle.cs` |
| Logging | `Console.*` only | `backend-cs/` logging calls |
| Backups | `CloudBackupWorker` timer started only in the self-host path; HTTP backup/restore endpoints | `backend-cs/CloudBackupWorker.cs`, `backend-cs/Controllers/BackupsController.cs` |
| Backup restore | Overwrites the live SQLite file while connections may be open | `backend-cs/Pos.Infrastructure/Persistence/BackupService.cs` |
| Printing | `winspool.drv` P/Invoke, local printer queues, GDI+ receipt rendering | `backend-cs/Pos.Infrastructure/Printing/PrinterService.cs`, `ReceiptBuilder.cs`, `ImagePrinter.cs` |
| Licensing | Fingerprint from machine name + WMI CPU ID | `backend-cs/Pos.Infrastructure/Devices/MachineIdProvider.cs`, `backend-cs/Pos.Application/Services/LicenseService.cs` |
| Seeded admin | Permanent seeded administrator credential; forced password change disabled by migration `040` | `backend-cs/Database/Migrations/012_seed_tenant.sql`, `040_disable_forced_password_change.sql` |
| Frontend API base | Hardcoded `http://localhost:3001` fallback | `src/lib/api.ts` |

## Absent from the Repository

- `Web.config`
- `Global.asax`
- `App.config` / `appsettings.json`
- `Microsoft.Owin.Host.SystemWeb` package
- Kestrel / ASP.NET Core hosting

## Critical Blockers

| ID | Priority | Blocker | Evidence | User impact | Required change |
| --- | --- | --- | --- | --- | --- |
| BLOCK-001 | P0 | No IIS/SystemWeb hosting configuration exists. | No `Web.config`, no `Microsoft.Owin.Host.SystemWeb`, no `System.Web` hosting; entry point is `WebApp.Start` inside an EXE. | The application cannot start under IIS at all. | Convert `backend-cs/pos-cs.csproj` to a SystemWeb-hosted class library, add `Microsoft.Owin.Host.SystemWeb`, add `Web.config`, keep `Program.cs` only as a development/self-host harness. |
| BLOCK-002 | P0 | Startup migrations call `Environment.Exit(1)`. | `MigrationRunner` exits the process on failure; `Startup` runs migrations before serving. | Under IIS this kills the `w3wp.exe` worker instead of returning an error; the app pool recycles in a loop. | Replace `Environment.Exit(1)` with an exception; fail the request/startup explicitly and log the migration error. |
| BLOCK-003 | P0 | SQLite path has no configuration override and assumes a writable app directory. | `DbConnectionFactory` builds `{BaseDirectory}/data/dev.db`; no env var or `appSettings` key. | On a shared host the app directory may be read-only or ephemeral, so the database is lost or the app fails to start. | Make the database path configurable (`appSettings`/env), default to a writable persistent location such as `App_Data`, and validate write access at startup. |
| BLOCK-004 | P0 | Frontend is built against `http://localhost:3001`. | `src/lib/api.ts` fallback base URL. | In the browser the SPA calls the visitor's own machine, so nothing loads. | Make the API base URL build-time configurable (or same-origin relative `/api`) and set it during the deployment build. |
| BLOCK-005 | P0 | Backend binds to `localhost` only. | `WebApp.Start` options in `Startup.cs`. | Unreachable from outside the server even in a working deployment. | Remove the loopback restriction under IIS hosting (SystemWeb ignores it) and keep it only for the self-host path. |
| BLOCK-006 | P0 | Server-side printing depends on Windows print infrastructure. | `winspool.drv` P/Invoke in `PrinterService`; GDI+ receipt rendering in `ReceiptBuilder`. | No local printers exist on a shared host; printing silently fails or throws, and GDI+ rendering is expensive (`GetPixel` loop in `ImagePrinter`). | Move printing to the browser or a local print agent. Keep server-side printing desktop-only behind a capability flag. |
| BLOCK-007 | P1 | Licensing is machine-bound. | `MachineIdProvider` uses machine name + WMI CPU ID. | A single shared host produces one fingerprint for all users; a per-till license model breaks, and unlocking becomes host-wide. | Re-scope licensing per tenant/organization, or move the fingerprint to a client-supplied device identity. |
| BLOCK-008 | P1 | Default seeded administrator password remains usable. | Migration `012` seeds an administrator; migration `040` disables forced password change. | A publicly reachable deployment ships with a known credential. | Remove the seeded credential from migration history or force rotation at first request; never rely on a shipped default. |
| BLOCK-009 | P1 | IIS ignores generated binding redirects. | Only `pos-server.exe.config` is produced by the build. | Assembly binding failures appear at runtime under IIS even though the EXE runs locally. | Add `<assemblyBinding>` redirects for Newtonsoft.Json, System.Web.Http, and System.Net.Http.Format to `Web.config`. |
| BLOCK-010 | P1 | Restore overwrites the live database file. | `BackupService` copies a backup over the active SQLite file while connections may be open. | Data corruption or "database is locked" errors on shared hosting with multiple worker processes. | Restore through a controlled shutdown/connection-closed path, or restore via a new file plus atomic swap under a maintenance flag. |

## Important Warnings

| ID | Priority | Warning | Evidence | Recommended action |
| --- | --- | --- | --- | --- |
| WARN-001 | P1 | Migrations have no cross-process lock. | `MigrationRunner` has no mutex/file lock. | Add a named mutex or lock file so concurrent workers cannot run migrations simultaneously. |
| WARN-002 | P1 | SQLite behavior on shared hosting is unverified. | File-based SQLite with `Microsoft.Data.Sqlite`; locking depends on filesystem semantics. | Validate write concurrency and lock behavior on the target host; consider restricting the app pool to one worker process. |
| WARN-003 | P2 | `CloudBackupWorker` only runs in the self-host path. | Started from `Startup` self-host branch only. | Use an external scheduler for hosted backups, or start the worker explicitly under IIS with a guarded lifetime. |
| WARN-004 | P2 | No IIS SPA fallback / default document. | Fallback lives in the self-host `FileServer` middleware. | Configure URL Rewrite and default documents in `Web.config` or hosting panel. |
| WARN-005 | P2 | 274 static `.txt` files depend on correct MIME handling. | Static export includes many `.txt` assets. | Ensure MIME mappings for `.txt` (and other used extensions) are present in `Web.config`. |
| WARN-006 | P2 | Static export is root-only. | `next.config.ts` has no `basePath`/`assetPrefix`. | Hosting must be at the domain root, or the build must set `basePath` for a subpath. |
| WARN-007 | P2 | Console-only logging. | `Console.*` calls throughout `backend-cs/`. | Add an IIS-compatible logging provider or forward logs to a file/ETW sink. |
| WARN-008 | P2 | `localStorage` session tokens on a network-accessible host. | `src/lib/auth-storage.ts`. | Acceptable for local desktop use; harden before exposing the app over the network. |

## Platform Assumptions to Confirm (MonsterASP.NET)

- Whether `Web.config` can be edited or is auto-generated.
- Whether writable persistent storage is provided, and where.
- Whether the app pool worker process count can be pinned to 1.
- Installed .NET Framework version and pipeline mode (Integrated vs Classic).
- URL Rewrite availability.
- Domain-root versus subpath hosting.
- Outbound HTTPS access and scheduler availability.
- Log file location and retention.

## Recommended Target Architecture

| Layer | Recommendation |
| --- | --- |
| Frontend | Next.js static export served at the domain root by IIS. |
| Backend | Web API 2 OWIN pipeline hosted by IIS/SystemWeb (`.dll` in an app pool), not the self-host EXE. |
| Database | SQLite in a writable persistent location (`App_Data` or mapped storage), with a configurable path. |
| App pool | .NET CLR v4.x, Integrated pipeline, single worker process. |
| Printing | Browser print or a local print agent; server-side printing disabled on the host. |
| Backups | External scheduler invoking backup endpoints or a maintenance job; no in-process timer. |
| Licensing | Tenant/organization-scoped instead of machine-scoped. |

## Required Changes (ordered)

1. Convert `backend-cs/pos-cs.csproj` to a SystemWeb-hosted class library; add `Microsoft.Owin.Host.SystemWeb`; keep `Program.cs` as a development harness only.
2. Add `Web.config` with CLR target, assembly binding redirects, SPA fallback/default document, and MIME mappings.
3. Remove `Environment.Exit(1)` from `MigrationRunner`; add a cross-process migration lock.
4. Make the SQLite path configurable and validate write access at startup.
5. Make the frontend API base URL build-time configurable or same-origin (`/api`).
6. Remove the loopback bind restriction for the hosted path.
7. Guard server-side printing behind a capability flag; provide a browser/agent path.
8. Re-scope licensing away from machine-bound fingerprints.
9. Remove or force-rotation of the seeded administrator credential.
10. Replace in-process backups with an external scheduler; make restore safe.
11. Replace `Console.*` logging with an IIS-compatible sink.

## Work State

### Completed

- Switched to `main`, pulled with `git pull --ff-only origin main`.
- Fixed `CS0579` duplicate assembly attributes by excluding `BranchAgent\**` in `backend-cs/pos-cs.csproj`.
- Verified backend build (0 warnings, 0 errors) and frontend build (35/35 pages).
- Completed this deployment audit (read-only; no files changed during the audit).
- **BLOCK-001 / BLOCK-005** — IIS/SystemWeb hosting: `OwinStartup.cs`, `Microsoft.Owin.Host.SystemWeb`,
  `deploy/iis/Web.config`, `package.ps1` builds the server with `-p:OutputType=Library`.
  Loopback bind stays self-host only.
- **BLOCK-002** — `MigrationRunner` throws instead of `Environment.Exit(1)`; cross-process named mutex
  (`Global\PosCsMigrations`, 120s timeout). `Startup` writes `data\startup-error.txt` and rethrows.
- **BLOCK-003** — SQLite path configurable via `POS_DB_PATH` (env or `appSettings` through `PosConfig`).
- **BLOCK-004** — Frontend builds with `NEXT_PUBLIC_API_URL=__SAME_ORIGIN__`; `package.ps1` fails the build
  if `localhost:3001` survives in any chunk.
- **BLOCK-006** — `POS_SERVER_PRINTING` capability flag (default on for self-host, `0` in the IIS payload).
  `PrintingController` returns `503 {success:false, message:...}` before touching winspool/GDI+.
- **BLOCK-009** — Assembly binding redirects completed in `Web.config`: Newtonsoft.Json, SQLitePCLRaw.core,
  System.Memory, System.Web.Http, System.Net.Http.Formatting, System.Web.Cors, System.Buffers,
  System.Runtime.CompilerServices.Unsafe, System.Numerics.Vectors, System.Threading.Tasks.Extensions
  (versions verified against the packaged `bin\`).
- **BLOCK-010** — `BackupService.Restore` now calls `SqliteConnection.ClearAllPools()`, swaps via
  `File.Replace` (with `File.Copy` fallback) and retries `IOException`/`UnauthorizedAccessException`.
- **WARN-001** — Migration lock added (see BLOCK-002).
- **WARN-003** — `CloudBackupWorker` can start under IIS behind `POS_BACKUP_WORKER` (default off in the
  payload; external scheduler still recommended).
- **WARN-007** — `ConsoleLog.Install()` tees `Console.Out/Error` to `data\logs\pos-YYYYMMDD.log`
  (14-day retention), enabled by `POS_LOG_DIR`. Covers all ~194 call sites without rewriting them.
- **Config plumbing** — `PosConfig.SyncToEnvironment()` copies `POS_*`/`PRINTER_NAME`/`API_PORT`
  `appSettings` into process env vars, so legacy `Environment.GetEnvironmentVariable` readers
  (license secret, printer name, backup/cloudinary keys) work under IIS where no `.env` is loaded.
- **Docs** — `docs/deploy/MONSTERASP-DEPLOY.md` documents every `Web.config` key, the 503 printing
  behaviour, and the `data\logs` location.
- **Tests** — `PosConfigTests` (19 cases) covers `GetBool` parsing, env-over-`appSettings` precedence,
  value trimming, and the `SyncToEnvironment` copy/skip rules.
- **Verification** — `dotnet build` (0 warnings/0 errors), `dotnet test` (213/213), `npx tsc --noEmit`,
  `npm run lint` (0 errors), `npm test` (15/15), `npm run build` (35 pages),
  `package.ps1`, `smoke-test.ps1` on both 64-bit and 32-bit IIS Express (13/13 checks).

### Deferred by decision

- **BLOCK-007 (licensing)** — machine-bound licensing kept as-is; the deployment runs on an
  operator-controlled server, so the shared-host fingerprint concern does not apply here.
- **BLOCK-008 (seeded admin credential)** — forced password rotation deferred; the operator changes
  `admin`/`1234` manually (documented step in `MONSTERASP-DEPLOY.md`).
- **WARN-007 full migration** — replacing `Console.*` with a logging abstraction (~194 call sites)
  deferred; the file tee covers hosted diagnostics for now.
- **BLOCK-006 browser/agent printing** — the print agent / browser print path is still future work;
  the server-side path is safely disabled instead.

### Active

- None.

### Blocked

- Cannot finalize the deployment plan without confirming the MonsterASP.NET platform assumptions listed above.

## Next Steps

1. Do not modify files until implementation is explicitly requested and the platform assumptions are confirmed.
2. Once authorized, implement items 1-5 of "Required Changes" first (hosting conversion, `Web.config`, migration safety, database path, API base URL), then re-verify with `dotnet build` and `npm run build`.
