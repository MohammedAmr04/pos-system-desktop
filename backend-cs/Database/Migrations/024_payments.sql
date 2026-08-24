-- Payments (Phase 5) — independent transactions preserving how money moved (spec §19).
-- No mutable PaidAmount anywhere: paid/remaining/status are always DERIVED by summing payments.
-- A payment targets exactly one party (client = money in, supplier = money out);
-- invoiceId is an optional link that must belong to that party.
CREATE TABLE IF NOT EXISTS "Payment" (
    id TEXT PRIMARY KEY,
    amount REAL NOT NULL,
    paymentMethod TEXT NOT NULL DEFAULT 'cash',
    date TEXT NOT NULL,
    invoiceId TEXT,
    clientId TEXT,
    supplierId TEXT,
    reference TEXT,
    notes TEXT,
    createdBy TEXT,
    createdAt TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS "idx_payments_client" ON "Payment" (clientId, date);
CREATE INDEX IF NOT EXISTS "idx_payments_supplier" ON "Payment" (supplierId, date);
CREATE INDEX IF NOT EXISTS "idx_payments_invoice" ON "Payment" (invoiceId);

INSERT OR IGNORE INTO "Permission" (id, key, name, description, resource, action, createdAt, updatedAt) VALUES
('payments.view',   'payments.view',   'View Payments',   'View and search payment records',   'payments', 'view',   CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('payments.create', 'payments.create', 'Record Payments', 'Record new customer / supplier payments', 'payments', 'create', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

INSERT OR IGNORE INTO "RolePermission" (roleId, permissionId)
SELECT 'role-admin', p.id FROM "Permission" p
WHERE p.id IN ('payments.view','payments.create');

INSERT OR IGNORE INTO "RolePermission" (roleId, permissionId)
SELECT 'role-manager', p.id FROM "Permission" p
WHERE p.id IN ('payments.view','payments.create');
