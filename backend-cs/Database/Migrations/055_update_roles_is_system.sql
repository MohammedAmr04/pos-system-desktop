-- Update Manager and Cashier roles to be non-system (editable)
-- Admin role remains system (locked)

UPDATE "Role" 
SET isSystem = 0,
    updatedAt = CURRENT_TIMESTAMP
WHERE id IN ('role-manager', 'role-cashier')
  AND isSystem = 1;

-- Verify the changes
SELECT id, name, isSystem 
FROM "Role" 
WHERE id IN ('role-admin', 'role-manager', 'role-cashier');