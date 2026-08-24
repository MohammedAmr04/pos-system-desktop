-- Stock Movement ledger (Phase 3) — every inventory mutation (sale/purchase/returns/adjustments)
-- passes through this table. quantity is signed, always in BASE units:
--   positive = stock in (purchase), negative = stock out (sale).
-- type: 'purchase' | 'purchase_reversal' | 'sale' | 'sale_reversal'
-- referenceId points at the owning invoice; reversal rows reference the original movement's invoice.
CREATE TABLE IF NOT EXISTS "StockMovement" (
    id TEXT PRIMARY KEY,
    productId TEXT NOT NULL,
    quantity REAL NOT NULL,
    type TEXT NOT NULL,
    referenceId TEXT,
    referenceNumber TEXT,
    createdAt TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS "idx_stock_movements_product" ON "StockMovement" (productId, createdAt);
CREATE INDEX IF NOT EXISTS "idx_stock_movements_reference" ON "StockMovement" (referenceId);
