[CmdletBinding()]
param(
    [string]$Version = "client-version",
    [ValidateSet("Test", "Production")]
    [string]$Channel = "Test",
    [string]$SecretPath = "license.secret",
    [string]$ReleaseRoot = "release",
    [string]$OutputName,
    [string]$ApiBaseUrl = "__SAME_ORIGIN__",
    [switch]$SkipInstall,
    [switch]$SkipTests,
    [switch]$NoZip,
    [switch]$Force
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path

function Write-Step([string]$Text) {
    Write-Host ""
    Write-Host "==> $Text" -ForegroundColor Cyan
}

function Resolve-CommandPath([string]$Name, [string[]]$Fallbacks) {
    foreach ($candidate in $Fallbacks) {
        if ($candidate -and (Test-Path -LiteralPath $candidate)) {
            return $candidate
        }
    }
    $command = Get-Command $Name -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }
    return $null
}

function Invoke-Checked([string]$FilePath, [string[]]$Arguments, [string]$Label) {
    Write-Host "    $Label"
    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Label failed with exit code $LASTEXITCODE."
    }
}

function Copy-Required([string]$Source, [string]$Destination, [string]$Label) {
    if (-not (Test-Path -LiteralPath $Source)) {
        throw "Missing ${Label}: $Source"
    }
    Copy-Item -LiteralPath $Source -Destination $Destination -Force
}

$safeVersion = ($Version -replace '[^A-Za-z0-9._-]', '-').Trim('-')
if (-not $safeVersion) {
    throw "Version must contain at least one letter or digit."
}

$stageName = "POS-App-$safeVersion-$Channel"
$releaseRootPath = if ([System.IO.Path]::IsPathRooted($ReleaseRoot)) {
    $ReleaseRoot
}
else {
    Join-Path $root $ReleaseRoot
}
$stageDir = Join-Path $releaseRootPath $stageName
$backendOut = Join-Path $root "backend-cs\bin\Release\net48"
$agentOut = Join-Path $root "backend-cs\BranchAgent\bin\Release\net48"

Write-Host ""
Write-Host "POS portable package builder" -ForegroundColor Green
Write-Host "  Version : $safeVersion"
Write-Host "  Channel : $Channel"
Write-Host "  Root    : $root"
Write-Host ""

$dotnet = Resolve-CommandPath "dotnet" @(
    (Join-Path $env:ProgramFiles "dotnet\dotnet.exe"),
    (Join-Path ${env:ProgramFiles(x86)} "dotnet\dotnet.exe"),
    (Join-Path $env:LOCALAPPDATA "Microsoft\dotnet\dotnet.exe")
)
if (-not $dotnet) {
    throw "dotnet was not found. Install the .NET SDK 8+ and the .NET Framework 4.8 targeting pack, then reopen the terminal."
}

$npm = Resolve-CommandPath "npm.cmd" @(
    (Join-Path $env:ProgramFiles "nodejs\npm.cmd")
)
if (-not $npm) {
    throw "npm was not found. Install Node.js 20+ and reopen the terminal."
}

$nodeModules = Join-Path $root "node_modules"
$nextCli = Join-Path $root "node_modules\.bin\next.cmd"
$tscCli = Join-Path $root "node_modules\.bin\tsc.cmd"

if (-not $SkipInstall) {
    if ($Force -or -not (Test-Path -LiteralPath $nodeModules)) {
        Write-Step "Installing frontend dependencies"
        Invoke-Checked $npm @("ci") "npm ci"
    }
    else {
        Write-Host "    node_modules already exists; skipping install (use -Force to reinstall)."
    }
}

if (-not (Test-Path -LiteralPath $nextCli)) {
    throw "Next.js CLI not found at $nextCli. Run npm.cmd ci first (or omit -SkipInstall)."
}

if (-not $SkipTests) {
    Write-Step "Verifying frontend"
    if (-not (Test-Path -LiteralPath $tscCli)) {
        throw "TypeScript CLI not found at $tscCli. Run npm.cmd ci first."
    }
    Invoke-Checked $tscCli @("--noEmit") "tsc --noEmit"
    Invoke-Checked $npm @("run", "lint") "npm run lint"
    Invoke-Checked $npm @("test") "npm test"

    Write-Step "Running backend tests"
    Invoke-Checked $dotnet @(
        "test",
        (Join-Path $root "backend-cs\Pos.Tests\Pos.Tests.csproj"),
        "--configuration", "Release"
    ) "dotnet test"
}

Write-Step "Building backend (Release)"
Invoke-Checked $dotnet @(
    "build",
    (Join-Path $root "backend-cs\pos-cs.csproj"),
    "--configuration", "Release"
) "dotnet build pos-cs"

Write-Step "Building branch agent (Release)"
Invoke-Checked $dotnet @(
    "build",
    (Join-Path $root "backend-cs\BranchAgent\BranchAgent.csproj"),
    "--configuration", "Release"
) "dotnet build BranchAgent"

Write-Step "Building static frontend"
$previousApiBase = $env:NEXT_PUBLIC_API_URL
$env:NEXT_PUBLIC_API_URL = $ApiBaseUrl
try {
    & (Join-Path $root "build-static.ps1")
}
finally {
    if ($null -eq $previousApiBase) {
        Remove-Item Env:\NEXT_PUBLIC_API_URL -ErrorAction SilentlyContinue
    }
    else {
        $env:NEXT_PUBLIC_API_URL = $previousApiBase
    }
}

if (-not (Test-Path -LiteralPath $backendOut)) {
    throw "Backend output not found: $backendOut"
}
if (-not (Test-Path -LiteralPath $agentOut)) {
    throw "Branch agent output not found: $agentOut"
}

Write-Step "Staging portable package"
$secretSource = if ([System.IO.Path]::IsPathRooted($SecretPath)) {
    $SecretPath
}
else {
    Join-Path $root $SecretPath
}
$secretValue = $null
if (Test-Path -LiteralPath $secretSource) {
    $secretValue = Get-Content -LiteralPath $secretSource -Raw
}

if (Test-Path -LiteralPath $stageDir) {
    Write-Host "    Removing previous staging folder: $stageDir"
    Remove-Item -LiteralPath $stageDir -Recurse -Force
}
New-Item -ItemType Directory -Path $stageDir -Force | Out-Null

$backendItems = @("*.exe", "*.exe.config", "*.dll", "runtimes", "Migrations", "wwwroot")
foreach ($item in $backendItems) {
    $source = Join-Path $backendOut $item
    if (-not (Test-Path -Path $source)) {
        continue
    }
    if ($item -match '[*?]') {
        Copy-Item -Path $source -Destination $stageDir -Force
    }
    else {
        Copy-Item -Path $source -Destination $stageDir -Recurse -Force
    }
}

$envExample = Join-Path $root ".env.example"
if (Test-Path -LiteralPath $envExample) {
    Copy-Item -LiteralPath $envExample -Destination (Join-Path $stageDir ".env.example") -Force
}

Copy-Required (Join-Path $root "start.bat") (Join-Path $stageDir "start.bat") "start.bat"

Copy-Required (Join-Path $agentOut "pos-branch-agent.exe") (Join-Path $stageDir "pos-branch-agent.exe") "branch agent executable"
$agentConfig = Join-Path $agentOut "pos-branch-agent.exe.config"
if (Test-Path -LiteralPath $agentConfig) {
    Copy-Item -LiteralPath $agentConfig -Destination (Join-Path $stageDir "pos-branch-agent.exe.config") -Force
}

$agentEnvExample = Join-Path $root "branch-agent.env.example"
if (Test-Path -LiteralPath $agentEnvExample) {
    Copy-Item -LiteralPath $agentEnvExample -Destination (Join-Path $stageDir "branch-agent.env.example") -Force
}

$toolsDest = Join-Path $stageDir "tools"
New-Item -ItemType Directory -Path $toolsDest -Force | Out-Null
$toolFiles = @(
    "get-license-code.ps1",
    "get-license-code.bat",
    "README-license.md",
    "generate-license-secret.ps1",
    "seed-demo-data.ps1"
)
foreach ($file in $toolFiles) {
    $source = Join-Path $root "tools\$file"
    if (Test-Path -LiteralPath $source) {
        Copy-Item -LiteralPath $source -Destination (Join-Path $toolsDest $file) -Force
    }
}

$secretDest = Join-Path $stageDir "license.secret"
if ($null -ne $secretValue) {
    Set-Content -LiteralPath $secretDest -Value $secretValue -NoNewline
    Write-Host "    Using license secret: $secretSource"
}
else {
    Write-Host "    No secret found at $secretSource; generating a new one."
    & (Join-Path $root "tools\generate-license-secret.ps1") -Path $secretDest
}

New-Item -ItemType Directory -Path (Join-Path $stageDir "data") -Force | Out-Null

$zipPath = $null
if (-not $NoZip) {
    Write-Step "Creating ZIP"
    if (-not $OutputName) {
        $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
        $OutputName = "$stageName-$timestamp.zip"
    }
    $zipPath = if ([System.IO.Path]::IsPathRooted($OutputName)) {
        $OutputName
    }
    else {
        Join-Path $releaseRootPath $OutputName
    }
    if (Test-Path -LiteralPath $zipPath) {
        Remove-Item -LiteralPath $zipPath -Force
    }
    $zipParent = Split-Path -Parent $zipPath
    if ($zipParent -and -not (Test-Path -LiteralPath $zipParent)) {
        New-Item -ItemType Directory -Path $zipParent -Force | Out-Null
    }
    Compress-Archive -Path (Join-Path $stageDir "*") -DestinationPath $zipPath -Force
}

Write-Host ""
Write-Host "==========================================" -ForegroundColor Green
Write-Host "  Portable package ready"
Write-Host "  Version : $safeVersion ($Channel)"
Write-Host "  Folder  : $stageDir"
if ($zipPath) {
    Write-Host "  ZIP     : $zipPath"
}
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Test on a PC:"
Write-Host "  1. Copy the ZIP (or the folder) to the target PC."
Write-Host "  2. Extract it to a writable folder, for example C:\POS."
Write-Host "  3. Run start.bat and open http://localhost:3001/ar"
Write-Host "  4. Log in with admin / 1234 and change the password."
Write-Host ""
Write-Host "Copy the prepared folder to production without rebuilding:"
Write-Host "  powershell -NoProfile -ExecutionPolicy Bypass -File .\copy-to-production.ps1 -Destination D:\POS -Version $safeVersion -Channel $Channel"
Write-Host ""
