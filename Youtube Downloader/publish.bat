@echo off
echo Building YouTube Downloader...
echo.

cd /d "%~dp0"

dotnet publish -c Release -r win-x64 --self-contained true -o "bin\publish"

if %ERRORLEVEL% EQU 0 (
    echo.
    echo Build successful!
    echo Output: %~dp0bin\publish\Youtube Downloader.exe
    echo.
    explorer "bin\publish"
) else (
    echo.
    echo Build failed with error code %ERRORLEVEL%
)

pause
