-- Migration 051: Central branch/terminal administration and safe terminal branch switching.
-- Adds terminal liveness/status, the runtime config credentials used to call central for a
-- branch switch, and an immutable per-terminal branch assignment audit trail.

ALTER TABLE "Terminal" ADD COLUMN "lastSeenAt" DATETIME;
ALTER TABLE "Terminal" ADD COLUMN "notes" TEXT;

CREATE INDEX IF NOT EXISTS "idx_terminal_branch" ON "Terminal" ("branchId", "isActive");

ALTER TABLE "BranchRuntimeConfig" ADD COLUMN "centralToken" TEXT;
ALTER TABLE "BranchRuntimeConfig" ADD COLUMN "centralBranchKey" TEXT;
ALTER TABLE "BranchRuntimeConfig" ADD COLUMN "status" TEXT NOT NULL DEFAULT 'active';

CREATE TABLE IF NOT EXISTS "BranchAssignmentHistory" (
    "id" TEXT NOT NULL PRIMARY KEY,
    "terminalId" TEXT NOT NULL,
    "fromBranchId" TEXT,
    "toBranchId" TEXT NOT NULL,
    "changedBy" TEXT,
    "nodeRole" TEXT,
    "requestId" TEXT NOT NULL UNIQUE,
    "outcome" TEXT NOT NULL,
    "createdAt" DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS "idx_branch_assignment_terminal"
    ON "BranchAssignmentHistory" ("terminalId", "createdAt");
