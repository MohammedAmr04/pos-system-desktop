Write-Host "Building Next.js for Standalone Mode..."
npm run build

Write-Host "Copying static assets and public folder to standalone directory..."
Copy-Item -Path "public" -Destination ".next\standalone\public" -Recurse -Force
New-Item -ItemType Directory -Force -Path ".next\standalone\.next\static" | Out-Null
Copy-Item -Path ".next\static\*" -Destination ".next\standalone\.next\static" -Recurse -Force

Write-Host "Copying SQLite database and Prisma schema..."
Copy-Item -Path "prisma" -Destination ".next\standalone\prisma" -Recurse -Force

Write-Host "Building Tauri Executable..."
npx tauri build
