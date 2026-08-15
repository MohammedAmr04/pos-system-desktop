INSERT OR IGNORE INTO "Role" (id, name, description, isSystem, createdAt, updatedAt) VALUES
('role-admin',   'Admin',   'Full access; manages users, roles, settings and license',   1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('role-manager', 'Manager', 'All selling and product operations incl. discounts, price override and reports', 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('role-cashier', 'Cashier', 'View products, view and create invoices, print receipts only', 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

INSERT OR IGNORE INTO "RolePermission" (roleId, permissionId) VALUES
('role-admin', 'products.view'),
('role-admin', 'products.create'),
('role-admin', 'products.update'),
('role-admin', 'products.delete'),
('role-admin', 'invoices.view'),
('role-admin', 'invoices.create'),
('role-admin', 'discounts.product'),
('role-admin', 'discounts.invoice'),
('role-admin', 'price.override'),
('role-admin', 'reports.view'),
('role-admin', 'reports.export'),
('role-admin', 'printing.receipt'),
('role-admin', 'printing.barcode'),
('role-admin', 'license.view'),
('role-admin', 'license.manage'),
('role-admin', 'settings.view'),
('role-admin', 'settings.update'),
('role-admin', 'users.manage'),
('role-admin', 'roles.manage');

INSERT OR IGNORE INTO "RolePermission" (roleId, permissionId) VALUES
('role-manager', 'products.view'),
('role-manager', 'products.create'),
('role-manager', 'products.update'),
('role-manager', 'products.delete'),
('role-manager', 'invoices.view'),
('role-manager', 'invoices.create'),
('role-manager', 'discounts.product'),
('role-manager', 'discounts.invoice'),
('role-manager', 'price.override'),
('role-manager', 'reports.view'),
('role-manager', 'printing.receipt'),
('role-manager', 'printing.barcode'),
('role-manager', 'license.view');

INSERT OR IGNORE INTO "RolePermission" (roleId, permissionId) VALUES
('role-cashier', 'products.view'),
('role-cashier', 'invoices.view'),
('role-cashier', 'invoices.create'),
('role-cashier', 'printing.receipt'),
('role-cashier', 'license.view');
