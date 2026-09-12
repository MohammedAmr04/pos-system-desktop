CREATE TABLE IF NOT EXISTS "Employee" (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    phone TEXT,
    isActive INTEGER NOT NULL DEFAULT 1,
    createdAt TEXT NOT NULL,
    updatedAt TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS "idx_employee_name" ON "Employee" (name);

ALTER TABLE "Invoice" ADD COLUMN "employeeId" TEXT;
CREATE INDEX IF NOT EXISTS "idx_invoice_employee" ON "Invoice" (employeeId);

INSERT OR IGNORE INTO "Permission" (id, key, name, description, resource, action, createdAt, updatedAt) VALUES
('employees.view', 'employees.view', 'View Employees', 'View the employee directory', 'employees', 'view', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('employees.manage', 'employees.manage', 'Manage Employees', 'Create and update employees', 'employees', 'manage', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

INSERT OR IGNORE INTO "RolePermission" (roleId, permissionId)
SELECT r.id, p.id FROM "Role" r CROSS JOIN "Permission" p
WHERE r.id IN ('role-admin', 'role-manager')
  AND p.id IN ('employees.view', 'employees.manage');
