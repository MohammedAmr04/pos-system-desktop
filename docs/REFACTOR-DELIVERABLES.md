# POS Clean Architecture Refactoring — Deliverables

Refactor of the POS backend (`backend-cs/`) into layered architecture while preserving 100% of existing behavior, API contracts, and DB schema. No database changes; no API contract changes; .NET Framework 4.8 retained.

---

## 1. What Changed

The single monolithic Web API project (`pos-cs.csproj` containing controllers + services + repositories + helpers + printing) was decomposed into four projects:

```text
backend-cs/
├── Pos.Domain/            (new) pure business logic — zero external dependencies
├── Pos.Application/       (new) use cases + ports (interfaces) — depends only on Domain
├── Pos.Infrastructure/    (new) Dapper repos, crypto, printing, devices — depends on Application
├── pos-cs.csproj          Presentation: OWIN host, controllers, middleware, composition root
└── Pos.Tests/             (new) xUnit test suite (58 tests)
```

**Dependency rule** (enforced by project references):

```text
pos-cs (Presentation) → Application → Domain
        ↓                   ↑
   Infrastructure ──────────┘
```

Domain and Application reference no infrastructure packages (no Dapper, no SQLite, no OWIN).

### Deleted legacy folders

`Repositories/`, `Services/`, `Helpers/`, `Builders/`, `Printers/`, `Formatters/`, `Database/Migrations/MigrationRunner.cs` — all behavior moved into the new layers.

### Kept in place

- All 11 controllers (rewritten thin), `Middleware/ApiAuthMiddleware.cs`, `Attributes/RequirePermissionAttribute.cs`, `Startup.cs`, `Program.cs`.
- `Database/Migrations/*.sql` files (still copied to output; runner moved to `Pos.Infrastructure/Data/MigrationRunner.cs`).
- Global JSON serializer stays camelCase; a custom `LegacyJsonContractResolver` (`Api/LegacyJsonContractResolver.cs`) pins `Invoice.InvoiceDetail` to PascalCase exactly as the legacy `[JsonProperty]` did, so wire responses are byte-compatible with the frontend.

---

## 2. Layer Contents

### Pos.Domain (pure)

| Area | Files |
|---|---|
| Entities | Product, ProductUnit, ProductBarcode, Invoice, InvoiceDetail, User, Role, Permission, Settings, Tenant, TenantFeature |
| Exceptions | DomainValidationException (400), NotFoundException (404), PermissionDeniedException / FeatureDisabledException (403), LoginLockedException (429), InsufficientStockException (400) |
| Enums | DiscountTypes, PriceModes |
| Rules (extracted verbatim from legacy controller logic) | `InvoicePricing` (line/invoice discounts, proportional discount distribution with rounding-drift correction, profit protection), `UnitRules`, `PriceSelection` (retail/wholesale selection, override detection >0.005 delta), `LicenseCode` (HMAC-SHA256 unlock code), `LowStockPolicy` |

### Pos.Application

- **Ports** (`Ports/`): repository interfaces per aggregate, `IAccessControl`, `IClock`, `IPasswordHasher`, `ITokenService`, `IMachineIdProvider`, `IReceiptPrinter`, `IBarcodeLabelPrinter`.
- **Models** (`Models/`): request DTOs (`*Request`) + result models (`PagedResult<T>`, `AccessBundle`, `LoginResult`, `InvoicePageResult`, `RoleSummary`, `UserSummary`, `FeatureToggleState`, `LicenseStatus`, printing payloads).
- **Services**: AuthService (+LoginThrottle), ProductService, InvoiceService (checkout flow incl. permission/feature gates), UsersService, RolesService, TenantFeaturesService, LicenseService, ReportsService, PrintingService, FeatureCatalog.

### Pos.Infrastructure

- **Persistence/**: Dapper repositories implementing all ports; `DbConnectionFactory` (SQLite at `{exe}\data\dev.db`); `AccessControl`; composite transactions for product+base-unit create/update and invoice+stock decrement.
- **Data/**: MigrationRunner (filename-ordered `.sql` from `{exe}\Migrations\`, tracked in `__Migrations`).
- **Security/**: PasswordHasher (PBKDF2-SHA256, 100k iters, `pbkdf2$iter$salt$hash`), TokenService (HMAC-SHA256 bearer tokens, base64url payload.sig, 12h expiry).
- **Devices/**: MachineIdProvider (SHA-256 over machine name/arch/CPU id → 16 hex chars).
- **Printing/**: ReceiptBuilder (GDI+ image rendering w/ Arabic shaping), ImagePrinter (ESC/POS raster), EscPosComposer (Code128 labels), PrinterService (winspool raw P/Invoke), ReceiptPrinter + BarcodeLabelPrinter port adapters.
- **SystemTime/**: SystemClock (UtcNow + local Today).

### Presentation (pos-cs.csproj)

- **CompositionRoot.cs** — the only place wiring concrete types (lazy singletons).
- **Controllers** — thin: bind request models, call one service method, project anonymous objects preserving exact legacy JSON keys, map exceptions via `ApiErrors.From`, keep legacy `[API] ...` console logs.
- **Startup** — unchanged pipeline (migrations → CORS → auth middleware → static files → SPA fallback → Web API) with the new contract resolver.

---

## 3. Exception → HTTP mapping (legacy-compatible)

| Exception | Status | Body message source |
|---|---|---|
| DomainValidationException | 400 | exception message (verbatim legacy strings) |
| NotFoundException | 404 | exception message |
| PermissionDeniedException / FeatureDisabledException | 403 | "Permission denied: X" / "Feature disabled: X" |
| LoginLockedException | 429 | "Too many attempts. Try again in a minute." |
| InsufficientStockException | 400 | exception message |
| anything else | 500 | endpoint-specific fallback ("Failed to fetch products", …) |

Printing endpoints keep their special shapes: success/failure bodies always HTTP-shaped `{success,message[,detail]}` with adapter-chosen status codes (including the legacy quirk that barcode open-failure returns **200**).

---

## 4. Verification Performed

Build: `dotnet build backend-cs/pos-cs.csproj --configuration Release` — 0 warnings, 0 errors (all 4 projects).

Tests: `dotnet test backend-cs/Pos.Tests/Pos.Tests.csproj` — **58 passed / 0 failed**
- Domain rules: pricing/discounts/distribution/profit protection, unit validation, price selection & override detection, license code determinism/format, low-stock policy.
- Application: login throttle (5-failure lockout, window expiry, reset), AuthService (success bundle/token, wrong password, inactive user, token resolution), RolesService (counts, admin lock, unknown keys).

Runtime smoke test (fresh server run against real SQLite):
- `/health`, `/api/license` (public) ✓
- Login `admin`/`1234` → token + access payload {user{id,name,isActive,tenantId}, roles, permissions, features} ✓
- Products: create (auto barcode + base unit + salePrice), get-all, barcode search, name search ranking, paged, count ✓
- Invoices: create (total 15 = 3×5), stock decremented 100→97, `InvoiceDetail` PascalCase key preserved, today list, paged totals {revenue, discounts} ✓
- Error paths verified verbatim: `"Product name is required"`, `"Barcode already in use"`, `"No items provided"`, `"Profit protection: 'Rare Item' would sell below cost price"` (400), product-not-found (404), feature-disabled gate (403), unauthenticated (401 `{"error":"Authentication required"}`), login lockout (429) ✓
- Users/Roles/Permissions/TenantFeatures/Reports response shapes match legacy projections ✓

---

## 5. Tech Debt & Notes for Future Migration (.NET 9 readiness)

1. **Static service locator** (`CompositionRoot`) instead of a DI container — deliberate (no new frameworks); swap for `IServiceCollection` during ASP.NET Core migration.
2. **Bug-compatible quirks intentionally preserved**:
   - `UsersService.Update` writes the user row *before* validating a too-short password (legacy order).
   - `TenantFeatures.SetFeatures` interleaves validate+upsert → partial writes on unknown key mid-list.
   - Product `Create` uses local time for `createdAt`, `Update` uses UTC (legacy inconsistency kept).
   - Barcode print open-failure responds HTTP 200 with `success:false`.
3. **Anonymous-object JSON projection in controllers** keeps wire format stable without touching entities; can be replaced by explicit DTOs later.
4. `ProductRepository.CreateWithBaseUnit` owns the transaction that legacy spread across controller+repos — same atomicity, cleaner ownership.
5. EF6 note: task.md mentioned EF6 but the codebase actually uses Dapper + Microsoft.Data.Sqlite; constraints were adapted accordingly.
6. `LoginThrottle` remains in-memory (resets on restart) as before.
7. Frontend untouched; `src/lib/api.ts` tolerances (`invoice.InvoiceDetail ?? invoice.invoiceDetail`) remain satisfied.

## 6. Test Commands

```powershell
dotnet build backend-cs/pos-cs.csproj --configuration Release   # whole app
dotnet test  backend-cs/Pos.Tests/Pos.Tests.csproj --configuration Release
backend-cs/bin/Release/net48/pos-server.exe                     # runs API on :3001
```
