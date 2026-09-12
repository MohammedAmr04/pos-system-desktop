-- Expenses (Phase 10) — operating costs, strictly separate from purchases (spec §26).
-- A cash expense is a drawer movement: it is stamped with the active shift and reduces
-- its Expected Cash (spec §27). Categories are managed separately.
CREATE TABLE IF NOT EXISTS "ExpenseCategory" (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    isActive INTEGER NOT NULL DEFAULT 1,
    createdAt TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS "Expense" (
    id TEXT PRIMARY KEY,
    categoryId TEXT NOT NULL,
    amount REAL NOT NULL,
    paymentMethod TEXT NOT NULL DEFAULT 'cash',
    date TEXT NOT NULL,
    description TEXT,
    reference TEXT,
    shiftId TEXT,
    createdBy TEXT,
    createdAt TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS "idx_expenses_category" ON "Expense" (categoryId);
CREATE INDEX IF NOT EXISTS "idx_expenses_shift" ON "Expense" (shiftId);

-- Spec §26 example categories, seeded once.
INSERT OR IGNORE INTO "ExpenseCategory" (id, name, isActive, createdAt) VALUES
('exp-cat-rent',          'إيجار',        1, CURRENT_TIMESTAMP),
('exp-cat-electricity',   'كهرباء',       1, CURRENT_TIMESTAMP),
('exp-cat-water',         'مياه',         1, CURRENT_TIMESTAMP),
('exp-cat-internet',      'إنترنت',       1, CURRENT_TIMESTAMP),
('exp-cat-maintenance',   'صيانة',        1, CURRENT_TIMESTAMP),
('exp-cat-transportation','نقل',          1, CURRENT_TIMESTAMP),
('exp-cat-salaries',      'مرتبات',       1, CURRENT_TIMESTAMP),
('exp-cat-other',         'أخرى',         1, CURRENT_TIMESTAMP);

INSERT OR IGNORE INTO "Permission" (id, key, name, description, resource, action, createdAt, updatedAt) VALUES
('expenses.view',      'expenses.view',      'View Expenses',      'View expenses and their categories', 'expenses', 'view', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('expenses.create',    'expenses.create',    'Create Expenses',    'Record cash expenses against the active shift', 'expenses', 'create', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('expenses.categories','expenses.categories','Manage Expense Categories', 'Create, rename and deactivate expense categories', 'expenses', 'categories', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

INSERT OR IGNORE INTO "RolePermission" (roleId, permissionId)
SELECT 'role-admin', p.id FROM "Permission" p
WHERE p.id IN ('expenses.view', 'expenses.create', 'expenses.categories');

INSERT OR IGNORE INTO "RolePermission" (roleId, permissionId)
SELECT 'role-manager', p.id FROM "Permission" p
WHERE p.id IN ('expenses.view', 'expenses.create', 'expenses.categories');

INSERT OR IGNORE INTO "RolePermission" (roleId, permissionId)
SELECT 'role-cashier', p.id FROM "Permission" p
WHERE p.id IN ('expenses.view', 'expenses.create');
