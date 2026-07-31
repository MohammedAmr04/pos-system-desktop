CREATE TABLE IF NOT EXISTS "ProductBarcode" (
    "id" TEXT NOT NULL PRIMARY KEY,
    "productId" TEXT NOT NULL,
    "barcode" TEXT NOT NULL,
    "isDefault" INTEGER NOT NULL DEFAULT 0,
    "createdAt" DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY ("productId") REFERENCES "Product"("id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX IF NOT EXISTS "ProductBarcode_barcode_key" ON "ProductBarcode"("barcode");

CREATE INDEX IF NOT EXISTS "ProductBarcode_productId_idx" ON "ProductBarcode"("productId");

INSERT INTO "ProductBarcode" ("id", "productId", "barcode", "isDefault", "createdAt")
    SELECT lower(hex(randomblob(16))), "id", "barcode", 1, "createdAt"
    FROM "Product"
    WHERE "barcode" IS NOT NULL AND TRIM("barcode") <> '';

DROP INDEX IF EXISTS "Product_barcode_key";

ALTER TABLE "Product" DROP COLUMN "barcode";
