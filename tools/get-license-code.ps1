param(
    [string]$MachineId
)

$ErrorActionPreference = "Stop"

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

function Compute-UnlockCode([string]$machineId) {
    # Same algorithm as LicenseCode.cs in the backend.
    $secret = "POS-LICENSE-ACTIVATION-2026-v1"
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

# If the caller provided a machine id, compute its code only.
# Otherwise compute the machine id of THIS computer first.
if (-not $MachineId) {
    $MachineId = Compute-MachineId
}

$code = Compute-UnlockCode -machineId $MachineId

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  Machine ID : $MachineId"
Write-Host "  Unlock code: $code"
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""
Write-Output $code
