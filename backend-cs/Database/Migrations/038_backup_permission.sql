INSERT OR IGNORE INTO "Permission" (id, key, name, description, resource, action, createdAt, updatedAt)
VALUES ('backups.manage', 'backups.manage', 'Manage Backups', 'Create and restore database backups', 'backups', 'manage', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

INSERT OR IGNORE INTO "RolePermission" (roleId, permissionId)
SELECT r.id, 'backups.manage' FROM "Role" r
WHERE r.id = 'role-admin';
