ALTER TABLE "InventoryAdjustment" ADD COLUMN "status" TEXT NOT NULL DEFAULT 'posted';
ALTER TABLE "InventoryAdjustment" ADD COLUMN "postedAt" TEXT;
ALTER TABLE "InventoryAdjustment" ADD COLUMN "postedBy" TEXT;
ALTER TABLE "InventoryAdjustment" ADD COLUMN "cancelledAt" TEXT;
ALTER TABLE "InventoryAdjustment" ADD COLUMN "cancelledBy" TEXT;

ALTER TABLE "InventoryAdjustmentLine" ADD COLUMN "productUnitId" TEXT;
ALTER TABLE "InventoryAdjustmentLine" ADD COLUMN "unitName" TEXT;
ALTER TABLE "InventoryAdjustmentLine" ADD COLUMN "quantityFactor" REAL NOT NULL DEFAULT 1;
ALTER TABLE "InventoryAdjustmentLine" ADD COLUMN "countedQuantity" REAL;
ALTER TABLE "InventoryAdjustmentLine" ADD COLUMN "countedBaseQuantity" REAL;
ALTER TABLE "InventoryAdjustmentLine" ADD COLUMN "isMatched" INTEGER NOT NULL DEFAULT 0;
ALTER TABLE "InventoryAdjustmentLine" ADD COLUMN "updatedAt" TEXT;

CREATE INDEX IF NOT EXISTS "idx_inventory_adjustment_status" ON "InventoryAdjustment" (status, createdAt);
