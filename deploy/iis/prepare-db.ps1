param(
    [string]$Configuration = "Release",
    [int]$Port = 3099
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repo = (Resolve-Path (Join-Path $scriptDir "..\..")).Path
$payload = Join-Path $scriptDir "payload"
$dbPath = Join-Path $payload "data\dev.db"
$exeDir = Join-Path $repo "backend-cs\bin\$Configuration\net48"
$exe = Join-Path $exeDir "pos-server.exe"

if (-not (Test-Path -LiteralPath $payload)) {
    throw "Payload not found at '$payload'. Run package.ps1 first."
}
if (-not (Test-Path -LiteralPath $exe)) {
    throw "Server executable not found at '$exe'. Run: dotnet build backend-cs/pos-cs.csproj -c $Configuration"
}

$dataDir = Split-Path -Parent $dbPath
if (Test-Path -LiteralPath $dataDir) {
    Remove-Item -LiteralPath $dataDir -Recurse -Force
}
New-Item -ItemType Directory -Path $dataDir -Force | Out-Null

$logFile = Join-Path $env:TEMP "pos-prepare-db.log"
Remove-Item -LiteralPath $logFile -Force -ErrorAction SilentlyContinue

$env:POS_DB_PATH = $dbPath
$env:POS_LOG_DIR = ""
$env:API_PORT = "$Port"
$env:POS_BACKUP_WORKER = "0"

Write-Host "Creating database at $dbPath ..."
$proc = $null
try {
    $proc = Start-Process -FilePath $exe -WorkingDirectory $exeDir -PassThru -WindowStyle Hidden `
        -RedirectStandardOutput $logFile

    $healthy = $false
    foreach ($i in 1..60) {
        Start-Sleep -Milliseconds 500
        if ($proc.HasExited) { break }
        try {
            $r = Invoke-WebRequest -Uri "http://localhost:$Port/health" -UseBasicParsing -TimeoutSec 3
            if ($r.StatusCode -eq 200) { $healthy = $true; break }
        }
        catch { }
    }

    if (-not $healthy) {
        $detail = ""
        if (Test-Path -LiteralPath $logFile) {
            $detail = "`n--- server output ---`n" + ((Get-Content -LiteralPath $logFile) -join "`n")
        }
        throw "Server did not become healthy on port $Port.$detail"
    }
}
finally {
    if ($proc -and -not $proc.HasExited) {
        Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
    }
    foreach ($name in @("POS_DB_PATH", "POS_LOG_DIR", "API_PORT", "POS_BACKUP_WORKER")) {
        Remove-Item "Env:\$name" -ErrorAction SilentlyContinue
    }
}

$applied = @()
$skipped = 0
if (Test-Path -LiteralPath $logFile) {
    $applied = @(Get-Content -LiteralPath $logFile | Select-String -Pattern "\[MIGRATIONS\] Applied:")
    $skipped = @(Get-Content -LiteralPath $logFile | Select-String -Pattern "\[MIGRATIONS\] Skipping already applied:").Count
}

$db = Get-Item -LiteralPath $dbPath
Write-Host ""
Write-Host ("Database ready: {0} ({1:N0} KB)" -f $dbPath, ($db.Length / 1KB))
Write-Host ("Migrations applied: {0}, already present: {1}" -f $applied.Count, $skipped)
Write-Host ""
Write-Host "This database contains the seeded admin (admin / 1234) and no business data."
Write-Host "NOTE: the site folder must still be writable at runtime - SQLite writes on every sale."
Write-Host ""
Write-Host "Next: create the upload zip with the database included:"
Write-Host "  powershell -ExecutionPolicy Bypass -File deploy\iis\make-site-zip.ps1"
