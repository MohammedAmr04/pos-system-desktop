@echo off
set DIST_PATH=E:\work\poc\pos-app\api-server\dist
if exist "%DIST_PATH%" (
    rmdir /s /q "%DIST_PATH%"
)
mkdir "%DIST_PATH%"
cd "E:\work\poc\pos-app\api-server"
npx tsc --noEmit 2>&1 | more
