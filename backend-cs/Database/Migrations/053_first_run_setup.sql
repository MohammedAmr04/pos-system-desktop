-- Migration 053: First-run node setup.
-- Persists whether the operator has completed the explicit role/enrollment step. A brand-new
-- install starts incomplete and the app forces the setup screen; an existing install that already
-- has central credentials, a central role, or any posted history is backfilled as complete so
-- upgrading devices are not pushed back through setup.

ALTER TABLE "BranchRuntimeConfig" ADD COLUMN "setupCompletedAt" DATETIME;

UPDATE "BranchRuntimeConfig"
SET "setupCompletedAt" = CURRENT_TIMESTAMP
WHERE "centralBaseUrl" IS NOT NULL
   OR "centralToken" IS NOT NULL
   OR "centralBranchKey" IS NOT NULL
   OR "nodeRole" = 'central'
   OR EXISTS (SELECT 1 FROM "Invoice" LIMIT 1);
