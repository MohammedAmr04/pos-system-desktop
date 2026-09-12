-- Categories master data (single-level, no hierarchy; never deleted, only deactivated)
CREATE TABLE IF NOT EXISTS "Category" (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    description TEXT,
    isActive INTEGER NOT NULL DEFAULT 1,
    createdAt TEXT NOT NULL,
    updatedAt TEXT NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS "UX_Category_name" ON "Category" (name COLLATE NOCASE);

ALTER TABLE "Product" ADD COLUMN categoryId TEXT;

CREATE INDEX IF NOT EXISTS "IX_Product_categoryId" ON "Product" (categoryId);
