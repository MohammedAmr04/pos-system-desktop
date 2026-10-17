param(
    [string]$Configuration = "Release",
    [switch]$SkipFrontend,
    [switch]$SkipBackend
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repo = (Resolve-Path (Join-Path $scriptDir "..\..")).Path
$payload = Join-Path $scriptDir "payload"

if (Test-Path -LiteralPath $payload) {
    Remove-Item -LiteralPath $payload -Recurse -Force
}
New-Item -ItemType Directory -Path $payload -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $payload "bin") -Force | Out-Null

if (-not $SkipFrontend) {
    Write-Host "Building frontend with same-origin API base..."
    Push-Location $repo
    try {
        $env:NEXT_PUBLIC_API_URL = "__SAME_ORIGIN__"
        $nextCli = Join-Path $repo "node_modules\.bin\next.cmd"
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
        Remove-Item Env:\NEXT_PUBLIC_API_URL -ErrorAction SilentlyContinue
    }
}

$repoOut = Join-Path $repo "out"
if (-not (Test-Path -LiteralPath $repoOut)) {
    throw "Static export directory '$repoOut' not found."
}
Write-Host "Copying out/ -> payload\ (site root)..."
Copy-Item -Path (Join-Path $repoOut "*") -Destination $payload -Recurse -Force

if (-not $SkipBackend) {
    Write-Host "Building backend (library) for IIS..."
    $binDir = Join-Path $payload "bin"
    & dotnet build (Join-Path $repo "backend-cs\pos-cs.csproj") `
        --configuration $Configuration `
        -p:OutputType=Library `
        -p:OutputPath="$binDir\" `
        -p:AppendTargetFrameworkToOutputPath=false `
        -p:AppendRuntimeIdentifierToOutputPath=false
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed with exit code $LASTEXITCODE"
    }
}

$binMigrations = Join-Path $payload "bin\Migrations"
if (Test-Path -LiteralPath $binMigrations) {
    Remove-Item -LiteralPath $binMigrations -Recurse -Force
}

Write-Host "Copying migrations -> payload\Migrations..."
$payloadMigrations = Join-Path $payload "Migrations"
New-Item -ItemType Directory -Path $payloadMigrations -Force | Out-Null
Copy-Item -Path (Join-Path $repo "backend-cs\Database\Migrations\*.sql") -Destination $payloadMigrations -Force

Write-Host "Copying Web.config -> payload\Web.config..."
Copy-Item -Path (Join-Path $scriptDir "Web.config") -Destination $payload -Force

$nugetLib = Join-Path $env:USERPROFILE ".nuget\packages\sqlitepclraw.lib.e_sqlite3\2.1.13\runtimes"
$x86Native = Join-Path $nugetLib "win-x86\native\e_sqlite3.dll"
if (-not (Test-Path -LiteralPath $x86Native)) {
    throw "x86 SQLite native library not found at '$x86Native'. Run dotnet restore first."
}
$x86Target = Join-Path $payload "bin\runtimes\win-x86\native"
New-Item -ItemType Directory -Path $x86Target -Force | Out-Null
Copy-Item -Path $x86Native -Destination $x86Target -Force

$required = @(
    (Join-Path $payload "Web.config"),
    (Join-Path $payload "bin\pos-server.dll"),
    (Join-Path $payload "bin\e_sqlite3.dll"),
    (Join-Path $payload "bin\runtimes\win-x64\native\e_sqlite3.dll"),
    (Join-Path $payload "bin\runtimes\win-x86\native\e_sqlite3.dll"),
    (Join-Path $payload "ar\index.html"),
    (Join-Path $payload "_next"),
    (Join-Path $payload "Migrations\001_init.sql")
)
if (Test-Path -LiteralPath (Join-Path $payload "wwwroot")) {
    throw "Payload must not contain a wwwroot folder (static export lives at site root)."
}
foreach ($file in $required) {
    if (-not (Test-Path -LiteralPath $file)) {
        throw "Payload is incomplete: missing '$file'"
    }
}

$chunk = Select-String -Path (Join-Path $payload "_next\static\chunks\*.js") -Pattern "localhost:3001" -SimpleMatch -List -ErrorAction SilentlyContinue
if ($chunk) {
    throw "Same-origin build failed: 'localhost:3001' still present in $($chunk.Path)"
}

Write-Host ""
Write-Host "Payload ready: $payload"
Write-Host "Upload its CONTENTS to the site root (wwwroot) of your MonsterASP account."
