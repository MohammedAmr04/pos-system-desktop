-- Brands master data (deletable only when no product references it)
CREATE TABLE IF NOT EXISTS "Brand" (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    isActive INTEGER NOT NULL DEFAULT 1,
    createdAt TEXT NOT NULL,
    updatedAt TEXT NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS "UX_Brand_name" ON "Brand" (name COLLATE NOCASE);

ALTER TABLE "Product" ADD COLUMN brandId TEXT;

CREATE INDEX IF NOT EXISTS "IX_Product_brandId" ON "Product" (brandId);
