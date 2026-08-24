-- Sales lifecycle (Phase 6) — mirrors purchases: draft (no side effects) -> posted
-- (full pipeline: ledger + FIFO + COGS + stock + auto cash payment) -> cancelled
-- (safe reversal, never deletes). Existing rows are legacy completed sales => posted.
-- paymentMethod: 'cash' | 'credit'; credit requires clientId (spec §21).
ALTER TABLE "Invoice" ADD COLUMN "status" TEXT NOT NULL DEFAULT 'posted';
ALTER TABLE "Invoice" ADD COLUMN "clientId" TEXT;
ALTER TABLE "Invoice" ADD COLUMN "paymentMethod" TEXT NOT NULL DEFAULT 'cash';
ALTER TABLE "Invoice" ADD COLUMN "createdBy" TEXT;

CREATE INDEX IF NOT EXISTS "idx_invoice_status" ON "Invoice" (status, createdAt);
CREATE INDEX IF NOT EXISTS "idx_invoice_client" ON "Invoice" (clientId);
