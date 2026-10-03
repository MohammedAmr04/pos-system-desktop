param(
    [int]$Port = 5011,
    [string]$Payload = "",
    [string]$IisExpress = "",
    [int]$TimeoutSeconds = 60
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $Payload) {
    $Payload = Join-Path $scriptDir "payload"
}
if (-not (Test-Path -LiteralPath (Join-Path $Payload "Web.config"))) {
    throw "Payload not found at '$Payload'. Run package.ps1 first."
}

if (-not $IisExpress) {
    $candidates = @(
        (Join-Path $env:ProgramFiles "IIS Express\iisexpress.exe"),
        (Join-Path ${env:ProgramFiles(x86)} "IIS Express\iisexpress.exe")
    )
    $IisExpress = $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}
if (-not $IisExpress -or -not (Test-Path -LiteralPath $IisExpress)) {
    throw "iisexpress.exe not found."
}

$outLog = Join-Path $env:TEMP "pos-iis-smoke.out.log"
$errLog = Join-Path $env:TEMP "pos-iis-smoke.err.log"
Remove-Item -LiteralPath $outLog, $errLog -ErrorAction SilentlyContinue

Write-Output "Starting: $IisExpress"
Write-Output "Payload:  $Payload"
$p = Start-Process -FilePath $IisExpress -ArgumentList "/path:$Payload", "/port:$Port" -PassThru -WindowStyle Hidden -RedirectStandardOutput $outLog -RedirectStandardError $errLog

Add-Type -AssemblyName System.Net.Http
$client = New-Object System.Net.Http.HttpClient
$client.Timeout = [TimeSpan]::FromSeconds(30)
$base = "http://localhost:$Port"
$results = @()

function Add-Result([string]$name, [bool]$pass, [string]$detail) {
    $script:results += [PSCustomObject]@{ Name = $name; Pass = $pass; Detail = $detail }
}

try {
    $up = $false
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    while ([DateTime]::UtcNow -lt $deadline) {
        Start-Sleep -Seconds 2
        try {
            $r = $client.GetAsync("$base/health").Result
            if ($r.IsSuccessStatusCode) { $up = $true; break }
        } catch { }
    }
    if (-not $up) {
        Add-Result "startup" $false "health endpoint not ready within ${TimeoutSeconds}s"
        throw "App did not start"
    }
    Add-Result "startup" $true "health endpoint is up"

    $r = $client.GetAsync("$base/health").Result
    Add-Result "GET /health" ($r.StatusCode.value__ -eq 200) ([string]$r.StatusCode)

    $r = $client.GetAsync("$base/").Result
    $body = $r.Content.ReadAsStringAsync().Result
    Add-Result "GET / (SPA root)" ($r.IsSuccessStatusCode -and $body.Contains("<html")) "$($r.StatusCode) len=$($body.Length)"

    $r = $client.GetAsync("$base/ar/products/").Result
    Add-Result "GET /ar/products/" ($r.IsSuccessStatusCode) ([string]$r.StatusCode)

    $r = $client.GetAsync("$base/api/products").Result
    Add-Result "GET /api/products (no token -> 401)" ($r.StatusCode.value__ -eq 401) ([string]$r.StatusCode)

    $req = New-Object System.Net.Http.HttpRequestMessage([System.Net.Http.HttpMethod]::Post, "$base/api/auth/login")
    $req.Content = New-Object System.Net.Http.StringContent('{"username":"__smoke__","password":"wrong"}', [System.Text.Encoding]::UTF8, "application/json")
    $r = $client.SendAsync($req).Result
    $loginBody = $r.Content.ReadAsStringAsync().Result
    $loginOk = ($r.StatusCode.value__ -eq 401 -or $r.StatusCode.value__ -eq 400) -and $loginBody.Contains("{")
    Add-Result "POST /api/auth/login (bad creds)" $loginOk "$($r.StatusCode) body=$loginBody"

    $req = New-Object System.Net.Http.HttpRequestMessage([System.Net.Http.HttpMethod]::Post, "$base/api/auth/login")
    $req.Content = New-Object System.Net.Http.StringContent('{"username":"admin","password":"1234"}', [System.Text.Encoding]::UTF8, "application/json")
    $r = $client.SendAsync($req).Result
    $adminBody = $r.Content.ReadAsStringAsync().Result
    $token = $null
    if ($r.IsSuccessStatusCode) {
        try { $token = ($adminBody | ConvertFrom-Json).token } catch { $token = $null }
    }
    Add-Result "POST /api/auth/login (seeded admin)" ($null -ne $token) "$($r.StatusCode)"

    if ($token) {
        $req = New-Object System.Net.Http.HttpRequestMessage([System.Net.Http.HttpMethod]::Post, "$base/api/printing/print-barcode")
        $req.Content = New-Object System.Net.Http.StringContent('{"barcode":"000","name":"smoke","price":1,"count":1}', [System.Text.Encoding]::UTF8, "application/json")
        $req.Headers.Authorization = New-Object System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", $token)
        $r = $client.SendAsync($req).Result
        $printBody = $r.Content.ReadAsStringAsync().Result
        $printOk = ($r.StatusCode.value__ -eq 503) -and $printBody.Contains("printing is disabled")
        Add-Result "POST /api/printing/print-barcode (POS_SERVER_PRINTING=0 -> 503)" $printOk "$($r.StatusCode) body=$printBody"
    } else {
        Add-Result "POST /api/printing/print-barcode (POS_SERVER_PRINTING=0 -> 503)" $false "no admin token"
    }

    $chunk = Get-ChildItem (Join-Path $Payload "_next\static\chunks\*.js") | Select-Object -First 1
    if ($chunk) {
        $root = (Resolve-Path $Payload).Path.TrimEnd("\")
        $rel = $chunk.FullName.Substring($root.Length).Replace("\", "/")
        $r = $client.GetAsync("$base$rel").Result
        Add-Result "GET static chunk" $r.IsSuccessStatusCode "$rel $($r.StatusCode)"
    } else {
        Add-Result "GET static chunk" $false "no chunk files found under payload\_next"
    }

    $r = $client.GetAsync("$base/data/dev.db").Result
    $blocked = ($r.StatusCode.value__ -eq 403 -or $r.StatusCode.value__ -eq 404)
    Add-Result "GET /data/dev.db blocked" $blocked "$($r.StatusCode)"

    $r = $client.GetAsync("$base/Migrations/001_init.sql").Result
    $blocked = ($r.StatusCode.value__ -eq 403 -or $r.StatusCode.value__ -eq 404)
    Add-Result "GET /Migrations/001_init.sql blocked" $blocked "$($r.StatusCode)"

    Add-Result "data/dev.db created" (Test-Path -LiteralPath (Join-Path $Payload "data\dev.db")) ""

    $logFiles = @(Get-ChildItem -Path (Join-Path $Payload "data\logs\pos-*.log") -ErrorAction SilentlyContinue)
    $logOk = $logFiles.Count -gt 0 -and $logFiles[0].Length -gt 0
    Add-Result "file log under data\logs" $logOk "$(if ($logFiles) { $logFiles[0].Name + ' ' + $logFiles[0].Length + 'B' } else { 'not found' })"
}
finally {
    Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue

    # The log file handle is released once iisexpress exits; give it a moment.
    foreach ($i in 1..10) {
        if (-not (Get-Process -Id $p.Id -ErrorAction SilentlyContinue)) { break }
        Start-Sleep -Milliseconds 300
    }

    $testData = Join-Path $Payload "data"
    if (Test-Path -LiteralPath $testData) {
        foreach ($i in 1..5) {
            Remove-Item -Path (Join-Path $testData "*") -Force -Recurse -ErrorAction SilentlyContinue
            if (-not (Get-ChildItem -LiteralPath $testData -Force -ErrorAction SilentlyContinue)) { break }
            Start-Sleep -Milliseconds 500
        }
        Remove-Item -LiteralPath $testData -Force -Recurse -ErrorAction SilentlyContinue
    }
    if (Test-Path -LiteralPath $testData) {
        Write-Warning "Could not remove '$testData' from the payload. Delete it manually before uploading."
    }
    else {
        Write-Output "Removed smoke-test data folder from payload (do not upload it)."
    }
}

Write-Output ""
$failed = 0
foreach ($item in $results) {
    $tag = if ($item.Pass) { "PASS" } else { "FAIL"; $failed++ }
    Write-Output ("[{0}] {1} {2}" -f $tag, $item.Name, $item.Detail)
}
Write-Output ""
if ($failed -gt 0) {
    Write-Output "$failed check(s) failed. See logs: $outLog / $errLog"
    exit 1
}
Write-Output "All checks passed."
exit 0
