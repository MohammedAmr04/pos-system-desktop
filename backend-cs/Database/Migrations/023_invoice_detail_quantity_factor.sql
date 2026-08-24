-- Plan Phase 4: base-unit factor on sold lines (used for FIFO consumption)
ALTER TABLE "InvoiceDetail" ADD COLUMN "quantityFactor" REAL NOT NULL DEFAULT 1;
