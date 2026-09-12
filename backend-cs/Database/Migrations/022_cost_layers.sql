-- FIFO Cost Layers (Phase 4) — historical cost tracking (spec: old invoices never change when
-- the current product cost changes).
-- CostLayer rows are created when a purchase invoice is posted; one layer per purchase line.
-- quantityRemaining decreases as sales consume layers in FIFO order.
-- Reversals (purchase edit/cancel) reduce BOTH quantityReceived and quantityRemaining by the
-- reversed amount — remaining can go negative only if goods were already sold; the allocator
-- skips non-positive layers and historical SaleCostAllocation rows are never rewritten.
CREATE TABLE IF NOT EXISTS "CostLayer" (
    id TEXT PRIMARY KEY,
    productId TEXT NOT NULL,
    sourcePurchaseId TEXT NOT NULL,
    quantityReceived REAL NOT NULL,
    quantityRemaining REAL NOT NULL,
    unitCost REAL NOT NULL,
    createdAt TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS "idx_cost_layers_product" ON "CostLayer" (productId, createdAt);

-- Audit detail of which layers each sales line consumed (also the basis for sales returns
-- returning stock to layers in Phase 7).
CREATE TABLE IF NOT EXISTS "SaleCostAllocation" (
    id TEXT PRIMARY KEY,
    invoiceId TEXT NOT NULL,
    invoiceDetailId TEXT NOT NULL,
    productId TEXT NOT NULL,
    costLayerId TEXT NOT NULL,
    quantity REAL NOT NULL,
    unitCost REAL NOT NULL,
    createdAt TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS "idx_allocations_invoice" ON "SaleCostAllocation" (invoiceId);
CREATE INDEX IF NOT EXISTS "idx_allocations_detail" ON "SaleCostAllocation" (invoiceDetailId);

-- Historical COGS stored on the sales line at sale time.
ALTER TABLE "InvoiceDetail" ADD COLUMN "totalCost" REAL;
