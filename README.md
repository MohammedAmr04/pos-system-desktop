# POS System (Desktop)

A bilingual (Arabic/English), offline-first point-of-sale desktop application for
retail stores, supermarkets, groceries, wholesalers and pharmacies. It pairs a
**C# / .NET self-host backend** with a **Next.js static frontend** served from the
same process — no cloud, no internet required.

The backend runs on **port 3001** and serves both the REST API (`/api/*`) and the
built frontend. The database is a single SQLite file stored in the `data/` folder.

---

## Features

### Point of Sale (POS)
- Fast product search and barcode scanning (exact barcode auto-adds to cart).
- **Retail / Wholesale price mode** — switching mode re-prices every cart line.
- **Multiple selling units** — products can be sold as Piece, Pack, Carton, etc.,
  each with its own barcode and price; stock is always counted in the base unit.
- **Per-line edit dialog**: override unit price (original shown struck through),
  optional cashier reason note, percentage or fixed discount, quantity changes.
- **Smart discounts** — invoice-level discount (percentage or fixed) applied only
  to products that allow discounts, distributed proportionally across lines.
- **Profit protection** — selling below cost price is blocked on both frontend
  and backend.
- **Insufficient stock protection** — over-selling is rejected with a clear
  message instead of silently failing.
- **Unknown-barcode dialog** — create a new product or link to an existing one
  right from the checkout screen.

### Products
- Product catalog with search, server-side paging, low-stock thresholds, notes,
  and per-product "allow discount" flag.
- **Product Units section** — add/edit/delete selling units with quantity factor,
  retail and wholesale prices, and unit-scoped barcodes (multiple barcodes per
  unit, one default).
- **Categories & Brands** — shared master data; products reference a Category
  and Brand (both optional); management screens with activate/deactivate
  (never deleted, FK preserved).
- **Unit Master** — shared unit definitions (Piece, Kilogram, etc.) linked to
  ProductUnit; used units cannot be deleted.
- Auto-generated 12-digit barcode when none is provided.
- **Hide from POS** — products can be marked `isHiddenFromPOS = true` to
  exclude them from POS search and cart; the backend `GET /api/products/pos`
  and search route both filter them out automatically.
- Low Stock Report screen using each product's own threshold.

### Invoices
- Auto-incrementing invoice numbers and full history with server-side paging.
- Search by invoice number, date-range filters with quick presets
  (today / week / month / all), revenue and discount summary cards.
- Invoice details show real invoice number, unit names, struck-through override
  prices, discounts and price-edit notes.
- Invoice lifecycle: **Draft** (editable, no stock/revenue effects) → **Posted**
  (full pipeline: FIFO cost, stock deduction, client balance) → **Cancelled**
  (safe reversal, nothing deleted).

### Purchases
- Purchase invoice management with full lifecycle: Draft → Posted → Cancelled.
- Draft purchases are editable with no side-effects; Posted purchases record
  stock movement via the Stock Movement ledger and update supplier balance.
- Lines support unit-scoped barcodes, quantity factors, and per-unit cost/sell
  prices.
- Editing a Posted purchase does a transactional reversal + re-application.
- Supplier is optional for cash purchases, required for credit.

### Clients & Suppliers
- **Clients** — customer master data with account statements showing
  invoices, payments, and returns with running balance.
- **Suppliers** — vendor master data with account statements showing
  purchase invoices, payments, and returns with running balance.
- Deactivated parties are never deleted; existing references are preserved.

### Payments
- Independent payment transactions linked to an Invoice, Client, or Supplier.
- Payment methods: Cash, Visa, Other.
- Payment status (Paid / Partially Paid / Unpaid) is **derived** from totals —
  never stored as a mutable field.
- Record payment dialog reachable from invoice, client, and supplier screens.

### Returns
- **Sales Returns** — create from a posted invoice (full or partial);
  validates returnable quantity (sold − already returned); restores stock via
  base-unit conversion and original FIFO cost allocation; original invoice
  transitions to Partially / Fully Returned.
- **Purchase Returns** — create from a posted purchase invoice; reverses stock,
  cost layers, and supplier balance.

### Shifts
- Open/close cash shift sessions with opening cash amount.
- **Expected Cash** computed on close = opening + cash sales + other cash in
  − cash returns − cash expenses.
- POS selling requires an open shift; shift indicator visible in POS.
- Cash sales, payments, and expenses are all linked to the active shift.

### Expenses
- Expense transactions with categories (Income / Outcome subclass).
- Each expense is linked to the active shift and immediately affects
  the shift's Expected Cash.
- Expense categories are manageable via the settings screen.

### Reports
All reports use date-range filters (today / week / month / custom) and
server-side aggregation:
- **Sales** — by date/product/category/brand/unit/cashier/payment method.
- **Purchases** — by date/supplier/product.
- **Inventory** — current stock levels, movement, and valuation.
- **Profit** — Revenue − FIFO COGS with gross margin per product/category.
- **Returns** — sales and purchase returns with values.
- **Expenses** — by category and date range.
- **Cash** — shift-based cash reconciliation report.
- **Low Stock** — products below their individual threshold.

### Printer Settings
- Configurable receipt printer name, paper width, copies, auto-cut, cash
  drawer trigger, receipt header/footer text, and store info (name, address,
  phone, tax ID).
- Settings persisted in DB via the Printing API and injected into
  `ReceiptPrinter` and `BarcodeLabelPrinter` at runtime.

### Printing
- ESC/POS thermal receipt printing with native **Arabic shaping and BiDi**
  (the receipt is rendered as an image via Windows GDI+, so Arabic text is
  flawless).
- Barcode label printing.
- Printer name configurable via the `PRINTER_NAME` environment variable
  (default `Xprinter`).

### Authentication & Access Control
- **Username + password login** (replaces the PIN code) with PBKDF2 password
  hashing and rate-limited attempts (5 failures lock out for 60 seconds).
- **Roles** — Admin, Manager and Cashier system roles plus custom roles with a
  per-permission matrix.
- **Permissions** — 19 permission keys enforced on the backend (attribute-based)
  and mirrored in the UI: menus, buttons and sections hide without the right
  permission.
- **Tenant features** — 9 per-restaurant capability switches combined with
  permissions (`AND`): a capability is usable only when both are granted.
- Default login: `admin` / `1234` (change after first login).

### Settings
- **Users** — manage accounts, assign roles, activate/deactivate.
- **Roles** — create/edit/delete roles and assign permissions.
- **Permissions** — grouped reference of all permission keys.
- **Features** — per-tenant feature toggles; changes apply immediately.

### License & Activation
- Machine-ID-based activation with a lock screen gate on first boot.
- Unlocking requires the `license.manage` permission and logs the acting user.

### Dashboard
- Today's revenue, sales count, discounts given, and total product count with a
  quick action to open the POS.

---

## Tech Stack

| Layer     | Technology |
|-----------|------------|
| Backend   | C# / .NET Framework 4.8, OWIN self-host, ASP.NET Web API |
| Auth      | Bearer tokens (OWIN middleware), PBKDF2 password hashing |
| Database  | SQLite via Dapper (`Microsoft.Data.Sqlite`), SQL migration files |
| Printing  | winspool.drv P/Invoke (RawPrinterHelper), System.Drawing receipt rendering |
| Frontend  | Next.js 16 (static export), React 19, TypeScript, Tailwind CSS 4 |
| UI        | shadcn/ui (base-ui port), @base-ui/react, lucide-react |
| State     | Zustand (POS store), TanStack Query (server state), react-hook-form + zod (forms) |
| i18n      | next-intl (Arabic-first, RTL) |

---

## Project Structure

```
backend-cs/                  # .NET Framework 4.8 backend
  Controllers/               # Auth, Products, Invoices, Purchases, License, Printing, Health, Reports
  Database/Migrations/       # 001_init .. 013_username_password (auto-applied on start)
  Models/                    # User, Role, Permission, TenantFeature, Product, Invoice
  Repositories/              # Dapper data access (Auth, Users, Roles, ...)
  Services/                  # Auth, Authorization, Receipt, Printer, Barcode, License
  Middleware/                # ApiAuthMiddleware (bearer token parsing)
  Attributes/                # RequirePermissionAttribute (permission + feature checks)
  Printers/                  # ESC/POS image + barcode output
  Builders/ReceiptBuilder.cs # Receipt image construction
src/                        # Next.js frontend (client components only)
  app/[locale]/             # Pages: /, /pos, /products, /invoices, /low-stock,
                             #   /purchases, /returns, /reports, /shifts, /expenses,
                             #   /clients, /suppliers, /settings/{users,roles,permissions,features}
  components/
    ui/                     # shadcn/ui base components (Button, Dialog, Input, Table, etc.)
    common/                  # Shared components (TooltipIconButton, TableBuilder, DataPagination, etc.)
    layouts/                # Dashboard layout with sidebar
  hooks/                    # TanStack Query hooks (use-*.ts), each exports *Keys object
  actions/                  # Mutations (create/update/delete + queryClient.invalidateQueries)
  types/domain/             # TypeScript domain types
  lib/                      # API client, constants, RTL utilities, barcode helpers
  store/                    # Zustand stores (POS cart store)
messages/                   # next-intl translations (ar.json)
docs/                       # Feature specs and release notes
CHANGELOG.md                # Version history
```

---

## Frontend Conventions

### Data Layer (TanStack Query)

Every feature has a hook file (`src/hooks/use-*.ts`) that exports:
- A `*Keys` object with all query key arrays
- Typed query hooks (`useX`, `useXPage`, `useXById`, etc.)
- Each hook returns `UseQueryResult<T>` with `data`, `isFetching`, `isError`, etc.

```ts
// Example: src/hooks/use-products.ts
export const productsKeys = {
  all: ["products"] as const,
  list: () => [...productsKeys.all, "list"] as const,
  detail: (id: string) => [...productsKeys.all, "detail", id] as const,
  paged: (page: number, pageSize: number, filter: ProductFilter) =>
    [...productsKeys.all, "paged", page, pageSize, filter] as const,
}
export function useProductsPage(page: number, pageSize: number, filter: ProductFilter) {
  return useQuery({
    queryKey: productsKeys.paged(page, pageSize, filter),
    queryFn: () => listProducts(page, pageSize, filter),
    enabled: page > 0,
  })
}
```

### Mutations

All mutations live in `src/actions/*.ts`. Each action:
1. Calls the API
2. On success, calls `queryClient.invalidateQueries({ queryKey: *Keys.all })`

```ts
// Example: src/actions/products.actions.ts
export async function createProduct(data: CreateProductInput) {
  const result = await api.post("/api/products", data)
  queryClient.invalidateQueries({ queryKey: productsKeys.all })
  return result
}
```

### Forms (react-hook-form + zod)

Forms use `react-hook-form` with `zodResolver` and inline error messages:

```tsx
const schema = z.object({
  name: z.string().trim().min(1, t("fieldRequired")),
  buyPrice: z.string().trim().min(1, t("fieldRequired"))
    .refine(v => Number.isFinite(parseFloat(v)) && parseFloat(v) >= 0, t("invalidNumber")),
  // ...
})
type FormValues = z.infer<typeof schema>

const { register, handleSubmit, reset, formState: { errors } } = useForm<FormValues>({
  resolver: zodResolver(schema),
  defaultValues: { name: initialData?.name ?? "", ... },
})
// ...
return (
  <form onSubmit={handleSubmit(onSubmit)}>
    <Input {...register("name")} />
    {errors.name && <p className="text-sm text-destructive">{errors.name.message}</p>}
  </form>
)
```

### RTL — Numbers and Text Direction

- Numbers are always **LTR** even in an RTL interface. Use `dir="ltr"` on all currency/quantity displays:
  ```tsx
  <span dir="ltr">{amount.toFixed(2)}</span>
  ```
- Product barcodes, phone numbers, and any Latin-character strings also use `dir="ltr"`.
- Sidebar uses `side="right"` (shadcn `Sidebar` component) for RTL physical positioning.

### Icon Buttons — TooltipIconButton

All icon-only buttons must use `TooltipIconButton` from `@/components/common/tooltip-icon-button` instead of bare `<Button size="icon">`. The `label` prop is mandatory (it becomes both the `aria-label` and the tooltip text — always pass a translated string from `t("key")`).

```tsx
// Wrong — no accessible label
<Button size="icon" onClick={handleEdit}><Pencil /></Button>

// Correct
<TooltipIconButton label={t("edit")} onClick={handleEdit}>
  <Pencil className="h-4 w-4" />
</TooltipIconButton>
```

### react-hooks — No setState in Effects

ESLint rule `set-state-in-effect` (react-hooks v6) forbids synchronous `setState` inside `useEffect`. Compliant patterns:

1. **Event-driven fetchQuery**: do async work + `setState` in event handlers, not effects
2. **Derived state via `useMemo`**: compute from props/state, never mirror via effect
3. **Keyed remount**: `<Component key={id} />` with props as initial state — no sync effect needed
4. **Render-phase adjustment**: check condition during render body, call setter directly (guard with `lastHydratedId` ref to avoid loops)

```tsx
// Wrong — sync setState in effect
useEffect(() => { setCount(data.count) }, [data.count])

// Correct — derived state via useMemo
const count = useMemo(() => data?.count ?? 0, [data?.count])
```

### Cache Key Conventions

All TanStack Query cache keys follow a consistent pattern exported as `*Keys`:

| Hook file | Keys object | Example |
|-----------|------------|---------|
| `use-products.ts` | `productsKeys` | `productsKeys.paged(1, 20, { q: "foo" })` |
| `use-invoices.ts` | `invoicesKeys` | `invoicesKeys.detail(id)` |
| `use-purchases.ts` | `purchasesKeys` | `purchasesKeys.paged(1, 10, { status: "posted" })` |
| `use-reports.ts` | `reportsKeys` | `reportsKeys.sales(from, to)` |
| `use-shifts.ts` | `shiftsKeys` | `shiftsKeys.active()` |

Always invalidate `*.all` after mutations: `queryClient.invalidateQueries({ queryKey: productsKeys.all })`.

---

## Quick Start (Development)

### 1. Run the backend

Requires .NET Framework 4.8 targeting (Windows).

```powershell
dotnet build backend-cs/pos-cs.csproj --configuration Release
# run the produced executable, e.g.:
& .\backend-cs\bin\Release\net48\pos-server.exe
```

The server listens on `http://localhost:3001`. The database is created in
`data/` next to the executable and migrations are applied automatically.
Sign in with the default account **`admin` / `1234`**.

Optional environment variables:

| Variable        | Default    | Purpose |
|-----------------|------------|---------|
| `API_PORT`      | `3001`     | Server port |
| `PRINTER_NAME`  | `Xprinter` | Thermal printer name for receipts/barcodes |

### 2. Run the frontend (dev mode)

```powershell
npm install
npm run dev
```

Open http://localhost:3000 (dev) or http://localhost:3001 (production). The API
base URL defaults to `http://localhost:3001` and can be overridden with
`NEXT_PUBLIC_API_URL`.

---

## Build & Release

The backend serves the static frontend, so there is no separate web server.

`npm run build` runs `next build` (output → `out/`) and automatically copies
the result into `backend-cs/bin/Release/net48/wwwroot` (preserving `logo.jpeg`).

1. Build everything:

   ```powershell
   npm run build
   dotnet build backend-cs/pos-cs.csproj --configuration Release
   ```

2. Package `pos-server.exe`, `Migrations/`, `wwwroot/` and `start.bat` together.
   `start.bat` launches the server and opens `http://localhost:3001`.

> **Upgrade note:** when updating an existing installation, replace the
> executable, `Migrations/` and `wwwroot/` but **keep the `data/` folder** — it
> contains the database. Migrations run automatically on start. The default
> login after upgrading is `admin` / `1234`.

---

## Commands

| Command | Description |
|---------|-------------|
| `npm run dev` | Start Next.js dev server |
| `npm run build` | Static export build → `out/` + auto-copy to `backend-cs/bin/Release/net48/wwwroot` |
| `npm run lint` | Run ESLint (0 errors required) |
| `npm test` | Run vitest unit tests |
| `dotnet build backend-cs/pos-cs.csproj --configuration Release` | Build the backend |

---

## Database

SQLite with versioned SQL migrations in `backend-cs/Database/Migrations/`,
applied automatically on server start. The server **exits on migration failure**
to avoid serving a partially upgraded database.

| Migration | Change |
|-----------|--------|
| `001_init` | Product, Invoice, InvoiceDetail |
| `002_settings` | Settings (machine ID / license) |
| `003_add_notes` | Product.notes |
| `004_add_invoice_number` | Invoice.invoiceNumber + auto-increment trigger |
| `005_add_discount_lowstock` | Discount eligibility, thresholds, discount columns |
| `006_product_barcodes` | ProductBarcode table, drop Product.barcode |
| `007_product_units` | ProductUnit table, unit-scoped barcodes, price mode |
| `008_price_edit_note` | InvoiceDetail.PriceEditNote |
| `009_auth_schema` | User, Role, Permission, RolePermission, UserRole, Tenant, TenantFeature |
| `010_seed_permissions` | 19 permission keys |
| `011_seed_roles` | Admin / Manager / Cashier roles + role-permission assignments |
| `012_seed_tenant` | Default tenant + 9 feature flags + admin user |
| `013_username_password` | Replace `pinHash` with `username` + PBKDF2 `passwordHash` |
| `014_categories` | Category table |
| `015_brands` | Brand table |
| `016_units` | Unit master table; ProductUnit.unitId FK |
| `017_suppliers_clients` | Supplier, Client, SupplierBalance, ClientBalance |
| `018_stock_movements` | StockMovement ledger (productId, quantity, type, reference, timestamp) |
| `019_purchases` | PurchaseInvoice, PurchaseInvoiceItem, PurchaseInvoiceStatus |
| `020_cost_layers` | CostLayer, SaleCostAllocation; InvoiceDetail.totalCost, quantityFactor |
| `021_payments` | Payment (amount, method, date, invoice/client/supplier reference) |
| `022_sales_lifecycle` | Invoice.status, clientId, paymentMethod, userId; backfill existing to posted |
| `023_sales_returns` | SalesReturn, SalesReturnItem, OriginalInvoiceId |
| `024_purchase_returns` | PurchaseReturn, PurchaseReturnItem, OriginalPurchaseInvoiceId |
| `025_shifts` | Shift (openedBy, openingCash, openedAt, closedAt, closingCash, expectedCash, status) |
| `026_expenses` | Expense, ExpenseCategory, shiftId FK |
| `027_printer_settings` | PrinterSettings (printer name, paper width, copies, auto-cut, header/footer, store info) |

---

## API Overview

All endpoints live under `http://localhost:3001/api`.

| Area        | Endpoints |
|-------------|-----------|
| Auth        | `POST /auth/login`, `GET /auth/me` |
| Users       | `GET/POST /users`, `PUT /users/{id}` |
| Roles       | `GET/POST /roles`, `GET/PUT/DELETE /roles/{id}`, `GET/PUT /roles/{id}/permissions` |
| Permissions | `GET /permissions` |
| Features    | `GET/PUT /tenant/features` |
| Products    | `GET /products`, `GET /products/{id}`, `POST /products`, `PUT /products/{id}`, `DELETE /products/{id}`, `GET /products/paged`, `GET /products/search`, `GET /products/count`, `GET /products/pos` |
| Categories  | `GET/POST /categories`, `GET/PUT/DELETE /categories/{id}` |
| Brands      | `GET/POST /brands`, `GET/PUT/DELETE /brands/{id}` |
| Units       | `GET/POST /units`, `GET/PUT/DELETE /units/{id}` |
| Units (product) | `POST/PUT/DELETE /products/{id}/units[/{unitId}]`, `POST/DELETE /products/{id}/units/{unitId}/barcodes[/{barcodeId}]`, `PUT .../barcodes/{barcodeId}/default` |
| Invoices    | `GET /invoices`, `POST /invoices`, `GET /invoices/{id}`, `GET /invoices/paged`, `GET /invoices/filter` |
| Purchases   | `GET /purchases`, `POST /purchases`, `GET /purchases/{id}`, `GET /purchases/paged`, `POST/PUT /purchases/{id}/post`, `POST /purchases/{id}/cancel` |
| Sales Returns | `POST /returns/sale`, `GET /returns/sale` |
| Purchase Returns | `POST /returns/purchase`, `GET /returns/purchase` |
| Clients     | `GET/POST /clients`, `GET/PUT/DELETE /clients/{id}`, `GET /clients/{id}/statement` |
| Suppliers   | `GET/POST /suppliers`, `GET/PUT/DELETE /suppliers/{id}`, `GET /suppliers/{id}/statement` |
| Payments    | `GET /payments`, `POST /payments`, `GET /payments/client/{id}`, `GET /payments/supplier/{id}` |
| Shifts      | `GET /shifts`, `POST /shifts/open`, `PUT /shifts/{id}/close` |
| Expenses    | `GET/POST /expenses`, `DELETE /expenses/{id}`, `GET /expenses/categories` |
| Reports     | `GET /reports/low-stock`, `GET /reports/sales`, `GET /reports/purchases`, `GET /reports/profit`, `GET /reports/returns`, `GET /reports/expenses`, `GET /reports/inventory`, `GET /reports/cash` |
| Printer Settings | `GET/PUT /printer-settings` |
| Printing    | `POST /printing/print`, `POST /printing/print-barcode` |
| License     | `GET /license`, `POST /license/unlock` |
| Health      | `GET /health` |

> All endpoints except `POST /auth/login` and `GET /license` require a
> `Bearer <token>` header issued by `POST /auth/login`.

---

## Localization

The UI is **Arabic-first with RTL** layout. Translations live in `messages/`
(`ar.json`) and use next-intl. Only the `ar` locale is built for production;
additional locales can be added by adding a new message file and a static param.

Key namespaces: `App`, `Dashboard`, `POS`, `Products`, `Invoices`, `Purchases`,
`Returns`, `Reports`, `Shifts`, `Expenses`, `Payments`, `Categories`, `Brands`,
`UnitsMaster`, `Clients`, `Suppliers`, `Roles`, `Permissions`, `Features`,
`Users`, `Sidebar`, `Auth`, `Access`, `Common`, `LowStock`, `Printing`, `License`.

---

## Documentation

- `docs/features/` — feature specs (multiple barcodes, advanced pricing & units).
- `docs/core_features/` — core feature specs (license & subscription).
- `docs/FEATURES.md` — the tenant feature catalog and how it gates the UI/backend.
- `docs/PERMISSIONS.md` — the 19 permission keys grouped by resource.
- `docs/AUTHORIZATION-ANALYSIS.md` — roles/permissions/features architecture.
- `docs/release/` — per-version release notes.
- `CHANGELOG.md` — full version history.
- `PRINTING-ANALYSIS.md` — receipt-printing implementation analysis.

---

## License

Proprietary. The application is activated per machine via a license key flow
implemented in the `License` API and the license lock screen.
