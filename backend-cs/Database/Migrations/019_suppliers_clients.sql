-- Suppliers & Clients (Phase 2) — parties are deactivated once referenced, never deleted
-- (spec §9.2 rule 7 / §10.2). Balances become meaningful in Phase 5 (Payments).
CREATE TABLE IF NOT EXISTS "Supplier" (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    phone TEXT,
    address TEXT,
    notes TEXT,
    isActive INTEGER NOT NULL DEFAULT 1,
    createdAt TEXT NOT NULL,
    updatedAt TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS "Client" (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    phone TEXT,
    address TEXT,
    notes TEXT,
    isActive INTEGER NOT NULL DEFAULT 1,
    createdAt TEXT NOT NULL,
    updatedAt TEXT NOT NULL
);

INSERT OR IGNORE INTO "Permission" (id, key, name, description, resource, action, createdAt, updatedAt) VALUES
('suppliers.view',   'suppliers.view',   'View Suppliers',   'View, search and list suppliers',            'suppliers', 'view',   CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('suppliers.create', 'suppliers.create', 'Create Suppliers', 'Create suppliers',                           'suppliers', 'create', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('suppliers.update', 'suppliers.update', 'Update Suppliers', 'Edit / activate / deactivate suppliers',     'suppliers', 'update', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('clients.view',     'clients.view',     'View Clients',     'View, search and list clients',              'clients',   'view',   CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('clients.create',   'clients.create',   'Create Clients',   'Create clients',                             'clients',   'create', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('clients.update',   'clients.update',   'Update Clients',   'Edit / activate / deactivate clients',       'clients',   'update', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

INSERT OR IGNORE INTO "RolePermission" (roleId, permissionId)
SELECT 'role-admin', p.id FROM "Permission" p
WHERE p.id IN ('suppliers.view','suppliers.create','suppliers.update',
               'clients.view','clients.create','clients.update');

INSERT OR IGNORE INTO "RolePermission" (roleId, permissionId)
SELECT 'role-manager', p.id FROM "Permission" p
WHERE p.id IN ('suppliers.view','suppliers.create','suppliers.update',
               'clients.view','clients.create','clients.update');
