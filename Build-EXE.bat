@echo off
setlocal
title Build Cire Cleaner v2
cd /d "%~dp0"
echo ==========================================
echo   Cire Cleaner v2 - .NET SDK publisher
echo ==========================================
echo.
where dotnet.exe >nul 2>&1
if errorlevel 1 (
  echo ERROR: dotnet SDK not found in PATH.
  pause
  exit /b 1
)
dotnet --list-sdks
echo.
if not exist "%~dp0Publish" mkdir "%~dp0Publish"
dotnet publish "%~dp0CireCleaner\CireCleaner.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "%~dp0Publish"
if errorlevel 1 (
  echo.
  echo PUBLISH FAILED. Read the error above.
  pause
  exit /b 1
)
echo.
echo SUCCESS: %~dp0Publish\Cire-Cleaner.exe
pause
