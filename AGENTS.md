<!-- BEGIN:nextjs-agent-rules -->
# This is NOT the Next.js you know

This version has breaking changes — APIs, conventions, and file structure may all differ from your training data. Read the relevant guide in `node_modules/next/dist/docs/` before writing any code. Heed deprecation notices.
<!-- END:nextjs-agent-rules -->

# Architecture (V9 — C# Backend + Next.js Static Export)

- **Backend**: .NET Framework 4.8 OWIN self-host (port 3001) at `backend-cs/`
  - Dapper + SQLite, custom SQL migrations (`Database/Migrations/`)
  - Controllers: Products, Invoices, License, Printing, Health
  - Print: RawPrinterHelper (winspool.drv P/Invoke) + ReceiptService (ESC/POS)
  - Build: `dotnet build backend-cs/pos-cs.csproj --configuration Release`
- **Frontend**: Next.js static export at `src/`
  - All pages are client components (no Server Actions or SSR)
  - Types: `src/types/domain/domain.types.ts`
  - API client: `src/lib/api.ts` — typed fetch helpers to `http://localhost:3001/api/*`
  - Data layer: TanStack Query hooks in `src/hooks/use-*.ts`; cache keys exported as `*Keys` objects
  - Mutations: `src/actions/*.ts` — call server action then `queryClient.invalidateQueries({queryKey: *Keys.all})`
  - Forms: react-hook-form + zod (use `zodResolver` + `useForm`); inline `<p className="text-sm text-destructive">` for errors
  - UI kit: shadcn base-ui port in `src/components/ui/`; shared components in `src/components/common/`
  - Sidebar: shadcn `Sidebar` kit (`src/components/ui/sidebar.tsx`) in `dashboard-layout.tsx`
- **Release**: `V8-Version-8.zip` at project root
  - `start.bat` launches `backend\pos-server.exe` then opens `http://localhost:3001`
  - The server serves both the API (`/api/*`) and static frontend (`/ar/` etc.)
  - Database lives in `data/` folder relative to the executable

# Conventions

- **All filtering/sorting/pagination is server-side** — the only exception is low-stock report snapshot (local `useMemo` filter).
- **All mutations go through TanStack Query**: call the action/mutation, then `invalidateQueries({queryKey: xKeys.all})` on success.
- **Cache keys**: every feature exports `*Keys` (e.g. `invoicesKeys`, `productsKeys`, `shiftsKeys`). Always invalidate `*.all` after mutation. Paged keys: `*.paged(page, pageSize, filter)`.
- **RTL**: Arabic is the primary locale. Sidebar uses `side="right"` (physical positioning). Numbers use `dir="ltr"` on inputs.
- **Tooltips**: use `TooltipIconButton` (from `@/components/common/tooltip-icon-button`) instead of bare `<Button size="icon">`. It requires a `label` prop (mandatory, replaces `aria-label`/`title`).
- **Translations**: `messages/ar.json` at project root. Namespaces correspond to page areas (Products, Invoices, Purchases, Returns, Reports, etc.). `useTranslations("Namespace")`.
- **Shared data hooks**: e.g. `useAllProducts()`, `useAllBrands()`, `useAllCategories()`, `useAllUnits()`, `useActiveShift()`.

# Code style

- **No comments** unless asked. No emojis unless asked.
- **No unused imports** — run `npx tsc --noEmit && npm run lint` before committing.
- **PowerShell**: always use `-LiteralPath` for paths containing `[locale]` brackets.
- **`rg` is not available** — use the `grep` tool or `Select-String` for searching.

# react-hooks rules (eslint v6 `set-state-in-effect`)

Sync `setState` inside `useEffect` body (not in an async callback) is an error. Compliant patterns:
1. **Event-driven fetchQuery**: do async work + `setState` in event handlers/`useCallback`, not in effects.
2. **Derived state**: use `useMemo` instead of mirroring props in state via effect.
3. **Keyed remount**: `<Component key={id} />` with props as default initial state (no sync effect).
4. **Render-phase adjustment**: check condition during render body (e.g. `if (existing && lastId !== existing.id)` then call setter) — guard with a ref/lastId to avoid infinite loops.

# Verification

```bash
npx tsc --noEmit && npm run lint && npm test && npm run build
```
