-- Migration 047: Branch scope for InventoryAdjustment so posted counts sync to central.
-- Purchases already carry branchId/terminalId from 042; inventory adjustments only had
-- branch context on their stock movements.

ALTER TABLE "InventoryAdjustment" ADD COLUMN "branchId" TEXT;
ALTER TABLE "InventoryAdjustment" ADD COLUMN "terminalId" TEXT;

UPDATE "InventoryAdjustment" SET "branchId" = 'default' WHERE "branchId" IS NULL;
UPDATE "InventoryAdjustment" SET "terminalId" = 'default-terminal' WHERE "terminalId" IS NULL;

CREATE INDEX IF NOT EXISTS "idx_inventory_adjustment_branch_created"
    ON "InventoryAdjustment" ("branchId", "createdAt");

CREATE TRIGGER IF NOT EXISTS "trg_inventory_adjustment_branch_context"
AFTER INSERT ON "InventoryAdjustment"
WHEN NEW."branchId" IS NULL
BEGIN
    UPDATE "InventoryAdjustment" SET "branchId"=(SELECT "branchId" FROM "BranchRuntimeConfig" WHERE "id"=1),
        "terminalId"=(SELECT "terminalId" FROM "BranchRuntimeConfig" WHERE "id"=1)
    WHERE "id"=NEW."id";
END;
