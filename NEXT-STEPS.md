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

---

## 2. What Was Implemented Next

| Task | Result | Files |
|---|---|---|
| Forced admin password change on first login | Completed. `mustChangePassword` flag + password policy + forced-change screen + `POST /api/auth/change-password`. | `backend-cs/Database/Migrations/032_force_password_change.sql`, `backend-cs/Pos.Application/Services/UsersService.cs`, `backend-cs/Pos.Infrastructure/Persistence/UsersRepository.cs`, `backend-cs/Controllers/AuthController.cs`, `src/components/common/password-change-screen.tsx`, `src/components/common/auth-context.tsx`, `src/components/common/auth-gate.tsx`, `src/actions/auth.actions.ts`, `messages/ar.json` |
| License secret rotation tooling | `tools/generate-license-secret.ps1` (crypto-random 32-byte secret). | `tools/generate-license-secret.ps1` |
| Release packaging automation | `package-release.ps1` builds backend + static export, generates `license.secret`, zips release (with `tools/` and `Migrations/`). `start.bat` finds exe in release root. | `package-release.ps1`, `start.bat` |
| Patch frontend dependency advisories | `npm audit fix` resolved 8/11 (8 high + 3 moderate → 3 high). Next intentionally stays pinned at `16.2.9`. | `package-lock.json` |
| Patch backend dependency advisories | `SQLitePCLRaw.bundle_e_sqlite3` → 2.1.13 resolves CVE-2025-6965 (GHSA-2m69-gcr7-jv3q). `dotnet list package --vulnerable` now clean. | `backend-cs/Pos.Infrastructure/Pos.Infrastructure.csproj`, `backend-cs/pos-cs.csproj` |
| Smoke test auth changes end-to-end | All checks pass on a fresh-copy DB (see §4). | — |

### Verification after this round

| Check | Result |
|---|---|
| `dotnet build backend-cs/pos-cs.csproj --configuration Release` | ✅ 0 warnings, 0 errors |
| `dotnet test backend-cs/Pos.Tests/Pos.Tests.csproj --configuration Release` | ✅ 124 passed |
| `dotnet list backend-cs/pos-cs.csproj package --vulnerable` | ✅ 0 vulnerable packages |
| `npx tsc --noEmit` | ✅ passed |
| `npm run lint` | ⚠️ 299 warnings (pre-existing), 0 errors |
| `npm test` | ✅ 15 passed |
| `npx next build` (static export) | ✅ passed |
| `npm audit` | ⚠️ 3 high remaining (all require next 16.3.x) |
| End-to-end smoke test | ✅ all 9 checks passed (see below) |

---

## 3. Remaining Critical — Must Decide Before Production Pilot

Only one critical item is still open, and it is a **decision** rather than pure implementation:

### 3.1 Upgrade Next.js to patch the last 3 high advisories

- **Problem:** `npm audit` still reports 3 high-severity advisories (`next`, `postcss`, `sharp`). The only fix available is `next@16.3.3`, which is **outside the pinned range** (`"next": "16.2.9"`).
- **Reachability:** The app is a pure static export (no Server Actions, no middleware, no custom server; images are `unoptimized`). Most flagged Next.js advisories target runtime/SSR surfaces that this deployment does not use. `postcss`/`sharp` are build-time tools.
- **Decision to make:**
  - Option A (recommended when comfortable): bump to `next@16.3.3`, run the full verification suite + static-export smoke test, and re-pin.
  - Option B: keep `16.2.9` and document the residual risk as accepted (defensible for a static, serverless export).

### 3.2 Enforce password change at the API layer (gap found in review)

- **Problem:** The forced-change gate is enforced in the **frontend only**. A token issued before the password change still works against all API endpoints (`GET /api/products`, etc.).
- **Why it matters:** A compromised admin token is usable even when the flag is set.
- **Recommended fix:** In `ApiAuthMiddleware` (or at each controller), when `User.MustChangePassword == true` allow only `POST /api/auth/change-password` (and `/api/auth/me`) and return `403` for everything else.

---

## 4. Smoke Test Results (auth & default-deny)

Executed against a fresh copy of the release build (new DB, migrations 001–032 applied):

| # | Check | Result |
|---|---|---|
| 1 | Server starts; `GET /health` (public) | ✅ 200 |
| 2 | `GET /api/license` (public, no token) | ✅ 200 |
| 3 | `GET /api/products` (no token) | ✅ 401 |
| 4 | Login `admin / 1234` → `token` + `mustChangePassword=true` | ✅ |
| 5 | `POST /api/auth/change-password` with weak password | ✅ 400 |
| 6 | `POST /api/auth/change-password` with strong password | ✅ success |
| 7 | Re-login with new password → `mustChangePassword=false` | ✅ |
| 8 | Login with old password `1234` | ✅ 401 |
| 9 | `GET /api/products` with fresh token | ✅ 200 |

---

## 5. High Priority — Before General Availability

### 5.1 Harden login

- Add per-IP rate limiting (e.g., max 10 attempts/minute per IP).
- Persist login lockout state to the database (not just in-memory).
- Increase username lockout window to at least 15 minutes.

### 5.2 Enforce `mustChangePassword` at the API layer

- See §3.2. Gate all non-password endpoints on the flag.

### 5.3 Add security headers

Add an OWIN middleware that sets:
- `Content-Security-Policy`
- `X-Frame-Options: DENY`
- `X-Content-Type-Options: nosniff`
- `Referrer-Policy: strict-origin-when-cross-origin`
- `Strict-Transport-Security` (if HTTPS is ever used)

### 5.4 Move bearer token out of `localStorage`

- Store the token in an `httpOnly`, `secure`, `SameSite=Strict` cookie.
- Update `ApiAuthMiddleware` to also read the cookie as a fallback.
- Remove `localStorage` token usage from `src/lib/auth-storage.ts`.

### 5.5 Add input validation and request limits

- Add maximum-length validation on product names, notes, supplier addresses, etc. (e.g., name ≤ 200, notes ≤ 2000).
- Configure OWIN request size limits.

### 5.6 Fix remaining lint warnings

- 299 ESLint warnings create noise and hide real issues.
- Decide which rules are non-negotiable, fix or disable the rest, and enforce 0 warnings in CI.

### 5.7 Add integration tests for authorization

- Verify `401` for missing/invalid tokens on protected endpoints.
- Verify `403` for missing permissions/features.
- Verify CORS is disabled in production builds.

---

## 6. Medium Priority — Operational Readiness

| Task | Why | Suggested Approach |
|---|---|---|
| Add structured logging | Console logs are lost; incidents are hard to diagnose. | Adopt Serilog and write rolling logs under `data/`. |
| Rename `dev.db` | Looks like development data; may be skipped in backups. | Rename to `pos.db` or make it configurable. |
| Document backup/restore | Database corruption can destroy sales history. | Nightly copy of `data/pos.db` to an external drive/cloud; test restoration. |
| Add health checks for startup scripts | `start.bat` waits a fixed 3 seconds; a real health poll is more robust. | Use PowerShell to poll `/health` before opening the browser. |
| Review tenant isolation | User updates don't check tenant boundaries. | Add tenant checks in `UsersService` and any future multi-tenant code. |
| Reconcile documentation | README migration numbering, API routes, permission/feature counts are out of sync. | Update README / FEATURES / PERMISSIONS to match actual code. |

---

## 7. Suggested Order of Work

### This week (critical path)

1. **Decide on the Next.js 16.3.3 upgrade** (§3.1) and apply the choice.
2. **Enforce `mustChangePassword` at the API layer** (§5.2) — closes the auth gap found in the smoke test.
3. **Cut a release with `package-release.ps1`** — builds backend + static export, generates a fresh `license.secret`, and zips the portable package.
4. **Run the release on a clean Windows machine** using the generated package.

### Next week

5. Harden login (rate limiting + persistent lockout).
6. Add security headers middleware.
7. Move auth token to `httpOnly` cookie.
8. Add input validation and request limits.
9. Add auth integration tests.

### Before GA

10. Fix/suppress lint warnings and enforce 0 warnings in CI.
11. Add structured logging and backup documentation.
12. Reconcile all docs.

---

## 8. How to Verify Each Future Change

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
dotnet list backend-cs/pos-cs.csproj package --vulnerable
```

For auth flow changes, re-run the end-to-end smoke test (start the packaged server on a fresh copy, verify login → forced change → re-login, §4).

---

## 9. Bottom Line

The first pass removed the most dangerous security holes (fail-open license, hardcoded secret, unauthenticated endpoints, permissive CORS, FIFO profit bug). This pass delivered the two biggest remaining critical items (forced admin password change, dependency hardening) plus release tooling and an end-to-end auth smoke test. What remains is largely **decisions** (Next.js major bump) and **hardening discipline** (API-layer gate, headers, token storage, rate limiting, lint).