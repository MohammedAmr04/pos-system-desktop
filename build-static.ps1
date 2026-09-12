$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$outDir = Join-Path $root "out"
$destDir = Join-Path $root "backend-cs\bin\Release\net48\wwwroot"

Write-Host "Building static export (out/)..."
Push-Location $root
try {
    $nextCli = Join-Path $root "node_modules\.bin\next.cmd"
    if (-not (Test-Path -LiteralPath $nextCli)) {
        throw "Local Next.js CLI not found. Run npm install before building."
    }
    & $nextCli build --webpack
    if ($LASTEXITCODE -ne 0) {
        throw "next build failed with exit code $LASTEXITCODE"
    }
}
finally {
    Pop-Location
}

if (-not (Test-Path -LiteralPath $outDir)) {
    throw "Export directory '$outDir' not found."
}

if (-not (Test-Path -LiteralPath $destDir)) {
    New-Item -ItemType Directory -Path $destDir -Force | Out-Null
}

$logo = Join-Path $destDir "logo.jpeg"
$preserveLogo = Test-Path -LiteralPath $logo

Write-Host "Clearing previous frontend build from $destDir (preserving logo.jpeg = $preserveLogo)..."
Get-ChildItem -LiteralPath $destDir -Force |
    Where-Object { $_.Name -ne "logo.jpeg" } |
    Remove-Item -Recurse -Force

Write-Host "Copying $outDir -> $destDir ..."
Copy-Item -Path (Join-Path $outDir "*") -Destination $destDir -Recurse -Force

Write-Host "Static files copied to $destDir"
