Write-Host "Cleaning up hanging processes and old build artifacts..."
Stop-Process -Name "node-x86_64-pc-windows-msvc" -Force -ErrorAction SilentlyContinue
Stop-Process -Name "app" -Force -ErrorAction SilentlyContinue

if (Test-Path ".next\standalone") {
    Remove-Item -Path ".next\standalone" -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host "Building Next.js for Standalone Mode..."
npm run build

Write-Host "Next.js standalone build complete."
