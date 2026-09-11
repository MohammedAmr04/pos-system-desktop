param(
    [string]$Version = "v2",
    [string]$OutputName,
    [string]$SecretPath = "license.secret"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $OutputName) {
    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $OutputName = "POS-App-$Version-$timestamp.zip"
}

$outZip = Join-Path $root $OutputName
$srcDir = Join-Path $root "backend-cs\bin\Release\net48"
$toolsSrc = Join-Path $root "tools"
$tempDir = Join-Path $root "release-temp-$([Guid]::NewGuid().ToString("N"))"

function Ensure-Dir([string]$path) {
    if (-not (Test-Path -LiteralPath $path)) {
        New-Item -ItemType Directory -Path $path -Force | Out-Null
    }
}

# Build backend
Write-Host "[BUILD] Building backend (Release)..."
Push-Location $root
try {
    dotnet build (Join-Path $root "backend-cs\pos-cs.csproj") --configuration Release
    if ($LASTEXITCODE -ne 0) {
        throw "Backend build failed."
    }
}
finally {
    Pop-Location
}

# Build frontend static export and copy into backend wwwroot
Write-Host "[BUILD] Building frontend static export..."
Push-Location $root
try {
    & (Join-Path $root "build-static.ps1")
    if ($LASTEXITCODE -ne 0) {
        throw "Frontend build failed."
    }
}
finally {
    Pop-Location
}

if (-not (Test-Path -LiteralPath $srcDir)) {
    throw "Backend release directory not found: $srcDir"
}

# Prepare temp release directory
Ensure-Dir $tempDir
Write-Host "[PACK] Preparing release directory: $tempDir"

# Copy backend binaries and runtime files
$include = @(
    "*.exe",
    "*.exe.config",
    "*.dll",
    "runtimes",
    "Migrations",
    "wwwroot"
)
foreach ($pattern in $include) {
    $source = Join-Path $srcDir $pattern
    if (Test-Path -LiteralPath $source) {
        $dest = Join-Path $tempDir $pattern
        Copy-Item -LiteralPath $source -Destination $dest -Recurse -Force
    }
}

# Create empty data directory
Ensure-Dir (Join-Path $tempDir "data")

# Copy start.bat
$startBat = Join-Path $root "start.bat"
if (Test-Path -LiteralPath $startBat) {
    Copy-Item -LiteralPath $startBat -Destination (Join-Path $tempDir "start.bat") -Force
}

# Copy license tools
$toolsDest = Join-Path $tempDir "tools"
Ensure-Dir $toolsDest
$toolFiles = @(
    "get-license-code.ps1",
    "get-license-code.bat",
    "README-license.md",
    "generate-license-secret.ps1"
)
foreach ($file in $toolFiles) {
    $source = Join-Path $toolsSrc $file
    if (Test-Path -LiteralPath $source) {
        Copy-Item -LiteralPath $source -Destination (Join-Path $toolsDest $file) -Force
    }
}

# Generate or copy license secret
$destSecret = Join-Path $tempDir "license.secret"
if (Test-Path -LiteralPath $SecretPath) {
    Write-Host "[PACK] Using existing license secret: $SecretPath"
    Copy-Item -LiteralPath $SecretPath -Destination $destSecret -Force
} else {
    Write-Host "[PACK] Generating new license secret..."
    & (Join-Path $toolsSrc "generate-license-secret.ps1") -Path $destSecret | Out-Null
}

# Create archive
if (Test-Path -LiteralPath $outZip) {
    Remove-Item -LiteralPath $outZip -Force
}

Write-Host "[PACK] Creating archive: $outZip"
Compress-Archive -Path (Join-Path $tempDir "*") -DestinationPath $outZip -Force

# Cleanup
Remove-Item -LiteralPath $tempDir -Recurse -Force

Write-Host ""
Write-Host "==========================================" -ForegroundColor Green
Write-Host "  Release package created"
Write-Host "  $outZip"
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Distribute the .zip file only. Do not commit license.secret." -ForegroundColor Yellow
