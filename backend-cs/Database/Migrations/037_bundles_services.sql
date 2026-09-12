ALTER TABLE "Product" ADD COLUMN "productType" TEXT NOT NULL DEFAULT 'product';
ALTER TABLE "Product" ADD COLUMN "serviceCost" REAL NOT NULL DEFAULT 0;
ALTER TABLE "InvoiceDetail" ADD COLUMN "bundleComponentsJson" TEXT;

CREATE TABLE IF NOT EXISTS "BundleComponent" (
    "id" TEXT NOT NULL PRIMARY KEY,
    "bundleProductId" TEXT NOT NULL,
    "componentProductId" TEXT NOT NULL,
    "quantity" REAL NOT NULL,
    "createdAt" DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY ("bundleProductId") REFERENCES "Product"("id") ON DELETE CASCADE,
    FOREIGN KEY ("componentProductId") REFERENCES "Product"("id") ON DELETE RESTRICT,
    CHECK ("quantity" > 0),
    CHECK ("bundleProductId" <> "componentProductId"),
    UNIQUE ("bundleProductId", "componentProductId")
);

CREATE INDEX IF NOT EXISTS "idx_bundle_component_bundle" ON "BundleComponent"("bundleProductId");
CREATE INDEX IF NOT EXISTS "idx_bundle_component_product" ON "BundleComponent"("componentProductId");
