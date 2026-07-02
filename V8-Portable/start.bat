@echo off
title POS Desktop Application - Version 8
cd /d "%~dp0"

echo =============================================
echo POS Desktop Application - Version 8
echo =============================================
echo.

if not exist "api-server\node_modules" (
    echo [SETUP] Installing API server dependencies...
    cd api-server
    npm install
    cd ..
    echo.
)

echo [INFO] Starting API server on port 3001...
echo [INFO] Starting web server on port 3000...
echo [INFO] Opening browser...
echo.

start "" http://localhost:3000/ar
node startup.js

pause
