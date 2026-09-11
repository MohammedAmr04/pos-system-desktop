@echo off
title POS Desktop Application - v2
cd /d "%~dp0"

set "SERVER_PATH=pos-server.exe"
if not exist "%SERVER_PATH%" set "SERVER_PATH=backend\pos-server.exe"
if not exist "%SERVER_PATH%" set "SERVER_PATH=backend-cs\bin\Release\net48\pos-server.exe"

if not exist "%SERVER_PATH%" (
    echo [ERROR] pos-server.exe not found.
    pause
    exit /b 1
)

echo =============================================
echo POS Desktop Application - v2
echo =============================================
echo.

echo [INFO] Starting POS server on port 3001...
start /min "POS Server" "%SERVER_PATH%"

echo [INFO] Waiting for server to start...
timeout /t 3 /nobreak >nul

echo [INFO] Opening browser...
start "" http://localhost:3001/ar

echo.
echo Server is running. Press any key to stop the POS server.
echo.
pause >nul

taskkill /IM pos-server.exe /F >nul 2>&1
