@echo off
title POS Desktop Application - Version 8
cd /d "%~dp0"

echo =============================================
echo POS Desktop Application - Version 8
echo =============================================
echo.

if not exist "backend\pos-server.exe" (
    echo [ERROR] pos-server.exe not found in the backend folder.
    pause
    exit /b 1
)

echo [INFO] Starting POS server on port 3001...
echo [INFO] Opening browser...
echo.

start "" http://localhost:3001/ar
start "" backend\pos-server.exe

echo.
echo Close this window to stop the POS server.
echo.
timeout /t 60 >nul
