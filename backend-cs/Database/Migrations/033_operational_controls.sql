CREATE TABLE IF NOT EXISTS "AuditLog" (
    id TEXT PRIMARY KEY,
    actorUserId TEXT,
    action TEXT NOT NULL,
    entityType TEXT NOT NULL,
    entityId TEXT,
    summary TEXT NOT NULL,
    beforeJson TEXT,
    afterJson TEXT,
    createdAt TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS "idx_audit_log_created" ON "AuditLog" (createdAt);
CREATE INDEX IF NOT EXISTS "idx_audit_log_entity" ON "AuditLog" (entityType, entityId);

CREATE TABLE IF NOT EXISTS "Alert" (
    id TEXT PRIMARY KEY,
    type TEXT NOT NULL,
    severity TEXT NOT NULL,
    entityType TEXT,
    entityId TEXT,
    message TEXT NOT NULL,
    status TEXT NOT NULL DEFAULT 'open',
    acknowledgedBy TEXT,
    acknowledgedAt TEXT,
    createdAt TEXT NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS "idx_alert_open_unique" ON "Alert" (type, entityType, entityId, status);

CREATE TABLE IF NOT EXISTS "CashDrawerMovement" (
    id TEXT PRIMARY KEY,
    shiftId TEXT NOT NULL,
    type TEXT NOT NULL,
    amount REAL NOT NULL,
    reason TEXT NOT NULL,
    createdBy TEXT NOT NULL,
    createdAt TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS "idx_drawer_movement_shift" ON "CashDrawerMovement" (shiftId, createdAt);

CREATE TABLE IF NOT EXISTS "InventoryAdjustment" (
    id TEXT PRIMARY KEY,
    number INTEGER NOT NULL UNIQUE,
    reason TEXT NOT NULL,
    createdBy TEXT NOT NULL,
    createdAt TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS "InventoryAdjustmentLine" (
    id TEXT PRIMARY KEY,
    adjustmentId TEXT NOT NULL,
    productId TEXT NOT NULL,
    systemQuantity REAL NOT NULL,
    actualQuantity REAL NOT NULL,
    differenceQuantity REAL NOT NULL,
    unitCost REAL NOT NULL
);
CREATE INDEX IF NOT EXISTS "idx_adjustment_line_adjustment" ON "InventoryAdjustmentLine" (adjustmentId);

INSERT OR IGNORE INTO "Permission" (id, key, name, description, resource, action, createdAt, updatedAt) VALUES
('audit.view', 'audit.view', 'View Audit Log', 'View sensitive operation history', 'audit', 'view', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('inventory.adjustments.view', 'inventory.adjustments.view', 'View Inventory Adjustments', 'View inventory adjustment records', 'inventory', 'adjustments.view', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('inventory.adjustments.create', 'inventory.adjustments.create', 'Create Inventory Adjustments', 'Post inventory adjustments', 'inventory', 'adjustments.create', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('alerts.view', 'alerts.view', 'View Alerts', 'View operational alerts', 'alerts', 'view', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('alerts.acknowledge', 'alerts.acknowledge', 'Acknowledge Alerts', 'Acknowledge operational alerts', 'alerts', 'acknowledge', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('shifts.drawer.manage', 'shifts.drawer.manage', 'Manage Cash Drawer', 'Create manual drawer movements', 'shifts', 'drawer.manage', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

INSERT OR IGNORE INTO "RolePermission" (roleId, permissionId)
SELECT r.id, p.id FROM "Role" r CROSS JOIN "Permission" p
WHERE r.id IN ('role-admin', 'role-manager')
  AND p.id IN ('audit.view', 'inventory.adjustments.view', 'inventory.adjustments.create', 'alerts.view', 'alerts.acknowledge', 'shifts.drawer.manage');
