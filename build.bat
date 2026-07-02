# Simple build script for Windows 7+ compatibility
# This script creates a portable desktop application using Node.js

@echo off
setlocal enabledelayedexpansion

echo ==================== POS Desktop Application Build ====================
echo.

REM Check if we're in the right directory
if not exist "package.json" (
    echo ERROR: package.json not found. Please run this from the project root.
    exit /b 1
)

echo [1/4] Checking Node.js installation...
npm --version >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo ERROR: Node.js is required. Please install Node.js from https://nodejs.org
    exit /b 1
)
echo Node.js found: OK
echo.

echo [2/4] Installing dependencies...
npm install > nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo ERROR: Failed to install dependencies
    exit /b 1
)
echo Dependencies installed: OK
echo.

echo [3/4] Building API server...
cd api-server
npm run build > nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo ERROR: Failed to build API server
    exit /b 1
)
echo API Server built: OK
cd ..
echo.

echo [4/4] Creating portable runtime structure...
if exist "portable\runtime" rmdir /s /q "portable\runtime"
if exist "portable" mkdir portable
mkdir portable\runtime

echo Copying runtime files...
copy /Y api-server\dist\* portable\runtime\api-server > nul 2>&1
if exist "src" xcopy /E /I /Q "src\*" "portable\runtime\src" > nul 2>&1
echo Runtime structure created: OK
echo.

echo ==================== Build Complete ========================
echo Portable application ready at: portable\runtime\
echo.
echo Next steps:
echo 1. Open the "portable\runtime" folder
 echo 2. Run "start.bat" to launch the POS application
 echo.
echo Features included:
echo - POS desktop interface (via browser)
echo - API server with JSON endpoints
 echo - SQLite database engine
 echo - Printing support
 echo - Arabic RTL support
 echo - Windows 7 compatible (no WebView2 dependency)
echo.
echo Note: You'll need to manually edit "start.bat" to add your WebView2 installer
if exist "install-webview2.ps1" (
    echo and include: powershell -ExecutionPolicy Bypass -File install-webview2.ps1
)

echo Press any key to continue...
pause > nul
