-- Username + Password login (replaces PIN-only login)
-- Adds a unique `username` column and renames `pinHash` -> `passwordHash`.
-- Existing users get `username` derived from their display name.

ALTER TABLE "User" ADD COLUMN "username" TEXT;

UPDATE "User" SET "username" = LOWER(REPLACE("name", ' ', '_')) WHERE "username" IS NULL OR "username" = '';

-- The seeded admin keeps a stable, documented login.
UPDATE "User" SET "username" = 'admin' WHERE "id" = 'user-admin' AND ("username" IS NULL OR "username" = '');

ALTER TABLE "User" RENAME COLUMN "pinHash" TO "passwordHash";

CREATE UNIQUE INDEX IF NOT EXISTS "User_username_key" ON "User"("username");
