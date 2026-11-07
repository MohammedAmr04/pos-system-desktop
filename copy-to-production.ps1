[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Destination,
    [string]$SourceDir,
    [string]$Version = "client-version",
    [ValidateSet("Test", "Production")]
    [string]$Channel = "Production",
    [string]$ReleaseRoot = "release",
    [switch]$StopRunning,
    [switch]$NoBackup
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path

function Write-Step([string]$Text) {
    Write-Host ""
    Write-Host "==> $Text" -ForegroundColor Cyan
}

$safeVersion = ($Version -replace '[^A-Za-z0-9._-]', '-').Trim('-')
if (-not $safeVersion) {
    throw "Version must contain at least one letter or digit."
}

$releaseRootPath = if ([System.IO.Path]::IsPathRooted($ReleaseRoot)) {
    $ReleaseRoot
}
else {
    Join-Path $root $ReleaseRoot
}

if (-not $SourceDir) {
    $SourceDir = Join-Path $releaseRootPath "POS-App-$safeVersion-$Channel"
}
elseif (-not [System.IO.Path]::IsPathRooted($SourceDir)) {
    $SourceDir = Join-Path $root $SourceDir
}

if (-not (Test-Path -LiteralPath $SourceDir)) {
    throw "Prepared package not found: $SourceDir. Run prepare-version.ps1 first."
}

foreach ($item in @("pos-server.exe", "start.bat", "wwwroot", "Migrations")) {
    if (-not (Test-Path -LiteralPath (Join-Path $SourceDir $item))) {
        throw "Prepared package is incomplete: missing $item under $SourceDir"
    }
}

if (-not [System.IO.Path]::IsPathRooted($Destination)) {
    $Destination = Join-Path $root $Destination
}
$Destination = [System.IO.Path]::GetFullPath($Destination)
$SourceFull = [System.IO.Path]::GetFullPath($SourceDir)

if ($SourceFull.TrimEnd('\') -eq $Destination.TrimEnd('\')) {
    throw "Source and destination are the same folder: $Destination"
}

$running = @(Get-Process -Name "pos-server", "pos-branch-agent" -ErrorAction SilentlyContinue)
if ($running.Count -gt 0) {
    if ($StopRunning) {
        Write-Step "Stopping running POS processes"
        & taskkill.exe /IM pos-server.exe /F 2>$null | Out-Null
        & taskkill.exe /IM pos-branch-agent.exe /F 2>$null | Out-Null
        Start-Sleep -Seconds 2
    }
    else {
        throw "POS is running (pos-server / pos-branch-agent). Close start.bat first or pass -StopRunning."
    }
}

Write-Host ""
Write-Host "Copying prepared package to production" -ForegroundColor Green
Write-Host "  Source      : $SourceFull"
Write-Host "  Destination : $Destination"
Write-Host "  Version     : $safeVersion ($Channel)"
Write-Host ""

New-Item -ItemType Directory -Path $Destination -Force | Out-Null
$dataDir = Join-Path $Destination "data"
New-Item -ItemType Directory -Path $dataDir -Force | Out-Null

if (-not $NoBackup) {
    $database = Join-Path $dataDir "dev.db"
    if (Test-Path -LiteralPath $database) {
        $backups = Join-Path $dataDir "backups"
        New-Item -ItemType Directory -Path $backups -Force | Out-Null
        $backup = Join-Path $backups ("pre-update-" + (Get-Date -Format "yyyyMMdd-HHmmss") + ".db")
        Copy-Item -LiteralPath $database -Destination $backup -Force
        Write-Host "    Database backup: $backup"
    }
}

$destinationSecret = Join-Path $Destination "license.secret"
$hadSecret = Test-Path -LiteralPath $destinationSecret

Write-Step "Copying application files"
$robocopyArgs = @(
    $SourceFull,
    $Destination,
    "/E",
    "/XF", "license.secret", ".env", "branch-agent.env",
    "/XD", "data",
    "/R:1",
    "/W:1",
    "/NFL", "/NDL", "/NJH", "/NJS", "/NP"
)
& robocopy.exe @robocopyArgs
$robocopyCode = $LASTEXITCODE
Write-Host "    robocopy exit code: $robocopyCode"
if ($robocopyCode -ge 8) {
    throw "robocopy failed with exit code $robocopyCode."
}

if (-not $hadSecret) {
    $sourceSecret = Join-Path $SourceFull "license.secret"
    if (Test-Path -LiteralPath $sourceSecret) {
        Copy-Item -LiteralPath $sourceSecret -Destination $destinationSecret -Force
        Write-Host "    Copied license.secret because the destination had none."
    }
    else {
        Write-Warning "No license.secret in the prepared package. Activate the installation before use."
    }
}

Write-Host ""
Write-Host "==========================================" -ForegroundColor Green
Write-Host "  Production copy complete"
Write-Host "  Destination: $Destination"
Write-Host "  Preserved  : data\, license.secret, .env, branch-agent.env"
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Next:"
Write-Host "  1. Open $Destination"
Write-Host "  2. Run start.bat"
Write-Host "  3. Check http://localhost:3001/health"
Write-Host "  4. Open http://localhost:3001/ar and log in."
Write-Host ""
Write-Host "Migrations run automatically on startup. If you need to roll back,"
Write-Host "restore the database backup printed above and copy the previous package back."
Write-Host ""
