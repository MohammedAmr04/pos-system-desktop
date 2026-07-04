$ErrorActionPreference = "Stop"

cd e:\work\poc\pos-app

Write-Host "Building frontend..."
npm run build

Write-Host "Copying frontend to backend wwwroot..."
if (!(Test-Path "backend-cs\wwwroot")) {
    New-Item -ItemType Directory -Force -Path "backend-cs\wwwroot" | Out-Null
} else {
    Remove-Item "backend-cs\wwwroot\*" -Recurse -Force
}
Copy-Item -Path "out\*" -Destination "backend-cs\wwwroot" -Recurse -Force

Write-Host "Building backend..."
cd backend-cs
dotnet build -c Release
if ($LASTEXITCODE -ne 0) { throw "Backend build failed" }

Write-Host "Creating zip..."
cd ..
if (Test-Path "pos-app-v13.zip") {
    Remove-Item "pos-app-v13.zip" -Force
}
Compress-Archive -Path "backend-cs\bin\Release\net48\*" -DestinationPath "pos-app-v13.zip"

Write-Host "Zip created at e:\work\poc\pos-app\pos-app-v13.zip"
