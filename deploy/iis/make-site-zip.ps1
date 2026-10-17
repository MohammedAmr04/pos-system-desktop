param(
    [string]$OutFile
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$payload = Join-Path $scriptDir "payload"

if (-not (Test-Path -LiteralPath $payload)) {
    throw "Payload not found at '$payload'. Run package.ps1 first."
}
if (-not $OutFile) {
    $OutFile = Join-Path $scriptDir "pos-site.zip"
}

$required = @("Web.config", "bin\pos-server.dll", "ar\index.html", "_next", "Migrations\001_init.sql")
foreach ($rel in $required) {
    if (-not (Test-Path -LiteralPath (Join-Path $payload $rel))) {
        throw "Payload is incomplete: missing '$rel'. Run package.ps1 first."
    }
}

if (Test-Path -LiteralPath $OutFile) {
    Remove-Item -LiteralPath $OutFile -Force
}

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$root = (Resolve-Path -LiteralPath $payload).Path.TrimEnd('\') + '\'
$fs = [System.IO.File]::Open($OutFile, [System.IO.FileMode]::CreateNew)
$zip = New-Object System.IO.Compression.ZipArchive($fs, [System.IO.Compression.ZipArchiveMode]::Create)
$count = 0
try {
    foreach ($file in Get-ChildItem -LiteralPath $payload -Recurse -File) {
        $relative = $file.FullName.Substring($root.Length).Replace('\', '/')
        $entry = $zip.CreateEntry($relative, [System.IO.Compression.CompressionLevel]::Optimal)
        $stream = $entry.Open()
        try {
            $bytes = [System.IO.File]::ReadAllBytes($file.FullName)
            $stream.Write($bytes, 0, $bytes.Length)
        }
        finally {
            $stream.Dispose()
        }
        $count++
    }
}
finally {
    $zip.Dispose()
    $fs.Dispose()
}

$probe = [System.IO.Compression.ZipFile]::OpenRead($OutFile)
try {
    $wrapped = @($probe.Entries | Where-Object { $_.FullName -like "payload/*" }).Count
    $badSeparators = @($probe.Entries | Where-Object { $_.FullName -match '\\' }).Count
}
finally {
    $probe.Dispose()
}

if ($wrapped -gt 0) {
    throw "Zip contains a 'payload/' wrapper folder. Extraction would nest the site one level deep."
}
if ($badSeparators -gt 0) {
    throw "Zip entries use backslash separators ($badSeparators). Some extractors would flatten the layout."
}

$dbFile = Join-Path $payload "data\dev.db"
$dbNote = if (Test-Path -LiteralPath $dbFile) {
    "included (pre-migrated)"
} else {
    "NOT included - run prepare-db.ps1 first if you want it"
}

Write-Host ""
Write-Host ("Created: {0}" -f $OutFile)
Write-Host ("Entries: {0}  Size: {1:N1} MB" -f $count, ((Get-Item -LiteralPath $OutFile).Length / 1MB))
Write-Host ("Database: {0}" -f $dbNote)
Write-Host ""
Write-Host "Upload this zip to wwwroot and extract it there. The zip root IS the site root:"
Write-Host "  wwwroot/Web.config, wwwroot/bin, wwwroot/ar, wwwroot/_next, wwwroot/Migrations"
