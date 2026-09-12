ALTER TABLE "Settings" ADD COLUMN "licenseType" TEXT NOT NULL DEFAULT 'trial';
ALTER TABLE "Settings" ADD COLUMN "trialDays" INTEGER NOT NULL DEFAULT 14;
ALTER TABLE "Settings" ADD COLUMN "licenseStartedAt" DATETIME NULL;
ALTER TABLE "Settings" ADD COLUMN "licenseExpiresAt" DATETIME NULL;

UPDATE "Settings"
SET "licenseType" = CASE WHEN "unlocked" = 1 THEN 'permanent' ELSE 'trial' END,
    "trialDays" = 14,
    "licenseStartedAt" = COALESCE("licenseStartedAt", "createdAt"),
    "licenseExpiresAt" = CASE
        WHEN "unlocked" = 1 THEN NULL
        ELSE datetime(COALESCE("licenseStartedAt", "createdAt"), '+14 days')
    END;
