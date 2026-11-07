# Prepare a POS Version (PC Test / Production)

This runbook covers two paths:

1. **Build a portable PC package** (folder + ZIP) and test it on a Windows PC.
2. **Copy a prepared package to production** without rebuilding, while keeping
   the live `data\` folder and secrets.

The portable package runs `pos-server.exe` on port `3001` and serves the static
frontend from the same process. The database is `data\dev.db`.

---

## 1. Prerequisites (build machine only)

Install these once on the machine that builds the package:

```powershell
winget install --id OpenJS.NodeJS.LTS -e
winget install --id Microsoft.DotNet.SDK.8 -e
winget install --id Microsoft.DotNet.Framework.DeveloperPack_4.8 -e
```

If `winget` is unavailable, install manually:

- Node.js 20+ — https://nodejs.org/en/download
- .NET SDK 8+ — https://dotnet.microsoft.com/download
- .NET Framework 4.8 Developer Pack — https://dotnet.microsoft.com/download/dotnet-framework/net48

> The backend targets **.NET Framework 4.8** (`net48`). A plain .NET SDK is not
> enough: the **.NET Framework 4.8 targeting pack / Developer Pack** must be
> installed, otherwise `dotnet build` fails with `MSB3644` (reference assemblies
> not found).

Verify the toolchain:

```powershell
node --version
npm.cmd --version
dotnet --version
Test-Path "C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8"
```

Expected: Node and .NET print versions, and `Test-Path` prints `True`.

If PowerShell blocks `npm.ps1` with an execution-policy error, always use
`npm.cmd` or run:

```powershell
Set-ExecutionPolicy -Scope Process Bypass -Force
```

The build PC also needs internet access the first time, for `npm ci` and NuGet
package restore.

---

## 2. Build a PC test package (one command)

From the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\prepare-version.ps1 -Version client-version -Channel Test
```

What the script does:

1. Runs `npm ci` when `node_modules` is missing (or `-Force` is passed).
2. Runs `tsc --noEmit`, `npm run lint`, `npm test`.
3. Runs the backend tests.
4. Builds `pos-server.exe` and `pos-branch-agent.exe` in Release mode.
5. Runs `build-static.ps1` (Next.js static export → backend `wwwroot`).
6. Stages a portable folder under `release\`.
7. Creates a ZIP with the same content.
8. Copies/generates `license.secret` and creates an empty `data\` folder.

Output:

```text
release\POS-App-client-version-Test\
release\POS-App-client-version-Test-<timestamp>.zip
```

Useful variations:

```powershell
# Quick rebuild: skip dependency install and all tests
powershell -NoProfile -ExecutionPolicy Bypass -File .\prepare-version.ps1 -Version client-version -Channel Test -SkipInstall -SkipTests

# Force a clean dependency install and rebuild
powershell -NoProfile -ExecutionPolicy Bypass -File .\prepare-version.ps1 -Version client-version -Channel Test -Force

# Build the folder only, no ZIP
powershell -NoProfile -ExecutionPolicy Bypass -File .\prepare-version.ps1 -Version client-version -Channel Test -NoZip

# Use a specific license secret
powershell -NoProfile -ExecutionPolicy Bypass -File .\prepare-version.ps1 -Version client-version -Channel Test -SecretPath .\license.client-version.secret

# Choose a custom ZIP name
powershell -NoProfile -ExecutionPolicy Bypass -File .\prepare-version.ps1 -Version client-version -Channel Test -OutputName POS-client-version-test.zip
```

> `-Version` is only an artifact label. It is not the app's semantic version and
> does not change `package.json`.

> The portable build inlines `NEXT_PUBLIC_API_URL` at build time. The default is
> `__SAME_ORIGIN__`, so API calls use the same host and port that served the
> page. Override it with `-ApiBaseUrl` only when the API lives on another origin.

> The older `package-release.ps1` still works, but it only produces a ZIP and
> does not keep a staging folder for instant production copies. Use
> `prepare-version.ps1` for the workflow in this runbook.

### Package contents

```text
POS-App-client-version-Test\
  pos-server.exe
  pos-server.exe.config
  pos-branch-agent.exe
  pos-branch-agent.exe.config
  *.dll
  runtimes\
  Migrations\
  wwwroot\
  tools\
  data\                  (empty; created on first run)
  license.secret         (private; keep it out of source control and never share its value)
  .env.example
  branch-agent.env.example
  start.bat
```

`.env.example` is a template. Create `.env` next to `pos-server.exe` only if
Cloudinary backups are used; never overwrite an existing production `.env`.
`branch-agent.env.example` is the optional branch-agent override template. The
copy script preserves both real files.

---

## 3. Test the package on the PC client

Copy the ZIP (or the staged folder) to the target PC and extract it to a
writable folder:

```powershell
Expand-Archive -LiteralPath .\POS-App-client-version-Test-<timestamp>.zip -DestinationPath C:\POS -Force
cd C:\POS
.\start.bat
```

`start.bat` starts the server, starts the branch agent, waits, and opens:

```text
http://localhost:3001/ar
```

First login: `admin` / `1234`. Change the password immediately.

Check the server directly:

```powershell
Invoke-WebRequest -Uri http://localhost:3001/health -UseBasicParsing | Select-Object StatusCode, Content
```

Expected body: `{"status":"ok","timestamp":"..."}`.

Notes:

- Keep `license.secret` next to `pos-server.exe`. Activation codes depend on it.
- The database is created automatically at `data\dev.db`; migrations run on
  startup.
- If SmartScreen blocks the executables after copying from another machine:

  ```powershell
  Unblock-File .\pos-server.exe
  Unblock-File .\pos-branch-agent.exe
  ```

- Do not use a test package's `data\` folder in production. It may contain demo
  or test transactions.

---

## 4. Build a production package

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\prepare-version.ps1 -Version client-version -Channel Production -SecretPath .\license.secret
```

Output:

```text
release\POS-App-client-version-Production\
release\POS-App-client-version-Production-<timestamp>.zip
```

### License secret rule

- **Upgrading an existing installation:** pass `-SecretPath` pointing to the
  same `license.secret` that is already deployed. Never generate a new secret
  for an existing customer, or every previously issued unlock code becomes
  invalid.
- **First installation:** generate a secret once and keep a secure backup. The
  script generates one automatically when `-SecretPath` does not exist.

The secret must be identical for:

- `license.secret` next to `pos-server.exe`
- `tools\get-license-code.ps1` on the activation machine
- Any backup of the installation

---

## 5. Copy instantly to production (no rebuild)

After a package has been built, copy the staged folder to the production PC:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\copy-to-production.ps1 -Destination "D:\POS" -Version client-version -Channel Production
```

If the POS is currently running, either close `start.bat` first or let the
script stop the processes:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\copy-to-production.ps1 -Destination "D:\POS" -Version client-version -Channel Production -StopRunning
```

Use an explicit source folder when needed:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\copy-to-production.ps1 -SourceDir ".\release\POS-App-client-version-Production" -Destination "D:\POS"
```

If you only have the ZIP, extract it first and point `-SourceDir` at the extracted
folder:

```powershell
Expand-Archive -LiteralPath .\release\POS-App-client-version-Production-<timestamp>.zip -DestinationPath .\release\POS-App-client-version-Production -Force
powershell -NoProfile -ExecutionPolicy Bypass -File .\copy-to-production.ps1 -SourceDir ".\release\POS-App-client-version-Production" -Destination "D:\POS"
```

Network share destination:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\copy-to-production.ps1 -Destination "\\PC-NAME\POS" -Version client-version -Channel Production -StopRunning
```

What the copy script does:

1. Refuses to run while `pos-server` / `pos-branch-agent` are running, unless
   `-StopRunning` is passed.
2. Creates `data\` if it does not exist.
3. Backs up the live database to
   `data\backups\pre-update-<timestamp>.db`.
4. Copies application files with `robocopy`.
5. **Preserves** `data\`, `license.secret`, `.env`, and `branch-agent.env`.
6. Copies `license.secret` only when the destination does not already have one.
7. Leaves old hashed `_next` files in place (safe, but they can be cleaned
   manually during a maintenance window).

The script does **not** delete production data. The database is never copied
from the package to the destination.

### Manual copy (if you prefer commands)

```powershell
$src = ".\release\POS-App-client-version-Production"
$dst = "D:\POS"
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"

# 1. Stop the POS first (close start.bat) or force-stop it
taskkill /IM pos-server.exe /F
taskkill /IM pos-branch-agent.exe /F

# 2. Back up the live database
New-Item -ItemType Directory -Path "$dst\data\backups" -Force | Out-Null
Copy-Item -LiteralPath "$dst\data\dev.db" -Destination "$dst\data\backups\pre-update-$stamp.db" -Force

# 3. Copy application files; keep data and runtime config
robocopy $src $dst /E /XF license.secret .env branch-agent.env /XD data /R:1 /W:1

# 4. First install only: copy the secret if the destination has none
if (-not (Test-Path -LiteralPath "$dst\license.secret")) {
    Copy-Item -LiteralPath "$src\license.secret" -Destination "$dst\license.secret" -Force
}

# 5. Start
cd $dst
.\start.bat
```

`robocopy` exit codes `0`–`7` mean success; `8` or higher means failure.

---

## 6. Upgrade an existing installation

1. Ask users to close the POS (`start.bat`).
2. Build or select the package.
3. Run `copy-to-production.ps1` (or the manual copy block above).
4. Start `start.bat`.
5. Migrations apply automatically on startup.
6. Log in and verify products, invoices, and the active shift.

If you want a full-folder backup before upgrading:

```powershell
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
Copy-Item -LiteralPath "D:\POS\data" -Destination "D:\POS-data-backup-$stamp" -Recurse -Force
```

Keep the last known-good ZIP/folder. It is the fastest rollback.

---

## 7. Verify the installation

```powershell
Invoke-WebRequest -Uri http://localhost:3001/health -UseBasicParsing | Select-Object StatusCode, Content
Start-Process http://localhost:3001/ar
```

Checklist:

- `GET /health` returns `200` and `status: ok`.
- The Arabic UI opens at `/ar`.
- `admin` / `1234` works on a fresh database.
- `data\dev.db` exists and grows after a test sale.
- `data\logs\pos-*.log` has no migration or startup errors.
- Printing and the branch agent work if they are used on that PC.

---

## 8. Rollback

```powershell
# 1. Stop the POS
taskkill /IM pos-server.exe /F
taskkill /IM pos-branch-agent.exe /F

# 2. Restore the database backup created before the update
Copy-Item -LiteralPath "D:\POS\data\backups\pre-update-<timestamp>.db" -Destination "D:\POS\data\dev.db" -Force

# 3. Copy the previous known-good package back over D:\POS
robocopy ".\release\POS-App-<previous-version>-Production" "D:\POS" /E /XF license.secret .env branch-agent.env /XD data /R:1 /W:1

# 4. Start
cd D:\POS
.\start.bat
```

Do not delete `data\` or `license.secret` during a rollback.

---

## 9. Manual build commands (troubleshooting)

The `prepare-version.ps1` script automates all of this. Run these commands
individually only when diagnosing a failure.

```powershell
# 1. Frontend dependencies
npm.cmd ci

# 2. Frontend checks
.\node_modules\.bin\tsc.cmd --noEmit
npm.cmd run lint
npm.cmd test

# 3. Backend build
dotnet build .\backend-cs\pos-cs.csproj --configuration Release
dotnet build .\backend-cs\BranchAgent\BranchAgent.csproj --configuration Release
dotnet test .\backend-cs\Pos.Tests\Pos.Tests.csproj --configuration Release

# 4. Static frontend -> backend-cs\bin\Release\net48\wwwroot
$env:NEXT_PUBLIC_API_URL = "__SAME_ORIGIN__"
powershell -NoProfile -ExecutionPolicy Bypass -File .\build-static.ps1
Remove-Item Env:\NEXT_PUBLIC_API_URL -ErrorAction SilentlyContinue

# 5. Package the build (runs the build again, then stages + zips)
powershell -NoProfile -ExecutionPolicy Bypass -File .\prepare-version.ps1 -Version client-version -Channel Test -SkipInstall
```

If step 5's rebuild is not wanted, stage from the existing output instead:

```powershell
$src = ".\backend-cs\bin\Release\net48"
$stage = ".\release\POS-App-client-version-Test"

if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force }
New-Item -ItemType Directory -Path $stage -Force | Out-Null

Copy-Item -Path "$src\*.exe" -Destination $stage -Force
Copy-Item -Path "$src\*.exe.config" -Destination $stage -Force
Copy-Item -Path "$src\*.dll" -Destination $stage -Force
Copy-Item -Path "$src\runtimes" -Destination $stage -Recurse -Force
Copy-Item -Path "$src\Migrations" -Destination $stage -Recurse -Force
Copy-Item -Path "$src\wwwroot" -Destination $stage -Recurse -Force
Copy-Item -LiteralPath ".\backend-cs\BranchAgent\bin\Release\net48\pos-branch-agent.exe" -Destination $stage -Force
$agentConfig = ".\backend-cs\BranchAgent\bin\Release\net48\pos-branch-agent.exe.config"
if (Test-Path -LiteralPath $agentConfig) {
    Copy-Item -LiteralPath $agentConfig -Destination $stage -Force
}
Copy-Item -LiteralPath ".\start.bat" -Destination $stage -Force
Copy-Item -LiteralPath ".\.env.example" -Destination $stage -Force
Copy-Item -LiteralPath ".\branch-agent.env.example" -Destination $stage -Force
Copy-Item -LiteralPath ".\tools" -Destination $stage -Recurse -Force
Copy-Item -LiteralPath ".\license.secret" -Destination "$stage\license.secret" -Force
New-Item -ItemType Directory -Path "$stage\data" -Force | Out-Null

Compress-Archive -Path "$stage\*" -DestinationPath ".\release\POS-App-client-version-Test.zip" -Force
```

---

## 10. Central IIS production (optional)

The portable PC package is separate from the central IIS/MonsterASP deployment.
For the central site:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File deploy\iis\package.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File deploy\iis\smoke-test.ps1 -Port 5011
powershell -NoProfile -ExecutionPolicy Bypass -File deploy\iis\make-site-zip.ps1
```

Upload `deploy\iis\pos-site.zip` to the site root and extract it there. Keep the
live `data\` folder and `.env`.

To include a pre-migrated empty database:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File deploy\iis\prepare-db.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File deploy\iis\make-site-zip.ps1
```

See `docs/CENTRAL-DEPLOYMENT.md` for the branch-key upgrade procedure.

---

## 11. Versioning and release notes

1. Update `CHANGELOG.md` with the new version and changes.
2. Add `docs/release/Version <version>.md` using the existing release-note
   format.
3. Build the package with the matching label:

   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File .\prepare-version.ps1 -Version client-version -Channel Test
   ```

4. Commit and tag if you use Git:

   ```powershell
   git status
   git add CHANGELOG.md docs/release PREPARE-VERSION.md prepare-version.ps1 copy-to-production.ps1
   git commit -m "release: client-version"
   git tag client-version
   ```

`package.json` currently stays at `0.1.0`; it is not used by the packaging
script. The artifact label comes from `-Version`.

---

## 12. Troubleshooting

| Symptom | Fix |
| --- | --- |
| `dotnet` is not recognized | Install the .NET SDK and reopen the terminal. If it is installed per-user, add `%LOCALAPPDATA%\Microsoft\dotnet` to `PATH`. |
| `MSB3644` / reference assemblies for `.NETFramework,Version=v4.8` not found | Install the .NET Framework 4.8 Developer Pack or Visual Studio Build Tools with the .NET desktop build workload. |
| `npm.ps1 cannot be loaded because running scripts is disabled` | Use `npm.cmd` instead of `npm`, or run `Set-ExecutionPolicy -Scope Process Bypass -Force`. |
| `Next.js CLI not found` | Run `npm.cmd ci` in the repository root. |
| `npm ci` fails behind a proxy | Configure the npm proxy or use an existing `node_modules` with `-SkipInstall`. |
| Port 3001 is already in use | `netstat -ano | findstr :3001`, then `taskkill /PID <pid> /F`. |
| `robocopy` returns 8 or higher | Stop the POS first, check folder permissions, and retry. Exit codes 0–7 are success. |
| The app starts but the browser shows a blank page | Confirm `wwwroot\ar\index.html` exists and that `start.bat` is run from the package root. |
| Login works on a fresh DB but not after upgrade | The old database was replaced. Restore `data\dev.db` from the backup and copy again without `data\`. |
| Activation code is rejected | The same `license.secret` must be next to `pos-server.exe` and used by `tools\get-license-code.ps1`. Regenerate the code after restoring the correct secret. |
| `start.bat` closes immediately | Run it from `cmd.exe` to see the error, and check `data\logs\pos-*.log`. |
| `Could not load file or assembly` | Copy the whole folder/ZIP, not only the `.exe`; runtime DLLs and `runtimes\` are required. |

---

## 13. Command cheat sheet

| Task | Command |
| --- | --- |
| Install prerequisites | `winget install --id OpenJS.NodeJS.LTS -e` / `winget install --id Microsoft.DotNet.SDK.8 -e` / `winget install --id Microsoft.DotNet.Framework.DeveloperPack_4.8 -e` |
| Build PC test package | `powershell -NoProfile -ExecutionPolicy Bypass -File .\prepare-version.ps1 -Version client-version -Channel Test` |
| Quick rebuild | `powershell -NoProfile -ExecutionPolicy Bypass -File .\prepare-version.ps1 -Version client-version -Channel Test -SkipInstall -SkipTests` |
| Build production package | `powershell -NoProfile -ExecutionPolicy Bypass -File .\prepare-version.ps1 -Version client-version -Channel Production -SecretPath .\license.secret` |
| Copy to production | `powershell -NoProfile -ExecutionPolicy Bypass -File .\copy-to-production.ps1 -Destination "D:\POS" -Version client-version -Channel Production` |
| Copy while running | `... -StopRunning` |
| Manual dependency install | `npm.cmd ci` |
| Frontend checks | `.\node_modules\.bin\tsc.cmd --noEmit` / `npm.cmd run lint` / `npm.cmd test` |
| Backend build | `dotnet build .\backend-cs\pos-cs.csproj --configuration Release` |
| Branch agent build | `dotnet build .\backend-cs\BranchAgent\BranchAgent.csproj --configuration Release` |
| Backend tests | `dotnet test .\backend-cs\Pos.Tests\Pos.Tests.csproj --configuration Release` |
| Static frontend | `powershell -NoProfile -ExecutionPolicy Bypass -File .\build-static.ps1` |
| Health check | `Invoke-WebRequest http://localhost:3001/health -UseBasicParsing` |
| IIS central package | `powershell -NoProfile -ExecutionPolicy Bypass -File deploy\iis\package.ps1` |
| IIS smoke test | `powershell -NoProfile -ExecutionPolicy Bypass -File deploy\iis\smoke-test.ps1 -Port 5011` |
| IIS site ZIP | `powershell -NoProfile -ExecutionPolicy Bypass -File deploy\iis\make-site-zip.ps1` |
