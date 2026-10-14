-- Migration 046: Retry scheduling for the local SyncOutbox.
-- Failed operations now back off and, after a capped number of attempts, move to
-- 'dead' so they stop retrying and surface to the operator.
ALTER TABLE "SyncOutbox" ADD COLUMN "nextAttemptAt" DATETIME;

CREATE INDEX IF NOT EXISTS "idx_sync_outbox_retry"
    ON "SyncOutbox" ("status", "nextAttemptAt");

UPDATE "SyncOutbox" SET "nextAttemptAt" = NULL WHERE "status" = 'failed';
