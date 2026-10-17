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

set "AGENT_PATH=backend-cs\BranchAgent\bin\Release\net48\pos-branch-agent.exe"
if not exist "%AGENT_PATH%" set "AGENT_PATH=backend\pos-branch-agent.exe"
if not exist "%AGENT_PATH%" set "AGENT_PATH=pos-branch-agent.exe"

if exist "branch-agent.env" (
    for /f "usebackq eol=# tokens=1,* delims==" %%A in ("branch-agent.env") do if not "%%A"=="" set "%%A=%%B"
)

echo =============================================
echo POS Desktop Application - v2
echo =============================================
echo.

echo [INFO] Starting POS server on port 3001...
start /min "POS Server" "%SERVER_PATH%"

if exist "%AGENT_PATH%" (
    echo [INFO] Starting branch sync and print worker...
    start /min "POS Branch Agent" "%AGENT_PATH%"
) else (
    echo [WARN] Branch agent executable not found; sync and print retry disabled.
)

echo [INFO] Waiting for server to start...
timeout /t 3 /nobreak >nul

echo [INFO] Opening browser...
start "" http://localhost:3001/ar

echo.
echo Server is running. Press any key to stop the POS server.
echo.
pause >nul

taskkill /IM pos-server.exe /F >nul 2>&1
taskkill /IM pos-branch-agent.exe /F >nul 2>&1
