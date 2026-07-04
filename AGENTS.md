<!-- BEGIN:nextjs-agent-rules -->
# This is NOT the Next.js you know

This version has breaking changes — APIs, conventions, and file structure may all differ from your training data. Read the relevant guide in `node_modules/next/dist/docs/` before writing any code. Heed deprecation notices.
<!-- END:nextjs-agent-rules -->

# Architecture (V8 — C# Backend + Next.js Static Export)

- **Backend**: .NET Framework 4.8 OWIN self-host (port 3001) at `backend-cs/`
  - Dapper + SQLite, custom SQL migrations (`Database/Migrations/`)
  - Controllers: Products, Invoices, License, Printing, Health
  - Print: RawPrinterHelper (winspool.drv P/Invoke) + ReceiptService (ESC/POS)
  - Build: `dotnet build backend-cs/pos-cs.csproj --configuration Release`
- **Frontend**: Next.js static export at `src/`
  - All pages are client components (no Server Actions or SSR)
  - API client: `src/lib/api.ts` — typed fetch wrappers to `http://localhost:3001/api/*`
  - Deleted: `api-server/`, `prisma/`, `scripts/` (no more Node.js backend)
- **Release**: `V8-Version-8.zip` at project root
  - `start.bat` launches `backend\pos-server.exe` then opens `http://localhost:3001`
  - The server serves both the API (`/api/*`) and static frontend (`/ar/` etc.)
  - Database lives in `data/` folder relative to the executable
