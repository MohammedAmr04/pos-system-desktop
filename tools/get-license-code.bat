@echo off
rem Launches the license-code helper on this computer (no Python needed).
rem Double-click this file to run it; the code prints at the bottom of the screen.

cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "get-license-code.ps1"
echo.
pause
