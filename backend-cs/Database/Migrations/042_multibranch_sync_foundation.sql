CREATE TABLE IF NOT EXISTS "Branch" (
    "id" TEXT NOT NULL PRIMARY KEY,
    "code" TEXT NOT NULL UNIQUE,
    "name" TEXT NOT NULL,
    "address" TEXT,
    "isActive" INTEGER NOT NULL DEFAULT 1,
    "createdAt" DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "updatedAt" DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS "Terminal" (
    "id" TEXT NOT NULL PRIMARY KEY,
    "branchId" TEXT NOT NULL,
    "name" TEXT NOT NULL,
    "isActive" INTEGER NOT NULL DEFAULT 1,
    "createdAt" DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "updatedAt" DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY ("branchId") REFERENCES "Branch"("id")
);

CREATE TABLE IF NOT EXISTS "BranchRuntimeConfig" (
    "id" INTEGER NOT NULL PRIMARY KEY CHECK ("id" = 1),
    "branchId" TEXT NOT NULL,
    "terminalId" TEXT NOT NULL,
    "nodeRole" TEXT NOT NULL DEFAULT 'branch',
    "centralBaseUrl" TEXT,
    "branchSecretHash" TEXT,
    "updatedAt" DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY ("branchId") REFERENCES "Branch"("id"),
    FOREIGN KEY ("terminalId") REFERENCES "Terminal"("id")
);

CREATE TABLE IF NOT EXISTS "SyncOutbox" (
    "id" TEXT NOT NULL PRIMARY KEY,
    "branchId" TEXT NOT NULL,
    "terminalId" TEXT NOT NULL,
    "operationType" TEXT NOT NULL,
    "entityType" TEXT NOT NULL,
    "entityId" TEXT NOT NULL,
    "payload" TEXT,
    "occurredAt" DATETIME NOT NULL,
    "status" TEXT NOT NULL DEFAULT 'pending',
    "attempts" INTEGER NOT NULL DEFAULT 0,
    "lastError" TEXT,
    "syncedAt" DATETIME,
    FOREIGN KEY ("branchId") REFERENCES "Branch"("id"),
    FOREIGN KEY ("terminalId") REFERENCES "Terminal"("id")
);

CREATE INDEX IF NOT EXISTS "idx_sync_outbox_pending"
    ON "SyncOutbox" ("status", "occurredAt");
CREATE INDEX IF NOT EXISTS "idx_sync_outbox_entity_operation"
    ON "SyncOutbox" ("entityType", "entityId", "operationType");

CREATE TABLE IF NOT EXISTS "SyncChangeLog" (
    "version" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "branchId" TEXT,
    "entityType" TEXT NOT NULL,
    "entityId" TEXT NOT NULL,
    "operationType" TEXT NOT NULL,
    "payload" TEXT NOT NULL,
    "createdAt" DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS "SyncInbox" (
    "operationId" TEXT NOT NULL PRIMARY KEY,
    "branchId" TEXT NOT NULL,
    "terminalId" TEXT NOT NULL,
    "operationType" TEXT NOT NULL,
    "entityType" TEXT NOT NULL,
    "entityId" TEXT NOT NULL,
    "payload" TEXT,
    "receivedAt" DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "appliedAt" DATETIME,
    "status" TEXT NOT NULL DEFAULT 'received',
    "lastError" TEXT
);

INSERT OR IGNORE INTO "Branch" ("id", "code", "name")
VALUES ('default', 'DEFAULT', 'Default Branch');

INSERT OR IGNORE INTO "Terminal" ("id", "branchId", "name")
VALUES ('default-terminal', 'default', 'Default POS');

INSERT OR IGNORE INTO "BranchRuntimeConfig" ("id", "branchId", "terminalId", "nodeRole")
VALUES (1, 'default', 'default-terminal', 'branch');

INSERT OR IGNORE INTO "Permission" (id, key, name, description, resource, action, createdAt, updatedAt) VALUES
('branches.view', 'branches.view', 'View Branches', 'View configured branches and terminals', 'branches', 'view', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('branches.manage', 'branches.manage', 'Manage Branch Runtime', 'Configure branch runtime and terminal binding', 'branches', 'manage', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

INSERT OR IGNORE INTO "RolePermission" (roleId, permissionId)
SELECT 'role-admin', p.id FROM "Permission" p
WHERE p.id IN ('branches.view','branches.manage');

ALTER TABLE "Invoice" ADD COLUMN "branchId" TEXT;
ALTER TABLE "Invoice" ADD COLUMN "terminalId" TEXT;
ALTER TABLE "Shift" ADD COLUMN "branchId" TEXT;
ALTER TABLE "Shift" ADD COLUMN "terminalId" TEXT;
ALTER TABLE "Payment" ADD COLUMN "branchId" TEXT;
ALTER TABLE "Payment" ADD COLUMN "terminalId" TEXT;
ALTER TABLE "Expense" ADD COLUMN "branchId" TEXT;
ALTER TABLE "Expense" ADD COLUMN "terminalId" TEXT;
ALTER TABLE "StockMovement" ADD COLUMN "branchId" TEXT;
ALTER TABLE "StockMovement" ADD COLUMN "terminalId" TEXT;
ALTER TABLE "SaleReturn" ADD COLUMN "branchId" TEXT;
ALTER TABLE "SaleReturn" ADD COLUMN "terminalId" TEXT;
ALTER TABLE "PurchaseInvoice" ADD COLUMN "branchId" TEXT;
ALTER TABLE "PurchaseInvoice" ADD COLUMN "terminalId" TEXT;
ALTER TABLE "PurchaseReturn" ADD COLUMN "branchId" TEXT;
ALTER TABLE "PurchaseReturn" ADD COLUMN "terminalId" TEXT;

UPDATE "Invoice" SET "branchId" = 'default' WHERE "branchId" IS NULL;
UPDATE "Invoice" SET "terminalId" = 'default-terminal' WHERE "terminalId" IS NULL;
UPDATE "Shift" SET "branchId" = 'default' WHERE "branchId" IS NULL;
UPDATE "Shift" SET "terminalId" = 'default-terminal' WHERE "terminalId" IS NULL;
UPDATE "Payment" SET "branchId" = 'default' WHERE "branchId" IS NULL;
UPDATE "Payment" SET "terminalId" = 'default-terminal' WHERE "terminalId" IS NULL;
UPDATE "Expense" SET "branchId" = 'default' WHERE "branchId" IS NULL;
UPDATE "Expense" SET "terminalId" = 'default-terminal' WHERE "terminalId" IS NULL;
UPDATE "StockMovement" SET "branchId" = 'default' WHERE "branchId" IS NULL;
UPDATE "StockMovement" SET "terminalId" = 'default-terminal' WHERE "terminalId" IS NULL;
UPDATE "SaleReturn" SET "branchId" = 'default' WHERE "branchId" IS NULL;
UPDATE "SaleReturn" SET "terminalId" = 'default-terminal' WHERE "terminalId" IS NULL;
UPDATE "PurchaseInvoice" SET "branchId" = 'default' WHERE "branchId" IS NULL;
UPDATE "PurchaseInvoice" SET "terminalId" = 'default-terminal' WHERE "terminalId" IS NULL;
UPDATE "PurchaseReturn" SET "branchId" = 'default' WHERE "branchId" IS NULL;
UPDATE "PurchaseReturn" SET "terminalId" = 'default-terminal' WHERE "terminalId" IS NULL;

CREATE INDEX IF NOT EXISTS "idx_invoice_branch_created" ON "Invoice" ("branchId", "createdAt");
CREATE INDEX IF NOT EXISTS "idx_shift_branch_opened" ON "Shift" ("branchId", "openedAt");
CREATE INDEX IF NOT EXISTS "idx_payment_branch_created" ON "Payment" ("branchId", "createdAt");

CREATE TRIGGER IF NOT EXISTS "trg_invoice_branch_context"
AFTER INSERT ON "Invoice"
WHEN NEW."branchId" IS NULL
BEGIN
    UPDATE "Invoice" SET "branchId"=(SELECT "branchId" FROM "BranchRuntimeConfig" WHERE "id"=1),
        "terminalId"=(SELECT "terminalId" FROM "BranchRuntimeConfig" WHERE "id"=1)
    WHERE "id"=NEW."id";
END;

CREATE TRIGGER IF NOT EXISTS "trg_shift_branch_context"
AFTER INSERT ON "Shift"
WHEN NEW."branchId" IS NULL
BEGIN
    UPDATE "Shift" SET "branchId"=(SELECT "branchId" FROM "BranchRuntimeConfig" WHERE "id"=1),
        "terminalId"=(SELECT "terminalId" FROM "BranchRuntimeConfig" WHERE "id"=1)
    WHERE "id"=NEW."id";
END;

CREATE TRIGGER IF NOT EXISTS "trg_payment_branch_context"
AFTER INSERT ON "Payment"
WHEN NEW."branchId" IS NULL
BEGIN
    UPDATE "Payment" SET "branchId"=(SELECT "branchId" FROM "BranchRuntimeConfig" WHERE "id"=1),
        "terminalId"=(SELECT "terminalId" FROM "BranchRuntimeConfig" WHERE "id"=1)
    WHERE "id"=NEW."id";
END;

CREATE TRIGGER IF NOT EXISTS "trg_expense_branch_context"
AFTER INSERT ON "Expense"
WHEN NEW."branchId" IS NULL
BEGIN
    UPDATE "Expense" SET "branchId"=(SELECT "branchId" FROM "BranchRuntimeConfig" WHERE "id"=1),
        "terminalId"=(SELECT "terminalId" FROM "BranchRuntimeConfig" WHERE "id"=1)
    WHERE "id"=NEW."id";
END;

CREATE TRIGGER IF NOT EXISTS "trg_stock_movement_branch_context"
AFTER INSERT ON "StockMovement"
WHEN NEW."branchId" IS NULL
BEGIN
    UPDATE "StockMovement" SET "branchId"=(SELECT "branchId" FROM "BranchRuntimeConfig" WHERE "id"=1),
        "terminalId"=(SELECT "terminalId" FROM "BranchRuntimeConfig" WHERE "id"=1)
    WHERE "id"=NEW."id";
END;

CREATE TRIGGER IF NOT EXISTS "trg_sale_return_branch_context"
AFTER INSERT ON "SaleReturn"
WHEN NEW."branchId" IS NULL
BEGIN
    UPDATE "SaleReturn" SET "branchId"=(SELECT "branchId" FROM "BranchRuntimeConfig" WHERE "id"=1),
        "terminalId"=(SELECT "terminalId" FROM "BranchRuntimeConfig" WHERE "id"=1)
    WHERE "id"=NEW."id";
END;

CREATE TRIGGER IF NOT EXISTS "trg_purchase_invoice_branch_context"
AFTER INSERT ON "PurchaseInvoice"
WHEN NEW."branchId" IS NULL
BEGIN
    UPDATE "PurchaseInvoice" SET "branchId"=(SELECT "branchId" FROM "BranchRuntimeConfig" WHERE "id"=1),
        "terminalId"=(SELECT "terminalId" FROM "BranchRuntimeConfig" WHERE "id"=1)
    WHERE "id"=NEW."id";
END;

CREATE TRIGGER IF NOT EXISTS "trg_purchase_return_branch_context"
AFTER INSERT ON "PurchaseReturn"
WHEN NEW."branchId" IS NULL
BEGIN
    UPDATE "PurchaseReturn" SET "branchId"=(SELECT "branchId" FROM "BranchRuntimeConfig" WHERE "id"=1),
        "terminalId"=(SELECT "terminalId" FROM "BranchRuntimeConfig" WHERE "id"=1)
    WHERE "id"=NEW."id";
END;

CREATE TRIGGER IF NOT EXISTS "trg_product_sync_insert"
AFTER INSERT ON "Product"
BEGIN
    INSERT INTO "SyncChangeLog" (branchId,entityType,entityId,operationType,payload)
    VALUES (NULL,'Product',NEW.id,'upsert','{}');
END;

CREATE TRIGGER IF NOT EXISTS "trg_product_sync_update"
AFTER UPDATE ON "Product"
BEGIN
    INSERT INTO "SyncChangeLog" (branchId,entityType,entityId,operationType,payload)
    VALUES (NULL,'Product',NEW.id,'upsert','{}');
END;

CREATE TRIGGER IF NOT EXISTS "trg_category_sync_insert"
AFTER INSERT ON "Category"
BEGIN
    INSERT INTO "SyncChangeLog" (branchId,entityType,entityId,operationType,payload)
    VALUES (NULL,'Category',NEW.id,'upsert','{}');
END;

CREATE TRIGGER IF NOT EXISTS "trg_category_sync_update"
AFTER UPDATE ON "Category"
BEGIN
    INSERT INTO "SyncChangeLog" (branchId,entityType,entityId,operationType,payload)
    VALUES (NULL,'Category',NEW.id,'upsert','{}');
END;

CREATE TRIGGER IF NOT EXISTS "trg_brand_sync_insert"
AFTER INSERT ON "Brand"
BEGIN
    INSERT INTO "SyncChangeLog" (branchId,entityType,entityId,operationType,payload)
    VALUES (NULL,'Brand',NEW.id,'upsert','{}');
END;

CREATE TRIGGER IF NOT EXISTS "trg_brand_sync_update"
AFTER UPDATE ON "Brand"
BEGIN
    INSERT INTO "SyncChangeLog" (branchId,entityType,entityId,operationType,payload)
    VALUES (NULL,'Brand',NEW.id,'upsert','{}');
END;

CREATE TRIGGER IF NOT EXISTS "trg_payment_sync_insert"
AFTER INSERT ON "Payment"
BEGIN
    INSERT INTO "SyncOutbox" (id,branchId,terminalId,operationType,entityType,entityId,occurredAt,status)
    VALUES (lower(hex(randomblob(16))),
        (SELECT branchId FROM BranchRuntimeConfig WHERE id=1),
        (SELECT terminalId FROM BranchRuntimeConfig WHERE id=1),
        'upsert','Payment',NEW.id,COALESCE(NEW.createdAt,CURRENT_TIMESTAMP),'pending');
END;

CREATE TRIGGER IF NOT EXISTS "trg_shift_sync_insert"
AFTER INSERT ON "Shift"
BEGIN
    INSERT INTO "SyncOutbox" (id,branchId,terminalId,operationType,entityType,entityId,occurredAt,status)
    VALUES (lower(hex(randomblob(16))),
        (SELECT branchId FROM BranchRuntimeConfig WHERE id=1),
        (SELECT terminalId FROM BranchRuntimeConfig WHERE id=1),
        'upsert','Shift',NEW.id,COALESCE(NEW.openedAt,CURRENT_TIMESTAMP),'pending');
END;

CREATE TRIGGER IF NOT EXISTS "trg_expense_sync_insert"
AFTER INSERT ON "Expense"
BEGIN
    INSERT INTO "SyncOutbox" (id,branchId,terminalId,operationType,entityType,entityId,occurredAt,status)
    VALUES (lower(hex(randomblob(16))),
        (SELECT branchId FROM BranchRuntimeConfig WHERE id=1),
        (SELECT terminalId FROM BranchRuntimeConfig WHERE id=1),
        'upsert','Expense',NEW.id,COALESCE(NEW.createdAt,CURRENT_TIMESTAMP),'pending');
END;

CREATE TRIGGER IF NOT EXISTS "trg_stock_movement_sync_insert"
AFTER INSERT ON "StockMovement"
BEGIN
    INSERT INTO "SyncOutbox" (id,branchId,terminalId,operationType,entityType,entityId,occurredAt,status)
    VALUES (lower(hex(randomblob(16))),
        (SELECT branchId FROM BranchRuntimeConfig WHERE id=1),
        (SELECT terminalId FROM BranchRuntimeConfig WHERE id=1),
        'upsert','StockMovement',NEW.id,COALESCE(NEW.createdAt,CURRENT_TIMESTAMP),'pending');
END;

CREATE TRIGGER IF NOT EXISTS "trg_sale_return_sync_insert"
AFTER INSERT ON "SaleReturn"
BEGIN
    INSERT INTO "SyncOutbox" (id,branchId,terminalId,operationType,entityType,entityId,occurredAt,status)
    VALUES (lower(hex(randomblob(16))),
        (SELECT branchId FROM BranchRuntimeConfig WHERE id=1),
        (SELECT terminalId FROM BranchRuntimeConfig WHERE id=1),
        'upsert','SaleReturn',NEW.id,COALESCE(NEW.createdAt,CURRENT_TIMESTAMP),'pending');
END;
