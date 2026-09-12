-- Force password change on first login for the seeded admin user.
-- Adds a mustChangePassword flag and sets it for the default admin.

ALTER TABLE "User" ADD COLUMN "mustChangePassword" INTEGER NOT NULL DEFAULT 0;

UPDATE "User" SET "mustChangePassword" = 1, "updatedAt" = CURRENT_TIMESTAMP WHERE "id" = 'user-admin';
