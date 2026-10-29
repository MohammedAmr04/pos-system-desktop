# Central deployment (MonsterASP / IIS)

`deploy/iis/pos-site.zip` is a complete site root update: `Web.config`, `bin`, `Migrations`, `ar`,
and `_next`. It does not contain `data/` or `.env`, so uploading its contents over an existing site
keeps the live database and server settings.

## Update an existing central site

1. Back up `wwwroot/data/dev.db` (and `wwwroot/.env` if present).
2. Upload `deploy/iis/pos-site.zip` to the site root and extract it, overwriting application files.
   Do not delete or replace the live `data/` folder.
3. Restart the site/application pool. Startup applies `054_branch_key_only.sql`, which clears the
   legacy `centralToken`.
4. Log in as an admin and open Settings → Branches.
   - Existing branches show the sync key as not configured.
   - Open each branch → set secret → generate a key, then copy the setup data/code.
5. On each branch, upgrade to `POS-App-v9-branch-key.zip`, open the first-run setup screen, paste
   the setup code, and finish setup. `start.bat` starts `pos-branch-agent.exe` automatically.
6. Confirm central → branch propagation: edit a category/product centrally and verify it appears on
   the branch after one poll interval (default 5 seconds).

## Verification

- `GET /api/branch/setup-status` returns `{"setupComplete":true,"nodeRole":"central"}`.
- `GET /api/branch/list` includes `hasSyncKey` for every branch.
- `GET /api/branch-sync/changes` with `X-Branch-Id` and `X-Branch-Key` returns a change list; a wrong
  key returns 401.
- `POST /api/branch-sync/receive` with a valid branch key acknowledges operations; an operation that
  claims another branch returns 400.

## HTTPS requirement

The branch key is long-lived and is sent in the `X-Branch-Key` header. The central site must be served
over HTTPS before production use. `https://pos-server.runasp.net/health` currently resets, so the live
site is HTTP-only; treat keys created against it as exposed and rotate them after HTTPS is enabled.

## Local validation

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File deploy/iis/package.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File deploy/iis/smoke-test.ps1 -Port 5011
powershell -NoProfile -ExecutionPolicy Bypass -File deploy/iis/make-site-zip.ps1
```
