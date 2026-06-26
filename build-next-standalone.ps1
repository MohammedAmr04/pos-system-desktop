Write-Host "Cleaning up hanging processes and old build artifacts..."
Stop-Process -Name "node-x86_64-pc-windows-msvc" -Force -ErrorAction SilentlyContinue
Stop-Process -Name "app" -Force -ErrorAction SilentlyContinue

if (Test-Path ".next\standalone") {
    Remove-Item -Path ".next\standalone" -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host "Building Next.js for Standalone Mode..."
npm run build

Write-Host "Copying static assets and public folder to standalone directory..."
Copy-Item -Path "public" -Destination ".next\standalone\public" -Recurse -Force
New-Item -ItemType Directory -Force -Path ".next\standalone\.next\static" | Out-Null
Copy-Item -Path ".next\static\*" -Destination ".next\standalone\.next\static" -Recurse -Force

Write-Host "Copying SQLite database and Prisma schema..."
Copy-Item -Path "prisma" -Destination ".next\standalone\prisma" -Recurse -Force

Write-Host "Next.js standalone build complete."
