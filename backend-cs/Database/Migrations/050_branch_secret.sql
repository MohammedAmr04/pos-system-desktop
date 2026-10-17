-- Migration 050: Per-branch sync secret.
-- The central node stores a PBKDF2 hash of each branch's key here. A branch agent presents the
-- plaintext key (X-Branch-Key) on central sync calls so the server can prove which branch is
-- submitting, instead of trusting a shared user token for every branch. Absent/blank means the
-- branch is not yet enrolled: central falls back to authenticated-user trust for backward
-- compatibility until a secret is set.
ALTER TABLE "Branch" ADD COLUMN "secretHash" TEXT;
