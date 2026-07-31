CREATE TABLE IF NOT EXISTS "ProductUnit" (
    "id" TEXT NOT NULL PRIMARY KEY,
    "productId" TEXT NOT NULL,
    "unitName" TEXT NOT NULL,
    "quantityFactor" REAL NOT NULL,
    "retailPrice" REAL NOT NULL,
    "wholesalePrice" REAL,
    "isBaseUnit" INTEGER NOT NULL DEFAULT 0,
    "createdAt" DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY ("productId") REFERENCES "Product"("id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "ProductUnit_productId_idx" ON "ProductUnit"("productId");

CREATE UNIQUE INDEX IF NOT EXISTS "ProductUnit_baseUnit_idx" ON "ProductUnit"("productId") WHERE "isBaseUnit" = 1;

INSERT INTO "ProductUnit" ("id", "productId", "unitName", "quantityFactor", "retailPrice", "wholesalePrice", "isBaseUnit", "createdAt")
    SELECT lower(hex(randomblob(16))), "id", 'Piece', 1, "salePrice", NULL, 1, "createdAt"
    FROM "Product";

CREATE TABLE "ProductBarcode_new" (
    "id" TEXT NOT NULL PRIMARY KEY,
    "productUnitId" TEXT NOT NULL,
    "barcode" TEXT NOT NULL,
    "isDefault" INTEGER NOT NULL DEFAULT 0,
    "createdAt" DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY ("productUnitId") REFERENCES "ProductUnit"("id") ON DELETE CASCADE
);

INSERT INTO "ProductBarcode_new" ("id", "productUnitId", "barcode", "isDefault", "createdAt")
    SELECT b."id", u."id", b."barcode", b."isDefault", b."createdAt"
    FROM "ProductBarcode" b
    JOIN "ProductUnit" u ON u."productId" = b."productId" AND u."isBaseUnit" = 1;

DROP TABLE "ProductBarcode";

ALTER TABLE "ProductBarcode_new" RENAME TO "ProductBarcode";

CREATE UNIQUE INDEX IF NOT EXISTS "ProductBarcode_barcode_key" ON "ProductBarcode"("barcode");

CREATE INDEX IF NOT EXISTS "ProductBarcode_productUnitId_idx" ON "ProductBarcode"("productUnitId");

ALTER TABLE "Invoice" ADD COLUMN "priceMode" TEXT;

ALTER TABLE "InvoiceDetail" ADD COLUMN "productUnitId" TEXT;

ALTER TABLE "InvoiceDetail" ADD COLUMN "unitName" TEXT;

ALTER TABLE "InvoiceDetail" ADD COLUMN "originalUnitPrice" REAL NOT NULL DEFAULT 0;

ALTER TABLE "InvoiceDetail" ADD COLUMN "unitPrice" REAL NOT NULL DEFAULT 0;

ALTER TABLE "InvoiceDetail" ADD COLUMN "discountType" TEXT;

ALTER TABLE "InvoiceDetail" ADD COLUMN "discountValue" REAL NOT NULL DEFAULT 0;

ALTER TABLE "InvoiceDetail" ADD COLUMN "lineSubtotal" REAL NOT NULL DEFAULT 0;

ALTER TABLE "InvoiceDetail" ADD COLUMN "finalTotal" REAL NOT NULL DEFAULT 0;

UPDATE "InvoiceDetail" SET
    "productUnitId" = (SELECT u."id" FROM "ProductUnit" u WHERE u."productId" = "InvoiceDetail"."productId" AND u."isBaseUnit" = 1),
    "unitName" = 'Piece',
    "originalUnitPrice" = "salePrice",
    "unitPrice" = "salePrice",
    "lineSubtotal" = ROUND("salePrice" * "quantity", 2),
    "finalTotal" = ROUND("salePrice" * "quantity" - "discountAmount", 2);

UPDATE "Invoice" SET "priceMode" = 'retail' WHERE "priceMode" IS NULL OR TRIM("priceMode") = '';

ALTER TABLE "Product" DROP COLUMN "salePrice";
