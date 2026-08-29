# POS Application — Next Steps

> **Updated:** 2026-08-29  
> **Purpose:** What to do next after the first production-hardening pass.

---

## 1. What Was Just Fixed

The following critical issues from `PRODUCTION-READINESS-REVIEW.md` have been addressed and committed:

| Issue | Fix | Files |
|---|---|---|
| License gate failed open on API error | `checkLicense()` now returns `"locked"` on any exception and logs the error. | `src/actions/license.actions.ts` |
| Hardcoded license signing secret | `LicenseCode` now reads `POS_LICENSE_SECRET` env var or `license.secret` file. Tools updated. | `backend-cs/Pos.Domain/Rules/LicenseCode.cs`, `tools/*` |
| Unauthenticated legacy printing endpoints | Added `[RequirePermission]` to `/api/printing/receipt` and `/api/printing/barcode` stubs. | `backend-cs/Controllers/PrintingController.cs` |
| CORS allowed any localhost origin | CORS is now disabled by default; enable with `POS_ENABLE_CORS=1` for dev. | `backend-cs/Startup.cs` |
| Authentication was opt-in | `ApiAuthMiddleware` now rejects unauthenticated `/api/*` requests (except login/license). | `backend-cs/Middleware/ApiAuthMiddleware.cs` |
| Profit report ignored FIFO COGS | Report now uses `InvoiceDetail.totalCost` instead of current `buyPrice`. | `backend-cs/Pos.Infrastructure/Persistence/ReportRepository.cs` |
| `start.bat` opened browser before server | Rewritten to start server, wait, open browser, then stop server on exit. | `start.bat` |
| No env template / secret not in gitignore | Added `.env.example`, ignored `license.secret`, `.md`, `vitamins.pdf`. | `.env.example`, `.gitignore` |
| License tests failed without secret | Tests now set a test secret and verify missing-secret behavior. | `backend-cs/Pos.Tests/Domain/DomainRuleTests.cs` |

### Verification after fixes

| Check | Result |
|---|---|
| `dotnet build backend-cs/pos-cs.csproj --configuration Release` | ✅ 0 warnings, 0 errors |
| `dotnet test backend-cs/Pos.Tests/Pos.Tests.csproj --configuration Release` | ✅ 124 passed |
| `npx tsc --noEmit` | ✅ passed |
| `npm run lint` | ⚠️ 299 warnings (pre-existing), 0 errors |
| `npm test` | ✅ 15 passed |

---

## 2. What Was Implemented Next

| Task | Result | Files |
|---|---|---|
| Default admin password forced change | Completed and verified. | `backend-cs/Database/Migrations/032_force_password_change.sql`, `backend-cs/Pos.Domain/Entities/User.cs`, `backend-cs/Pos.Application/Services/UsersService.cs`, `backend-cs/Pos.Infrastructure/Persistence/UsersRepository.cs`, `backend-cs/Controllers/AuthController.cs`, `src/components/common/password-change-screen.tsx`, `src/components/common/auth-context.tsx`, `src/components/common/auth-gate.tsx`, `src/actions/auth.actions.ts`, `messages/ar.json` |

### Verification after forced password change

| Check | Result |
|---|---|
| `dotnet build backend-cs/pos-cs.csproj --configuration Release` | ✅ 0 warnings, 0 errors |
| `dotnet test backend-cs/Pos.Tests/Pos.Tests.csproj --configuration Release` | ✅ 124 passed |
| `npx tsc --noEmit` | ✅ passed |
| `npm run lint` | ⚠️ 299 warnings (pre-existing), 0 errors |
| `npm test` | ✅ 15 passed |

---

## 3. Critical — Must Do Before Production Pilot

These items still block a safe production deployment:

### 3.1 Rotate the license secret

- **Problem:** The old hardcoded secret `POS-LICENSE-ACTIVATION-2026-v1` is still in git history.
- **Why it matters:** Anyone who inspected the repo or an old binary knows the previous secret.
- **Recommended fix:**
  1. Generate a new random secret (e.g., 32+ bytes from `RNGCryptoServiceProvider`, base64-encoded).
  2. Store it in `license.secret` next to `pos-server.exe` during release packaging.
  3. Never commit the new secret.
  4. Update `tools/get-license-code.ps1` and vendor documentation.

### 2.3 Patch dependency vulnerabilities

- **Problem:** `npm audit` reports 8 high-severity frontend advisories (Next.js, postcss, sharp, undici, etc.).
- **Why it matters:** Some advisories may be reachable even with static export.
- **Recommended fix:**
  1. Run `npm audit fix` and test the build.
  2. If Next.js needs a major patch, run `npm audit fix --force` and verify `npm run build` still produces a working static export.
  3. Update `Microsoft.Data.Sqlite` / `SQLitePCLRaw` in the backend to resolve the high-severity native SQLite advisory.

### 2.4 Verify default-deny auth does not break public/static assets

- **Problem:** We changed `ApiAuthMiddleware` to reject unauthenticated `/api/*` requests.
- **Why it matters:** We must confirm the SPA still loads, login still works, and `/health` is still public.
- **Recommended verification:**
  1. Start the backend.
  2. Confirm `GET /health` returns 200 without a token.
  3. Confirm `GET /api/license` returns 200 without a token.
  4. Confirm `POST /api/auth/login` works without a token.
  5. Confirm any other `POST /api/*` without a token returns 401.
  6. Build the frontend and confirm the login page loads and authenticates.

---

## 4. High Priority — Before General Availability

### 3.1 Harden login

- Add per-IP rate limiting (e.g., max 10 attempts/minute per IP).
- Persist login lockout state to the database (not just in-memory).
- Increase username lockout window to at least 15 minutes.

### 3.2 Add security headers

Add an OWIN middleware that sets:
- `Content-Security-Policy`
- `X-Frame-Options: DENY`
- `X-Content-Type-Options: nosniff`
- `Referrer-Policy: strict-origin-when-cross-origin`
- `Strict-Transport-Security` (if HTTPS is ever used)

### 3.3 Move bearer token out of `localStorage`

- Store the token in an `httpOnly`, `secure`, `SameSite=Strict` cookie.
- Update `ApiAuthMiddleware` to also read the cookie as a fallback.
- Remove `localStorage` token usage from `src/lib/auth-storage.ts`.

### 3.4 Add input validation and request limits

- Add maximum-length validation on product names, notes, supplier addresses, etc. (e.g., name ≤ 200, notes ≤ 2000).
- Configure OWIN request size limits.

### 3.5 Fix remaining lint warnings

- 299 ESLint warnings create noise and hide real issues.
- Decide which rules are non-negotiable, fix or disable the rest, and enforce 0 warnings in CI.

### 3.6 Add integration tests for authorization

- Verify `401` for missing/invalid tokens on protected endpoints.
- Verify `403` for missing permissions/features.
- Verify CORS is disabled in production builds.

---

## 5. Medium Priority — Operational Readiness

| Task | Why | Suggested Approach |
|---|---|---|
| Add structured logging | Console logs are lost; incidents are hard to diagnose. | Adopt Serilog and write rolling logs under `data/`. |
| Rename `dev.db` | Looks like development data; may be skipped in backups. | Rename to `pos.db` or make it configurable. |
| Document backup/restore | Database corruption can destroy sales history. | Nightly copy of `data/pos.db` to an external drive/cloud; test restoration. |
| Add health checks for startup scripts | `start.bat` waits a fixed 3 seconds; a real health poll is more robust. | Use PowerShell to poll `/health` before opening the browser. |
| Review tenant isolation | User updates don't check tenant boundaries. | Add tenant checks in `UsersService` and any future multi-tenant code. |
| Reconcile documentation | README migration numbering, API routes, permission/feature counts are out of sync. | Update README / FEATURES / PERMISSIONS to match actual code. |

---

## 6. Suggested Order of Work

### This week (critical path)

1. **Default admin password forced change** — this is the biggest remaining critical blocker.
2. **Rotate license secret** — generate a new secret and update release packaging.
3. **Patch dependencies** — run `npm audit fix` and update backend SQLite packages.
4. **Smoke test the auth changes** — verify login, public endpoints, and 401 behavior end-to-end.

### Next week

5. Add security headers middleware.
6. Move auth token to `httpOnly` cookie.
7. Add input validation and request limits.
8. Add auth integration tests.

### Before GA

9. Fix/suppress lint warnings and enforce 0 warnings in CI.
10. Add structured logging and backup documentation.
11. Reconcile all docs.
12. Run a full end-to-end test on a clean Windows machine using the release package.

---

## 7. How to Verify Each Future Change

Always run this sequence before committing:

```powershell
# Backend
dotnet build backend-cs/pos-cs.csproj --configuration Release
dotnet test backend-cs/Pos.Tests/Pos.Tests.csproj --configuration Release

# Frontend
npx tsc --noEmit
npm run lint
npm test
npm run build
```

For security changes, also run:

```powershell
npm audit
dotnet list package --vulnerable
```

---

## 8. Bottom Line

The first pass removed the most dangerous security holes (fail-open license, hardcoded secret, unauthenticated endpoints, permissive CORS, FIFO profit bug). The remaining work is mostly about **hardening defaults, patching dependencies, and improving operational discipline**. The highest-value next task is **forced admin password change on first login**.
