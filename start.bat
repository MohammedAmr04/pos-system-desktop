@echo off
setlocal enabledelayedexpansion

echo ====================================
echo POS Desktop Application Launcher
echo ====================================
echo.

REM Check for admin privileges for WebView2 installation
net session > nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo WARNING: Admin privileges required for WebView2 installation
    echo To install WebView2 manually:
    echo 1. Download from: https://developer.microsoft.com/en-us/microsoft-edge/webview2/
    echo 2. Run installer and select "Let me choose how to install"
    echo 3. Choose "Install without Microsoft Edge"
    echo 4. Make sure to install the legacy version for Windows 7 support
    echo.
)

echo.
echo [1/4] Starting the API server...
cd api-server
node dist\server.js
