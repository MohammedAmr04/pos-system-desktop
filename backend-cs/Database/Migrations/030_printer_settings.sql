-- Printer Settings (Phase 12, spec §28) — moves printing configuration out of hardcoded
-- values into a simple key/value store. Missing keys fall back to code defaults.
CREATE TABLE IF NOT EXISTS "PrinterSetting" (
    key TEXT PRIMARY KEY,
    value TEXT NOT NULL,
    updatedAt TEXT NOT NULL
);
