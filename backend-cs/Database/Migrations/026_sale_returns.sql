-- Sales Returns (Phase 7) — a return is its own transaction referencing the original
-- posted sale (spec §22): the original invoice is never converted or destroyed.
-- Partial returns are tracked per line; completing all quantities flips the invoice's
-- returnStatus from 'partial' to 'full' (NULL = never returned).
CREATE TABLE IF NOT EXISTS "SaleReturn" (
    id TEXT PRIMARY KEY,
    number INTEGER NOT NULL UNIQUE,
    invoiceId TEXT NOT NULL,
    date TEXT NOT NULL,
    totalAmount REAL NOT NULL,
    restoredCost REAL NOT NULL DEFAULT 0,
    paymentMethod TEXT NOT NULL DEFAULT 'cash',
    notes TEXT,
    status TEXT NOT NULL DEFAULT 'posted',
    createdBy TEXT,
    createdAt TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS "idx_sale_returns_invoice" ON "SaleReturn" (invoiceId);

CREATE TABLE IF NOT EXISTS "SaleReturnDetail" (
    id TEXT PRIMARY KEY,
    returnId TEXT NOT NULL,
    invoiceDetailId TEXT NOT NULL,
    productId TEXT NOT NULL,
    unitName TEXT,
    quantity REAL NOT NULL,
    quantityFactor REAL NOT NULL DEFAULT 1,
    unitPrice REAL NOT NULL,
    lineTotal REAL NOT NULL,
    restoredCost REAL NOT NULL DEFAULT 0
);

CREATE INDEX IF NOT EXISTS "idx_sale_return_details_return" ON "SaleReturnDetail" (returnId);

-- How much of each FIFO allocation has already been restored by returns; prevents
-- double-restoring layers across successive partial returns.
ALTER TABLE "SaleCostAllocation" ADD COLUMN returnedQuantity REAL NOT NULL DEFAULT 0;

-- NULL = no returns yet | 'partial' | 'full'. The Invoice.status lifecycle stays untouched.
ALTER TABLE "Invoice" ADD COLUMN returnStatus TEXT;

INSERT OR IGNORE INTO "Permission" (id, key, name, description, resource, action, createdAt, updatedAt) VALUES
('invoices.return', 'invoices.return', 'Process Sales Returns', 'Create sales returns against posted invoices', 'invoices', 'return', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

INSERT OR IGNORE INTO "RolePermission" (roleId, permissionId)
SELECT 'role-admin', p.id FROM "Permission" p
WHERE p.id = 'invoices.return';

INSERT OR IGNORE INTO "RolePermission" (roleId, permissionId)
SELECT 'role-manager', p.id FROM "Permission" p
WHERE p.id = 'invoices.return';
