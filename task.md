You are a senior full-stack engineer specializing in:

- Next.js App Router
- next-intl
- React Query / SWR
- C#
- Dapper
- SQLite
- REST API design

The Node.js backend has been migrated to a native C# backend.

Your task is to perform a complete API compatibility audit.

The frontend must require ZERO modifications after the migration.

Treat the original frontend as the source of truth.

Do not implement temporary fixes.

--------------------------------------------------
Phase 1 — Explore
--------------------------------------------------

Explore the entire frontend.

Identify every API request.

For every endpoint record:

- URL
- HTTP Method
- Request Body
- Query Parameters
- Response Shape
- Expected JSON Contract

Then inspect the C# implementation for the same endpoint.

--------------------------------------------------
Phase 2 — Contract Comparison
--------------------------------------------------

Compare every endpoint between:

Frontend expectations

vs

C# backend responses.

Verify:

- property names
- nesting
- arrays
- object structure
- nullable values
- date format
- number format
- boolean values
- enum values
- camelCase
- response wrappers
- pagination
- metadata

The frontend should never need to transform backend data.

Restore the original API contract whenever differences are found.

--------------------------------------------------
Phase 3 — DTO Validation
--------------------------------------------------

Inspect:

Controllers

Repositories

Models

DTOs

Mapping logic

Manual mapping

Ensure every response DTO exactly matches what the frontend expects.

--------------------------------------------------
Phase 4 — CRUD Validation
--------------------------------------------------

Test every CRUD flow.

Products

Categories

Invoices

Invoice Details

Settings

POS

License

Any remaining modules.

For every operation verify:

Create

Update

Delete

List

Details

Search

Pagination

Filters

--------------------------------------------------
Phase 5 — Automatic Refresh
--------------------------------------------------

After every successful mutation verify that the UI refreshes correctly.

Examples:

Create Product

↓

Product list refreshes automatically

Update Product

↓

Table refreshes automatically

Delete Product

↓

Table refreshes automatically

Create Invoice

↓

Invoices list refreshes automatically

Any mutation should update the UI without requiring a manual browser refresh.

Inspect:

React Query

SWR

Server Actions

fetch()

custom hooks

cache invalidation

router.refresh()

query invalidation

revalidation

state updates

Find why the UI remains stale after requests.

Fix the root cause.

Do not use window.location.reload().

--------------------------------------------------
Phase 6 — Invoice Details
--------------------------------------------------

Investigate why invoice details are not rendered.

The backend returns the data.

The modal opens.

Totals appear.

Products are missing.

Compare the returned JSON with the frontend expectations.

Restore compatibility.

--------------------------------------------------
Phase 7 — Validation
--------------------------------------------------

Run the application.

Test every page manually.

Verify:

Products

Invoices

Invoice Details

POS

Dashboard

Settings

Search

Create

Edit

Delete

Everything should behave exactly like the original Node.js backend.

--------------------------------------------------
Final Report
--------------------------------------------------

Provide:

• API contract mismatches found

• DTO mismatches

• Mapping issues

• CRUD issues

• Cache invalidation issues

• Refresh issues

• Invoice Details fixes

• Files modified

• Validation performed

Do not stop until the frontend behaves identically to the original Express + Prisma implementation while using the native C# backend.