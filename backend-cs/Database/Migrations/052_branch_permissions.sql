-- Migration 052: Seed the branch/terminal administration permissions and grant them to the
-- built-in admin and manager roles. Reuses the existing centralized permission model.

INSERT OR IGNORE INTO "Permission" (id, key, name, description, resource, action, createdAt, updatedAt) VALUES
('branches.view',   'branches.view',   'View Branches',   'View branches, terminals and their configuration', 'branches', 'view',   CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('branches.manage', 'branches.manage', 'Manage Branches', 'Create/update branches and terminals, configure runtime and switch branch', 'branches', 'manage', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

INSERT OR IGNORE INTO "RolePermission" (roleId, permissionId)
SELECT 'role-admin', p.id FROM "Permission" p
WHERE p.id IN ('branches.view','branches.manage');

INSERT OR IGNORE INTO "RolePermission" (roleId, permissionId)
SELECT 'role-manager', p.id FROM "Permission" p
WHERE p.id IN ('branches.view','branches.manage');
