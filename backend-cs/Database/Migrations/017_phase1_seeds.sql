INSERT OR IGNORE INTO "Permission" (id, key, name, description, resource, action, createdAt, updatedAt) VALUES
('categories.view',   'categories.view',   'View Categories',   'View, search and list categories',            'categories', 'view',   CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('categories.create', 'categories.create', 'Create Categories', 'Create categories',                           'categories', 'create', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('categories.update', 'categories.update', 'Update Categories', 'Rename / activate / deactivate categories',   'categories', 'update', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('brands.view',       'brands.view',       'View Brands',       'View, search and list brands',                'brands',     'view',   CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('brands.create',     'brands.create',     'Create Brands',     'Create brands',                               'brands',     'create', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('brands.update',     'brands.update',     'Update Brands',     'Rename / activate / deactivate brands',       'brands',     'update', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('brands.delete',     'brands.delete',     'Delete Brands',     'Delete brands that no product references',    'brands',     'delete', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('units.view',        'units.view',        'View Units',        'View, search and list units',                 'units',      'view',   CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('units.create',      'units.create',      'Create Units',      'Create units',                                'units',      'create', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('units.update',      'units.update',      'Update Units',      'Rename / activate / deactivate units',        'units',      'update', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('units.delete',      'units.delete',      'Delete Units',      'Delete units that no product references',     'units',      'delete', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

INSERT OR IGNORE INTO "RolePermission" (roleId, permissionId)
SELECT 'role-admin', p.id FROM "Permission" p
WHERE p.id IN ('categories.view','categories.create','categories.update',
               'brands.view','brands.create','brands.update','brands.delete',
               'units.view','units.create','units.update','units.delete');

INSERT OR IGNORE INTO "RolePermission" (roleId, permissionId)
SELECT 'role-manager', p.id FROM "Permission" p
WHERE p.id IN ('categories.view','categories.create','categories.update',
               'brands.view','brands.create','brands.update','brands.delete',
               'units.view','units.create','units.update','units.delete');

INSERT OR IGNORE INTO "TenantFeature" (tenantId, featureKey, enabled) VALUES
('tenant-default', 'categories', 1),
('tenant-default', 'brands', 1);
