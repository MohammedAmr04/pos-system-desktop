# ROADMAP

## Shipped Features

### v2 Core Infrastructure ✅
- [x] **Master Data** — Categories, Brands, Unit master with management screens
- [x] **Suppliers & Clients** — CRUD + account statements
- [x] **Stock Movement Ledger** — unified inventory tracking for all transactions
- [x] **Purchase Invoices** — Draft → Posted → Cancelled lifecycle
- [x] **FIFO Cost Layers** — `FifoAllocator` domain rule with unit tests
- [x] **Payments** — independent transactions linked to invoice/client/supplier
- [x] **Sales Returns** — from posted invoice with FIFO cost restoration
- [x] **Purchase Returns** — from posted purchase with stock/cost reversal
- [x] **Shifts** — open/close with Expected Cash computation
- [x] **Expenses** — with categories, linked to active shift
- [x] **Reports** — sales, purchases, inventory, profit, returns, expenses, cash, low-stock
- [x] **Printer Settings** — configurable via DB, injected at runtime
- [x] **POS search filtering** — `isHiddenFromPOS` field; backend `/api/products/pos` and search route filter hidden products

### v2 Frontend UI ✅
- [x] Full v2 pages: brands, categories, clients, expenses, payments, purchases, reports, returns, printing settings, shifts, suppliers, units
- [x] TanStack Query migration + new UI kit (shadcn base-ui)
- [x] Arabic labels in Select triggers for unmatched values

### Phase 6 — POS Sales Invoice Lifecycle ✅
- [x] **Save/Resume Draft** — POS cart can be saved as draft and resumed later
- [x] **Drafts list** — UI to list and resume saved drafts before posting
- [x] **Client picker on POS** — mandatory client for credit sales, optional for cash
- [x] **Payment method selection** — Cash / Visa / Other on the POS checkout screen
- [x] **Open-shift guard** — POS blocks selling when no shift is open
- [x] **Decimal quantities** — allow fractional quantities on POS cart lines
- [x] **Status/payment badges** in invoices list
- [x] **Record payment** action from invoice details dialog
- [x] **Cost/Profit display** in invoice details (requires Phase 4 FIFO data)
- [x] Invoice status (draft/posted/cancelled) + ClientId + PaymentMethod on invoice
- [x] Draft linkage (drafts are `Invoice` rows with `status='draft'`; resume via `PUT /api/invoices/{id}` then `POST /api/invoices/{id}/post`)
- [x] Active-shift enforcement on invoice creation/posting (`RequireActiveShiftId`) + POS frontend guard
- [x] Payment method persisted end-to-end (card/bank_transfer stored on invoice + auto-payment mirrors the method)

---

## Future Ideas

> Things that have been discussed or are worth considering but are not yet planned.

### High Priority
- **Bulk product import** — CSV/XLSX import for initial inventory setup
- **Product barcode label printing** — print shelf labels from product list
- **Dashboard improvements** — today's top-selling products, recent invoices widget
- **Invoice search by client name** — expand invoice search to include client info

### Medium Priority
- **Multi-warehouse** — assign products to warehouse locations; stock per warehouse
- **Customer-specific pricing** — price lists per client or client tier
- **Margin alerts** — notify when a product's margin drops below a threshold
- **End-of-day report** — summary printed at shift close
- **Tax support** — configurable tax rate per product or invoice
- **Backup/restore** — export/import SQLite database from the UI

### Lower Priority / Nice-to-Have
- **Dark mode** — theme toggle (system / light / dark)
- **Kitchen display** — separate view for kitchen/fulfillment orders
- **Multi-branch** — same database with branch separation
- **Audit log UI** — view who changed what and when
- **Barcode scanner setup** — test scanner input on a settings page
- **Expiry tracking** — batch/expiry date on stock; expiry report

### Out of Scope (per spec)
- Bundle / composite products
- Automatic price/margin calculation
- Supplier-specific pricing rules
- Complex accounting / GL
- Manual returns without an original invoice
