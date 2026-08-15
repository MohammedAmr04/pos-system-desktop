INSERT OR IGNORE INTO "Tenant" (id, name, createdAt, updatedAt)
VALUES ('tenant-default', 'Default Restaurant', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

INSERT OR IGNORE INTO "TenantFeature" (tenantId, featureKey, enabled) VALUES
('tenant-default', 'multiple_units', 1),
('tenant-default', 'multiple_barcodes', 1),
('tenant-default', 'wholesale_price', 1),
('tenant-default', 'product_discount', 1),
('tenant-default', 'invoice_discount', 1),
('tenant-default', 'price_override', 1),
('tenant-default', 'low_stock_report', 1),
('tenant-default', 'receipt_printing', 1),
('tenant-default', 'barcode_printing', 1);

INSERT OR IGNORE INTO "User" (id, tenantId, name, pinHash, isActive, createdAt, updatedAt)
VALUES ('user-admin', 'tenant-default', 'Admin', 'pbkdf2$100000$DWDqj4MmN8WfOLnKh2T7HQ==$oUkX8Q9bnuzlRkq9GQ4nsJ01nlpal95zDv7t3/xSqKc=', 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

INSERT OR IGNORE INTO "UserRole" (userId, roleId) VALUES ('user-admin', 'role-admin');
