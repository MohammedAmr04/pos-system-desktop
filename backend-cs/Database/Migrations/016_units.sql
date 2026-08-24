-- Shared Unit master (the conversion factor lives on ProductUnit, not here)
CREATE TABLE IF NOT EXISTS "Unit" (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    isActive INTEGER NOT NULL DEFAULT 1,
    createdAt TEXT NOT NULL,
    updatedAt TEXT NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS "UX_Unit_name" ON "Unit" (name COLLATE NOCASE);

ALTER TABLE "ProductUnit" ADD COLUMN unitId TEXT;

-- Backfill the Unit master from existing free-text product unit names
INSERT OR IGNORE INTO "Unit" (id, name, isActive, createdAt, updatedAt)
SELECT LOWER(HEX(RANDOMBLOB(16))), pu.unitName, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
FROM (SELECT DISTINCT TRIM(unitName) AS unitName FROM ProductUnit
      WHERE unitName IS NOT NULL AND TRIM(unitName) <> '') pu
WHERE NOT EXISTS (SELECT 1 FROM Unit u WHERE u.name = pu.unitName COLLATE NOCASE);

-- Link existing product units to their Unit master row (unitName stays as the historical snapshot)
UPDATE ProductUnit
SET unitId = (SELECT u.id FROM Unit u WHERE u.name = ProductUnit.unitName COLLATE NOCASE)
WHERE unitId IS NULL AND unitName IS NOT NULL AND TRIM(unitName) <> '';
