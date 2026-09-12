# Future Features

This document contains product ideas intentionally deferred from POS v2 and the first Dry Clean release. They should be prioritized using real customer demand and operational evidence.

## 1. Product and Customer Import/Export

### Value

Reduce manual setup time and make migration from an existing system practical.

### Scope

- Import products, units, barcodes, categories, brands, customers, and suppliers from CSV or Excel.
- Preview rows before saving.
- Report invalid rows with field-level errors.
- Detect duplicate barcodes and customer identifiers.
- Choose whether matching records are updated or rejected.
- Export filtered data using the same server-side filters as the application.
- Record import and export activity in the audit log.

### Safety Requirements

- Never partially import silently.
- Provide a summary of created, updated, skipped, and failed rows.
- Require a backup before large imports.

## 2. Excel and PDF Report Export

### Value

Allow owners and accountants to share reports and keep periodic records outside the application.

### Reports

- Sales.
- Purchases.
- Profit and FIFO cost.
- Inventory valuation.
- Returns.
- Expenses.
- Cash and shifts.
- Client and supplier balances.
- Dry Clean orders and outstanding balances.

### Requirements

- Export the same filtered data shown in the application.
- Include store name, report title, date range, generation time, and totals.
- Support Arabic text, RTL layout, and printable page sizes.
- Keep export calculations identical to on-screen calculations.

## 3. Performance Improvements

### Value

Keep search, checkout, and reports responsive as the database grows.

### Candidate Work

- Add and verify SQLite indexes for common searches and date ranges.
- Measure product search, invoice search, and report generation times.
- Reduce unnecessary payload fields.
- Keep pagination and filtering server-side.
- Test with realistic large datasets.
- Add slow-query or slow-report diagnostics to logs.

### Done When

Performance targets are defined from real hardware and verified with repeatable test data.

## 4. User-Friendly Installer and Updater

### Value

Allow a store owner to install and update the application without developer assistance.

### Scope

- Install the backend, static frontend, migrations, and required runtime files.
- Create a desktop shortcut.
- Start the server and open the browser only after health check success.
- Create a safety backup before an upgrade.
- Preserve the data folder during upgrades.
- Show clear errors and recovery instructions.
- Provide an uninstall path that protects business data by default.

## 5. Multi-Branch Support

### Value

Manage multiple stores while keeping stock, cash, users, and reports separated.

### Scope

- Branch-specific inventory.
- Users assigned to branches.
- Branch-specific shifts and cash reconciliation.
- Transfers between branches.
- Reports per branch and consolidated reports.
- Branch-scoped permissions.

### Prerequisites

- Reliable backup and restore.
- Complete audit logging.
- Inventory adjustments.
- A clear concurrency and synchronization design.

## 6. Cloud Sync

### Value

Synchronize branches and protect data beyond a single Windows machine.

### Scope

- Central backend or synchronization service.
- Encrypted transport.
- Offline operation and retry behavior.
- Conflict detection and resolution.
- Central backups.
- Tenant and branch isolation.
- Explicit privacy and retention policy.

This is a product and infrastructure change, not just a frontend feature.

## 7. Expiry and Batch Tracking

### Value

Support products whose cost, expiry, or traceability depends on a batch.

### Scope

- Capture batch number and expiry during receiving.
- Track stock by batch.
- Report expired and soon-to-expire stock.
- Support FIFO or FEFO selection by product policy.
- Preserve batch information through returns and adjustments.

## 8. Accounting Integration

### Value

Reduce duplicate work for accountants and support formal financial workflows.

### Possible Scope

- Export sales, purchases, payments, expenses, and taxes.
- Customer and supplier ledger export.
- Daily journal summaries.
- Mapping POS accounts to accounting accounts.
- Integration with a selected accounting product.

The exact accounting system and local tax requirements must be selected before implementation.

## 9. Dry Clean Expansion

Possible additions after the first Dry Clean pilot:

- Garment photos at intake.
- Barcode or QR code per order or garment.
- SMS or WhatsApp notifications.
- Home delivery and route management.
- Detailed production stations such as cleaning, pressing, quality check, and packing.
- Missing or damaged garment workflow.
- Customer compensation tracking.
- Customer portal or mobile application.
- Multi-branch Dry Clean operations.

## Prioritization Rule

Future features should be selected using three questions:

1. Does this solve a repeated problem for a real store?
2. Does it improve revenue, speed, accuracy, or data safety?
3. Can it be added without weakening the POS v2 financial and authorization rules?

