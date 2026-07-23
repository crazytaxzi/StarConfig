@echo off
setlocal EnableExtensions
cd /d "%~dp0"
title StarConfig Windows Builder

set "TOOLS=%~dp0.tools"
set "DOTNET_DIR=%TOOLS%\dotnet"
set "DOTNET_EXE=%DOTNET_DIR%\dotnet.exe"
set "SDK_ZIP=%TOOLS%\dotnet-sdk-8.0.126-win-x64.zip"
set "SDK_URL=https://builds.dotnet.microsoft.com/dotnet/Sdk/8.0.126/dotnet-sdk-8.0.126-win-x64.zip"

echo.
echo ============================================================
echo   STARCONFIG - WINDOWS APPLICATION BUILDER
echo ============================================================
echo.

where dotnet >nul 2>nul
if not errorlevel 1 (
    set "DOTNET_EXE=dotnet"
    goto :build
)

if exist "%DOTNET_EXE%" goto :build

echo No system .NET SDK found.
echo Downloading Microsoft's official portable .NET 8 SDK locally...
echo Nothing will be installed system-wide.
echo.

if not exist "%TOOLS%" mkdir "%TOOLS%"
if not exist "%DOTNET_DIR%" mkdir "%DOTNET_DIR%"

where curl.exe >nul 2>nul
if errorlevel 1 (
    echo ERROR: Windows curl.exe is unavailable.
    echo This builder requires Windows 10 or Windows 11 with curl.exe.
    pause
    exit /b 1
)

curl.exe --fail --location --retry 3 --output "%SDK_ZIP%" "%SDK_URL%"
if errorlevel 1 (
    echo.
    echo ERROR: Could not download the official Microsoft .NET SDK archive.
    echo Check the internet connection and run this file again.
    pause
    exit /b 1
)

where tar.exe >nul 2>nul
if errorlevel 1 (
    echo ERROR: Windows tar.exe is unavailable.
    pause
    exit /b 1
)

tar.exe -xf "%SDK_ZIP%" -C "%DOTNET_DIR%"
if errorlevel 1 (
    echo.
    echo ERROR: The SDK archive could not be extracted.
    pause
    exit /b 1
)

del /q "%SDK_ZIP%" >nul 2>nul

if not exist "%DOTNET_EXE%" (
    echo ERROR: Portable .NET SDK setup did not produce dotnet.exe.
    pause
    exit /b 1
)

:build
set "DOTNET_CLI_TELEMETRY_OPTOUT=1"
set "DOTNET_NOLOGO=1"
set "DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1"

echo Building standalone Windows x64 application...
if exist "%~dp0dist\StarConfig" rmdir /s /q "%~dp0dist\StarConfig"

"%DOTNET_EXE%" restore "%~dp0StarConfig.sln"
if errorlevel 1 goto :failed

"%DOTNET_EXE%" publish "%~dp0src\StarConfig.App\StarConfig.App.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o "%~dp0dist\StarConfig"
if errorlevel 1 goto :failed

(
    echo STARCONFIG
    echo.
    echo Run StarConfig.exe.
    echo Select an exported Star Citizen XML profile.
    echo Select an action, click Listen for Input, press or move the control, then save.
    echo Every save creates a timestamped backup beside the profile.
) > "%~dp0dist\StarConfig\START-HERE.txt"

echo.
echo ============================================================
echo   BUILD COMPLETE
echo   %~dp0dist\StarConfig\StarConfig.exe
echo ============================================================
echo.
start "" "%~dp0dist\StarConfig"
pause
exit /b 0

:failed
echo.
echo BUILD FAILED. Read the error above.
pause
exit /b 1