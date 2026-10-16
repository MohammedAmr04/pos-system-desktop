-- Migration 049: Index supporting per-branch stock aggregation on the central node.
-- Central stock is derived from the immutable StockMovement ledger (SUM(quantity) grouped by
-- product for a branch) rather than a mutable per-branch counter, so a wrong or replayed
-- movement cannot drift a stored balance. This index keeps that aggregation efficient as the
-- ledger grows and the number of branches increases.
CREATE INDEX IF NOT EXISTS "idx_stock_movement_branch_product" ON "StockMovement" ("branchId", "productId");
