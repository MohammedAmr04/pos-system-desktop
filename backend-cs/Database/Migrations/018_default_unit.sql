-- Ensure the default "قطعة" master unit always exists so product forms
-- can preselect it on fresh databases (no products backfilled yet)
INSERT INTO "Unit" (id, name, isActive, createdAt, updatedAt)
SELECT LOWER(HEX(RANDOMBLOB(16))), 'قطعة', 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
WHERE NOT EXISTS (SELECT 1 FROM Unit WHERE name = 'قطعة' COLLATE NOCASE);
