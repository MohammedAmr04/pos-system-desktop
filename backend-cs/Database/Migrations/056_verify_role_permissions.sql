-- Ensure Manager and Cashier roles have their permissions properly set
-- Note: They will inherit the permissions already defined in migration 011

-- Verify that Manager and Cashier roles exist with their permissions
SELECT r.id, r.name, r.isSystem, COUNT(rp.permissionId) as permissionCount
FROM "Role" r
LEFT JOIN "RolePermission" rp ON r.id = rp.roleId
WHERE r.id IN ('role-manager', 'role-cashier')
GROUP BY r.id, r.name, r.isSystem;