CREATE TABLE IF NOT EXISTS "Tenant" (
    "id" TEXT NOT NULL PRIMARY KEY,
    "name" TEXT NOT NULL,
    "createdAt" DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "updatedAt" DATETIME NOT NULL
);

CREATE TABLE IF NOT EXISTS "User" (
    "id" TEXT NOT NULL PRIMARY KEY,
    "tenantId" TEXT NOT NULL,
    "name" TEXT NOT NULL,
    "pinHash" TEXT NOT NULL,
    "isActive" INTEGER NOT NULL DEFAULT 1,
    "createdAt" DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "updatedAt" DATETIME NOT NULL,
    FOREIGN KEY ("tenantId") REFERENCES "Tenant"("id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "User_tenantId_key" ON "User"("tenantId");

CREATE TABLE IF NOT EXISTS "Role" (
    "id" TEXT NOT NULL PRIMARY KEY,
    "name" TEXT NOT NULL UNIQUE,
    "description" TEXT,
    "isSystem" INTEGER NOT NULL DEFAULT 0,
    "createdAt" DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "updatedAt" DATETIME NOT NULL
);

CREATE TABLE IF NOT EXISTS "Permission" (
    "id" TEXT NOT NULL PRIMARY KEY,
    "key" TEXT NOT NULL UNIQUE,
    "name" TEXT NOT NULL,
    "description" TEXT,
    "resource" TEXT NOT NULL,
    "action" TEXT NOT NULL,
    "createdAt" DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "updatedAt" DATETIME NOT NULL
);

CREATE TABLE IF NOT EXISTS "UserRole" (
    "userId" TEXT NOT NULL,
    "roleId" TEXT NOT NULL,
    PRIMARY KEY ("userId", "roleId"),
    FOREIGN KEY ("userId") REFERENCES "User"("id") ON DELETE CASCADE,
    FOREIGN KEY ("roleId") REFERENCES "Role"("id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "UserRole_roleId_key" ON "UserRole"("roleId");

CREATE TABLE IF NOT EXISTS "RolePermission" (
    "roleId" TEXT NOT NULL,
    "permissionId" TEXT NOT NULL,
    PRIMARY KEY ("roleId", "permissionId"),
    FOREIGN KEY ("roleId") REFERENCES "Role"("id") ON DELETE CASCADE,
    FOREIGN KEY ("permissionId") REFERENCES "Permission"("id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "RolePermission_permissionId_key" ON "RolePermission"("permissionId");

CREATE TABLE IF NOT EXISTS "TenantFeature" (
    "tenantId" TEXT NOT NULL,
    "featureKey" TEXT NOT NULL,
    "enabled" INTEGER NOT NULL DEFAULT 1,
    PRIMARY KEY ("tenantId", "featureKey"),
    FOREIGN KEY ("tenantId") REFERENCES "Tenant"("id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "TenantFeature_featureKey_key" ON "TenantFeature"("featureKey");

ALTER TABLE "Settings" ADD COLUMN "tokenSecret" TEXT;
