param(
    [string]$MachineId,
    [string]$Secret
)

$ErrorActionPreference = "Stop"

function Get-LicenseSecret {
    param([string]$ExplicitSecret)

    if (-not [string]::IsNullOrWhiteSpace($ExplicitSecret)) {
        return $ExplicitSecret.Trim()
    }

    $envSecret = $env:POS_LICENSE_SECRET
    if (-not [string]::IsNullOrWhiteSpace($envSecret)) {
        return $envSecret.Trim()
    }

    $scriptDir = Split-Path -Parent $MyInvocation.ScriptName
    $searchPaths = @(
        (Join-Path $scriptDir "license.secret"),
        (Join-Path (Get-Location) "license.secret"),
        (Join-Path $PSScriptRoot "license.secret")
    )

    foreach ($path in $searchPaths) {
        if (Test-Path -LiteralPath $path) {
            $fileSecret = (Get-Content -LiteralPath $path -Raw).Trim()
            if (-not [string]::IsNullOrWhiteSpace($fileSecret)) {
                return $fileSecret
            }
        }
    }

    throw "License secret is not configured. Set POS_LICENSE_SECRET environment variable, provide -Secret, or create license.secret next to the script."
}

function Compute-MachineId {
    # Same algorithm as MachineIdProvider.cs in the backend.
    $parts = New-Object System.Text.StringBuilder
    try {
        [void]$parts.Append($env:COMPUTERNAME)
        [void]$parts.Append('-')
        [void]$parts.Append([Environment]::OSVersion.Platform)
        [void]$parts.Append('-')
        [void]$parts.Append($(if ([Environment]::Is64BitOperatingSystem) { "x64" } else { "x86" }))
        [void]$parts.Append('-')

        $cpu = "unknown"
        try {
            $cpuInfo = Get-WmiObject -Class Win32_Processor | Select-Object -First 1
            $cpu = $cpuInfo.ProcessorId
            if (-not $cpu) { $cpu = "unknown" }
        } catch {
            $cpu = [Environment]::ProcessorCount.ToString()
        }
        [void]$parts.Append($cpu)
    } catch {
        [void]$parts.Append("unknown")
    }

    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = $sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($parts.ToString()))
        return ([BitConverter]::ToString($bytes)).Replace("-", "").Substring(0, 16).ToLower()
    } finally {
        $sha.Dispose()
    }
}

function Compute-UnlockCode([string]$machineId, [string]$secret) {
    $hmac = New-Object System.Security.Cryptography.HMACSHA256
    try {
        $hmac.Key = [Text.Encoding]::UTF8.GetBytes($secret)
        $hash = $hmac.ComputeHash([Text.Encoding]::UTF8.GetBytes("unlock:" + $machineId))
        $value = [BitConverter]::ToUInt32($hash, 0)
        return ($value % 10000).ToString("D4")
    } finally {
        $hmac.Dispose()
    }
}

$resolvedSecret = Get-LicenseSecret -ExplicitSecret $Secret

if (-not $MachineId) {
    $MachineId = Compute-MachineId
}

$code = Compute-UnlockCode -machineId $MachineId -secret $resolvedSecret

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  Machine ID : $MachineId"
Write-Host "  Unlock code: $code"
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""
Write-Output $code
