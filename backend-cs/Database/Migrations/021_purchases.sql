-- Purchase Invoices (Phase 3) — stock entering the business (spec §11/§12/§13).
-- Lifecycle: draft (no side effects) → posted (stock in, price updates; FIFO layers arrive in
-- Phase 4) → cancelled (safe reversal rows, never deletes). Editing a posted purchase is a
-- transactional reversal + re-application, never a direct line update.
-- paymentMethod: 'cash' | 'credit' — supplier optional for cash, REQUIRED for credit (spec §11.1).
CREATE TABLE IF NOT EXISTS "PurchaseInvoice" (
    id TEXT PRIMARY KEY,
    invoiceNumber INTEGER NOT NULL,
    supplierInvoiceNumber TEXT,
    supplierId TEXT,
    date TEXT NOT NULL,
    paymentMethod TEXT NOT NULL DEFAULT 'cash',
    status TEXT NOT NULL DEFAULT 'draft',
    subtotal REAL NOT NULL DEFAULT 0,
    discount REAL NOT NULL DEFAULT 0,
    tax REAL NOT NULL DEFAULT 0,
    total REAL NOT NULL DEFAULT 0,
    notes TEXT,
    createdBy TEXT,
    createdAt TEXT NOT NULL,
    updatedAt TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS "PurchaseInvoiceItem" (
    id TEXT PRIMARY KEY,
    purchaseInvoiceId TEXT NOT NULL REFERENCES "PurchaseInvoice"(id),
    productId TEXT NOT NULL,
    productUnitId TEXT NOT NULL,
    unitName TEXT NOT NULL,
    quantityFactor REAL NOT NULL DEFAULT 1,
    quantity REAL NOT NULL,
    unitCost REAL NOT NULL,
    lineTotal REAL NOT NULL,
    newRetailPrice REAL,
    newWholesalePrice REAL
);

CREATE INDEX IF NOT EXISTS "idx_purchase_items_invoice" ON "PurchaseInvoiceItem" (purchaseInvoiceId);
CREATE INDEX IF NOT EXISTS "idx_purchases_status" ON "PurchaseInvoice" (status, date);

INSERT OR IGNORE INTO "Permission" (id, key, name, description, resource, action, createdAt, updatedAt) VALUES
('purchases.view',   'purchases.view',   'View Purchases',   'View and search purchase invoices',              'purchases', 'view',   CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('purchases.create', 'purchases.create', 'Create Purchases', 'Create / post purchase invoices',                'purchases', 'create', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('purchases.update', 'purchases.update', 'Update Purchases', 'Edit drafts, edit posted, post and cancel',      'purchases', 'update', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

INSERT OR IGNORE INTO "RolePermission" (roleId, permissionId)
SELECT 'role-admin', p.id FROM "Permission" p
WHERE p.id IN ('purchases.view','purchases.create','purchases.update');

INSERT OR IGNORE INTO "RolePermission" (roleId, permissionId)
SELECT 'role-manager', p.id FROM "Permission" p
WHERE p.id IN ('purchases.view','purchases.create','purchases.update');
