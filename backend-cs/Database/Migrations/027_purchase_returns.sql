-- Purchase Returns (Phase 8) — a return is its own transaction referencing the original
-- posted purchase (spec §25): the original invoice is never converted or destroyed.
-- Partial returns are tracked per line; completing all quantities flips the purchase's
-- returnStatus from 'partial' to 'full' (NULL = never returned). Refunds reuse the
-- original payment method: cash purchases get an automatic negative payment, credit
-- purchases reduce the supplier balance through the account statement.
CREATE TABLE IF NOT EXISTS "PurchaseReturn" (
    id TEXT PRIMARY KEY,
    number INTEGER NOT NULL UNIQUE,
    purchaseInvoiceId TEXT NOT NULL,
    date TEXT NOT NULL,
    totalAmount REAL NOT NULL,
    paymentMethod TEXT NOT NULL DEFAULT 'cash',
    notes TEXT,
    status TEXT NOT NULL DEFAULT 'posted',
    createdBy TEXT,
    createdAt TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS "idx_purchase_returns_invoice" ON "PurchaseReturn" (purchaseInvoiceId);

CREATE TABLE IF NOT EXISTS "PurchaseReturnDetail" (
    id TEXT PRIMARY KEY,
    returnId TEXT NOT NULL,
    purchaseItemId TEXT NOT NULL,
    productId TEXT NOT NULL,
    unitName TEXT,
    quantity REAL NOT NULL,
    quantityFactor REAL NOT NULL DEFAULT 1,
    unitCost REAL NOT NULL,
    lineTotal REAL NOT NULL
);

CREATE INDEX IF NOT EXISTS "idx_purchase_return_details_return" ON "PurchaseReturnDetail" (returnId);

-- NULL = no returns yet | 'partial' | 'full'. The PurchaseInvoice.status lifecycle stays untouched.
ALTER TABLE "PurchaseInvoice" ADD COLUMN returnStatus TEXT;

INSERT OR IGNORE INTO "Permission" (id, key, name, description, resource, action, createdAt, updatedAt) VALUES
('purchases.return', 'purchases.return', 'Process Purchase Returns', 'Create purchase returns against posted purchases', 'purchases', 'return', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

INSERT OR IGNORE INTO "RolePermission" (roleId, permissionId)
SELECT 'role-admin', p.id FROM "Permission" p
WHERE p.id = 'purchases.return';

INSERT OR IGNORE INTO "RolePermission" (roleId, permissionId)
SELECT 'role-manager', p.id FROM "Permission" p
WHERE p.id = 'purchases.return';
