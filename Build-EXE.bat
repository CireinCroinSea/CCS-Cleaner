@echo off
setlocal
title Build CCS Cleaner v2.0.0
cd /d "%~dp0"
echo ==========================================
echo   CCS Cleaner v2.0.0 - .NET SDK publisher
echo ==========================================
echo.
where dotnet.exe >nul 2>&1
if errorlevel 1 (
  echo ERROR: .NET SDK not found in PATH.
  pause
  exit /b 1
)
dotnet --list-sdks
echo.
if not exist "%~dp0Publish" mkdir "%~dp0Publish"
dotnet restore "%~dp0CireCleaner.csproj" -r win-x64
if errorlevel 1 (
  echo.
  echo RESTORE FAILED. Read the error above.
  pause
  exit /b 1
)
dotnet build "%~dp0CireCleaner.csproj" -c Release -r win-x64 --no-restore -p:SelfContained=true
if errorlevel 1 (
  echo.
  echo BUILD FAILED. Read the error above.
  pause
  exit /b 1
)
for /d %%D in ("%~dp0Publish\*") do if exist "%%~fD" rmdir /s /q "%%~fD"
del /q "%~dp0Publish\*" 2>nul
dotnet publish "%~dp0CireCleaner.csproj" -c Release -r win-x64 --no-restore --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=None -p:DebugSymbols=false -o "%~dp0Publish"
if errorlevel 1 (
  echo.
  echo PUBLISH FAILED. Read the error above.
  pause
  exit /b 1
)
echo.
if not exist "%~dp0Publish\CCS-Cleaner.exe" (
  echo ERROR: Publish completed but CCS-Cleaner.exe was not found.
  pause
  exit /b 1
)
echo SUCCESS: %~dp0Publish\CCS-Cleaner.exe
pause
