Write-Host "Running standalone build via build-next-standalone.ps1..."
powershell -ExecutionPolicy Bypass -File build-next-standalone.ps1

Write-Host "Building Tauri Executable..."
npx tauri build --bundles exe
