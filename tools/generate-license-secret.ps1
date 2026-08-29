param(
    [string]$Path = "license.secret",
    [switch]$Force
)

$ErrorActionPreference = "Stop"

if (Test-Path -LiteralPath $Path) {
    if (-not $Force) {
        throw "File already exists: $Path. Use -Force to overwrite."
    }
}

$rng = New-Object System.Security.Cryptography.RNGCryptoServiceProvider
$bytes = New-Object byte[] 32
$rng.GetBytes($bytes)
$secret = [Convert]::ToBase64String($bytes)

$secret | Set-Content -LiteralPath $Path -NoNewline

Write-Host ""
Write-Host "==========================================" -ForegroundColor Green
Write-Host "  Generated license secret"
Write-Host "  Saved to: $Path"
Write-Host "  Length: $($secret.Length) characters"
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Keep this file secret. Do not commit it to source control." -ForegroundColor Yellow
Write-Host ""
