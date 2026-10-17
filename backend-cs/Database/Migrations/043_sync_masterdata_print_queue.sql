DROP TRIGGER IF EXISTS "trg_product_sync_insert";
DROP TRIGGER IF EXISTS "trg_product_sync_update";
DROP TRIGGER IF EXISTS "trg_category_sync_insert";
DROP TRIGGER IF EXISTS "trg_category_sync_update";
DROP TRIGGER IF EXISTS "trg_brand_sync_insert";
DROP TRIGGER IF EXISTS "trg_brand_sync_update";

CREATE TRIGGER "trg_product_sync_insert"
AFTER INSERT ON "Product"
BEGIN
    INSERT INTO "SyncChangeLog" (branchId,entityType,entityId,operationType,payload)
    VALUES (NULL,'Product',NEW.id,'upsert',json_object(
        'id',NEW.id,'name',NEW.name,'productType',NEW.productType,
        'serviceCost',NEW.serviceCost,'buyPrice',NEW.buyPrice,
        'stockQuantity',NEW.stockQuantity,'notes',NEW.notes,'allowDiscount',NEW.allowDiscount,
        'lowStockThreshold',NEW.lowStockThreshold,'isHiddenFromPOS',NEW.isHiddenFromPOS,
        'categoryId',NEW.categoryId,'brandId',NEW.brandId,'createdAt',NEW.createdAt,'updatedAt',NEW.updatedAt));
END;

CREATE TRIGGER "trg_product_sync_update"
AFTER UPDATE ON "Product"
BEGIN
    INSERT INTO "SyncChangeLog" (branchId,entityType,entityId,operationType,payload)
    VALUES (NULL,'Product',NEW.id,'upsert',json_object(
        'id',NEW.id,'name',NEW.name,'productType',NEW.productType,
        'serviceCost',NEW.serviceCost,'buyPrice',NEW.buyPrice,
        'stockQuantity',NEW.stockQuantity,'notes',NEW.notes,'allowDiscount',NEW.allowDiscount,
        'lowStockThreshold',NEW.lowStockThreshold,'isHiddenFromPOS',NEW.isHiddenFromPOS,
        'categoryId',NEW.categoryId,'brandId',NEW.brandId,'createdAt',NEW.createdAt,'updatedAt',NEW.updatedAt));
END;

CREATE TRIGGER "trg_category_sync_insert"
AFTER INSERT ON "Category"
BEGIN
    INSERT INTO "SyncChangeLog" (branchId,entityType,entityId,operationType,payload)
    VALUES (NULL,'Category',NEW.id,'upsert',json_object('id',NEW.id,'name',NEW.name,'description',NEW.description,'isActive',NEW.isActive,'createdAt',NEW.createdAt,'updatedAt',NEW.updatedAt));
END;

CREATE TRIGGER "trg_category_sync_update"
AFTER UPDATE ON "Category"
BEGIN
    INSERT INTO "SyncChangeLog" (branchId,entityType,entityId,operationType,payload)
    VALUES (NULL,'Category',NEW.id,'upsert',json_object('id',NEW.id,'name',NEW.name,'description',NEW.description,'isActive',NEW.isActive,'createdAt',NEW.createdAt,'updatedAt',NEW.updatedAt));
END;

CREATE TRIGGER "trg_brand_sync_insert"
AFTER INSERT ON "Brand"
BEGIN
    INSERT INTO "SyncChangeLog" (branchId,entityType,entityId,operationType,payload)
    VALUES (NULL,'Brand',NEW.id,'upsert',json_object('id',NEW.id,'name',NEW.name,'isActive',NEW.isActive,'createdAt',NEW.createdAt,'updatedAt',NEW.updatedAt));
END;

CREATE TRIGGER "trg_brand_sync_update"
AFTER UPDATE ON "Brand"
BEGIN
    INSERT INTO "SyncChangeLog" (branchId,entityType,entityId,operationType,payload)
    VALUES (NULL,'Brand',NEW.id,'upsert',json_object('id',NEW.id,'name',NEW.name,'isActive',NEW.isActive,'createdAt',NEW.createdAt,'updatedAt',NEW.updatedAt));
END;

CREATE TRIGGER "trg_product_unit_sync_insert"
AFTER INSERT ON "ProductUnit"
BEGIN
    INSERT INTO "SyncChangeLog" (branchId,entityType,entityId,operationType,payload)
    VALUES (NULL,'ProductUnit',NEW.id,'upsert',json_object(
        'id',NEW.id,'productId',NEW.productId,'unitName',NEW.unitName,'unitId',NEW.unitId,
        'quantityFactor',NEW.quantityFactor,'retailPrice',NEW.retailPrice,'wholesalePrice',NEW.wholesalePrice,
        'isBaseUnit',NEW.isBaseUnit,'createdAt',NEW.createdAt));
END;

CREATE TRIGGER "trg_product_unit_sync_update"
AFTER UPDATE ON "ProductUnit"
BEGIN
    INSERT INTO "SyncChangeLog" (branchId,entityType,entityId,operationType,payload)
    VALUES (NULL,'ProductUnit',NEW.id,'upsert',json_object(
        'id',NEW.id,'productId',NEW.productId,'unitName',NEW.unitName,'unitId',NEW.unitId,
        'quantityFactor',NEW.quantityFactor,'retailPrice',NEW.retailPrice,'wholesalePrice',NEW.wholesalePrice,
        'isBaseUnit',NEW.isBaseUnit,'createdAt',NEW.createdAt));
END;

CREATE TABLE IF NOT EXISTS "PrintQueue" (
    "id" TEXT NOT NULL PRIMARY KEY,
    "branchId" TEXT NOT NULL,
    "terminalId" TEXT NOT NULL,
    "jobType" TEXT NOT NULL,
    "payload" TEXT NOT NULL,
    "status" TEXT NOT NULL DEFAULT 'pending',
    "attempts" INTEGER NOT NULL DEFAULT 0,
    "lastError" TEXT,
    "createdAt" DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "printedAt" DATETIME
);

CREATE INDEX IF NOT EXISTS "idx_print_queue_pending"
    ON "PrintQueue" ("status", "createdAt");
