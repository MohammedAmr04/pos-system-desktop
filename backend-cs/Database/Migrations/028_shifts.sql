-- Shifts & Cash Sessions (Phase 9) — one active shift at a time; every real money
-- movement through the drawer is already a Payment row, so the shift only needs to
-- stamp those rows and carry its own opening/closing numbers (spec §27).
-- Expected Cash = opening + cash sales + other cash in − cash returns − cash out.
CREATE TABLE IF NOT EXISTS "Shift" (
    id TEXT PRIMARY KEY,
    number INTEGER NOT NULL UNIQUE,
    openedBy TEXT NOT NULL,
    openingCash REAL NOT NULL DEFAULT 0,
    openedAt TEXT NOT NULL,
    closedAt TEXT,
    countedCash REAL,
    expectedCash REAL,
    difference REAL,
    notes TEXT,
    status TEXT NOT NULL DEFAULT 'open'
);

-- Drawer movements are attributed to the shift that was open when they happened.
ALTER TABLE "Payment" ADD COLUMN shiftId TEXT;
CREATE INDEX IF NOT EXISTS "idx_payments_shift" ON "Payment" (shiftId);

-- Every posted sale is stamped with its shift for per-shift sales reporting.
ALTER TABLE "Invoice" ADD COLUMN shiftId TEXT;

INSERT OR IGNORE INTO "Permission" (id, key, name, description, resource, action, createdAt, updatedAt) VALUES
('shifts.view', 'shifts.view', 'View Shifts', 'View shifts and their cash reports', 'shifts', 'view', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('shifts.open', 'shifts.open', 'Open Shift', 'Open a new cash shift with an opening float', 'shifts', 'open', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('shifts.close', 'shifts.close', 'Close Shift', 'Close the active shift with a cash count', 'shifts', 'close', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

INSERT OR IGNORE INTO "RolePermission" (roleId, permissionId)
SELECT 'role-admin', p.id FROM "Permission" p
WHERE p.id IN ('shifts.view', 'shifts.open', 'shifts.close');

INSERT OR IGNORE INTO "RolePermission" (roleId, permissionId)
SELECT 'role-manager', p.id FROM "Permission" p
WHERE p.id IN ('shifts.view', 'shifts.open', 'shifts.close');

INSERT OR IGNORE INTO "RolePermission" (roleId, permissionId)
SELECT 'role-cashier', p.id FROM "Permission" p
WHERE p.id IN ('shifts.view', 'shifts.open', 'shifts.close');
