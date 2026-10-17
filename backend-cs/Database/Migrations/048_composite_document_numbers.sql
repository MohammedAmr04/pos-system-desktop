-- Migration 048: Per-branch document numbering.
-- Shift / SaleReturn / PurchaseReturn / InventoryAdjustment used a single-column
-- UNIQUE(number) while each branch mints its own MAX(number)+1 sequence. Two branches
-- therefore produced the same number, and the central SyncRepository inserts (INSERT OR
-- IGNORE) silently dropped the loser. Uniqueness is now scoped to (number, branchId) so
-- each branch keeps its own 1..N sequence and nothing is lost on convergence.
-- SQLite cannot drop a column constraint, so each table is rebuilt (create new -> copy ->
-- drop old -> rename) and its indexes/triggers are recreated. Foreign keys are not enabled
-- in this app, so the rebuild needs no PRAGMA toggling.

-- ---------------------------------------------------------------------------
-- Shift
-- ---------------------------------------------------------------------------
CREATE TABLE "Shift_new" (
    id TEXT PRIMARY KEY,
    number INTEGER NOT NULL,
    openedBy TEXT NOT NULL,
    openingCash REAL NOT NULL DEFAULT 0,
    openedAt TEXT NOT NULL,
    closedAt TEXT,
    countedCash REAL,
    expectedCash REAL,
    difference REAL,
    notes TEXT,
    status TEXT NOT NULL DEFAULT 'open',
    branchId TEXT,
    terminalId TEXT,
    UNIQUE ("number", "branchId")
);

INSERT INTO "Shift_new" (id,number,openedBy,openingCash,openedAt,closedAt,countedCash,expectedCash,difference,notes,status,branchId,terminalId)
    SELECT id,number,openedBy,openingCash,openedAt,closedAt,countedCash,expectedCash,difference,notes,status,branchId,terminalId FROM "Shift";

DROP TABLE "Shift";
ALTER TABLE "Shift_new" RENAME TO "Shift";

CREATE INDEX IF NOT EXISTS "idx_shift_branch_opened" ON "Shift" ("branchId", "openedAt");

CREATE TRIGGER "trg_shift_branch_context"
AFTER INSERT ON "Shift"
WHEN NEW."branchId" IS NULL
BEGIN
    UPDATE "Shift" SET "branchId"=(SELECT "branchId" FROM "BranchRuntimeConfig" WHERE "id"=1),
        "terminalId"=(SELECT "terminalId" FROM "BranchRuntimeConfig" WHERE "id"=1)
    WHERE "id"=NEW."id";
END;

CREATE TRIGGER "trg_shift_sync_insert"
AFTER INSERT ON "Shift"
WHEN (SELECT "nodeRole" FROM "BranchRuntimeConfig" WHERE "id"=1) = 'branch'
BEGIN
    INSERT INTO "SyncOutbox" (id,branchId,terminalId,operationType,entityType,entityId,payload,occurredAt,status)
    VALUES (lower(hex(randomblob(16))),
        (SELECT branchId FROM BranchRuntimeConfig WHERE id=1),
        (SELECT terminalId FROM BranchRuntimeConfig WHERE id=1),
        'upsert','Shift',NEW.id,
        json_object('id',NEW.id,'number',NEW.number,'openedBy',NEW.openedBy,'openingCash',NEW.openingCash,
            'openedAt',NEW.openedAt,'closedAt',NEW.closedAt,'countedCash',NEW.countedCash,
            'expectedCash',NEW.expectedCash,'difference',NEW.difference,'notes',NEW.notes,'status',NEW.status),
        COALESCE(NEW.openedAt,CURRENT_TIMESTAMP),'pending');
END;

-- ---------------------------------------------------------------------------
-- SaleReturn
-- ---------------------------------------------------------------------------
CREATE TABLE "SaleReturn_new" (
    id TEXT PRIMARY KEY,
    number INTEGER NOT NULL,
    invoiceId TEXT NOT NULL,
    date TEXT NOT NULL,
    totalAmount REAL NOT NULL,
    restoredCost REAL NOT NULL DEFAULT 0,
    paymentMethod TEXT NOT NULL DEFAULT 'cash',
    notes TEXT,
    status TEXT NOT NULL DEFAULT 'posted',
    createdBy TEXT,
    createdAt TEXT NOT NULL,
    branchId TEXT,
    terminalId TEXT,
    UNIQUE ("number", "branchId")
);

INSERT INTO "SaleReturn_new" (id,number,invoiceId,date,totalAmount,restoredCost,paymentMethod,notes,status,createdBy,createdAt,branchId,terminalId)
    SELECT id,number,invoiceId,date,totalAmount,restoredCost,paymentMethod,notes,status,createdBy,createdAt,branchId,terminalId FROM "SaleReturn";

DROP TABLE "SaleReturn";
ALTER TABLE "SaleReturn_new" RENAME TO "SaleReturn";

CREATE INDEX IF NOT EXISTS "idx_sale_returns_invoice" ON "SaleReturn" ("invoiceId");

CREATE TRIGGER "trg_sale_return_branch_context"
AFTER INSERT ON "SaleReturn"
WHEN NEW."branchId" IS NULL
BEGIN
    UPDATE "SaleReturn" SET "branchId"=(SELECT "branchId" FROM "BranchRuntimeConfig" WHERE "id"=1),
        "terminalId"=(SELECT "terminalId" FROM "BranchRuntimeConfig" WHERE "id"=1)
    WHERE "id"=NEW."id";
END;

-- ---------------------------------------------------------------------------
-- PurchaseReturn
-- ---------------------------------------------------------------------------
CREATE TABLE "PurchaseReturn_new" (
    id TEXT PRIMARY KEY,
    number INTEGER NOT NULL,
    purchaseInvoiceId TEXT NOT NULL,
    date TEXT NOT NULL,
    totalAmount REAL NOT NULL,
    paymentMethod TEXT NOT NULL DEFAULT 'cash',
    notes TEXT,
    status TEXT NOT NULL DEFAULT 'posted',
    createdBy TEXT,
    createdAt TEXT NOT NULL,
    branchId TEXT,
    terminalId TEXT,
    UNIQUE ("number", "branchId")
);

INSERT INTO "PurchaseReturn_new" (id,number,purchaseInvoiceId,date,totalAmount,paymentMethod,notes,status,createdBy,createdAt,branchId,terminalId)
    SELECT id,number,purchaseInvoiceId,date,totalAmount,paymentMethod,notes,status,createdBy,createdAt,branchId,terminalId FROM "PurchaseReturn";

DROP TABLE "PurchaseReturn";
ALTER TABLE "PurchaseReturn_new" RENAME TO "PurchaseReturn";

CREATE INDEX IF NOT EXISTS "idx_purchase_returns_invoice" ON "PurchaseReturn" ("purchaseInvoiceId");

CREATE TRIGGER "trg_purchase_return_branch_context"
AFTER INSERT ON "PurchaseReturn"
WHEN NEW."branchId" IS NULL
BEGIN
    UPDATE "PurchaseReturn" SET "branchId"=(SELECT "branchId" FROM "BranchRuntimeConfig" WHERE "id"=1),
        "terminalId"=(SELECT "terminalId" FROM "BranchRuntimeConfig" WHERE "id"=1)
    WHERE "id"=NEW."id";
END;

-- ---------------------------------------------------------------------------
-- InventoryAdjustment
-- ---------------------------------------------------------------------------
CREATE TABLE "InventoryAdjustment_new" (
    id TEXT PRIMARY KEY,
    number INTEGER NOT NULL,
    reason TEXT NOT NULL,
    createdBy TEXT NOT NULL,
    createdAt TEXT NOT NULL,
    status TEXT NOT NULL DEFAULT 'posted',
    postedAt TEXT,
    postedBy TEXT,
    cancelledAt TEXT,
    cancelledBy TEXT,
    notes TEXT,
    branchId TEXT,
    terminalId TEXT,
    UNIQUE ("number", "branchId")
);

INSERT INTO "InventoryAdjustment_new" (id,number,reason,createdBy,createdAt,status,postedAt,postedBy,cancelledAt,cancelledBy,notes,branchId,terminalId)
    SELECT id,number,reason,createdBy,createdAt,status,postedAt,postedBy,cancelledAt,cancelledBy,notes,branchId,terminalId FROM "InventoryAdjustment";

DROP TABLE "InventoryAdjustment";
ALTER TABLE "InventoryAdjustment_new" RENAME TO "InventoryAdjustment";

CREATE INDEX IF NOT EXISTS "idx_inventory_adjustment_status" ON "InventoryAdjustment" ("status", "createdAt");
CREATE INDEX IF NOT EXISTS "idx_inventory_adjustment_branch_created" ON "InventoryAdjustment" ("branchId", "createdAt");

CREATE TRIGGER "trg_inventory_adjustment_branch_context"
AFTER INSERT ON "InventoryAdjustment"
WHEN NEW."branchId" IS NULL
BEGIN
    UPDATE "InventoryAdjustment" SET "branchId"=(SELECT "branchId" FROM "BranchRuntimeConfig" WHERE "id"=1),
        "terminalId"=(SELECT "terminalId" FROM "BranchRuntimeConfig" WHERE "id"=1)
    WHERE "id"=NEW."id";
END;
