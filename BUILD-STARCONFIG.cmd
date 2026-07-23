@echo off
setlocal EnableExtensions
cd /d "%~dp0"
title StarConfig Windows Builder

set "TOOLS=%~dp0.tools"
set "DOTNET_DIR=%TOOLS%\dotnet"
set "DOTNET_EXE=%DOTNET_DIR%\dotnet.exe"
set "SDK_ZIP=%TOOLS%\dotnet-sdk-8.0.126-win-x64.zip"
set "SDK_URL=https://builds.dotnet.microsoft.com/dotnet/Sdk/8.0.126/dotnet-sdk-8.0.126-win-x64.zip"
set "APP_OUT=%~dp0dist\StarConfig"
set "INSTALLER_OUT=%~dp0dist\Installer"
set "PAYLOAD=%~dp0src\StarConfig.Installer\Payload.zip"

echo.
echo ============================================================
echo   STARCONFIG - WINDOWS APPLICATION AND INSTALLER BUILDER
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
    pause
    exit /b 1
)

curl.exe --fail --location --retry 3 --output "%SDK_ZIP%" "%SDK_URL%"
if errorlevel 1 (
    echo ERROR: Could not download the official Microsoft .NET SDK archive.
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

if exist "%APP_OUT%" rmdir /s /q "%APP_OUT%"
if exist "%INSTALLER_OUT%" rmdir /s /q "%INSTALLER_OUT%"
if exist "%PAYLOAD%" del /q "%PAYLOAD%"
mkdir "%APP_OUT%"
mkdir "%INSTALLER_OUT%"

echo Building standalone StarConfig application...
"%DOTNET_EXE%" restore "%~dp0StarConfig.sln"
if errorlevel 1 goto :failed

"%DOTNET_EXE%" publish "%~dp0src\StarConfig.App\StarConfig.App.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o "%APP_OUT%"
if errorlevel 1 goto :failed

(
    echo STARCONFIG
    echo.
    echo Run StarConfig.exe.
    echo Select an exported Star Citizen XML profile.
    echo Select an action, click Listen for Input, press or move the control, then save.
    echo Every save creates a timestamped backup beside the profile.
) > "%APP_OUT%\START-HERE.txt"

echo Packing application into the installer...
tar.exe -a -c -f "%PAYLOAD%" -C "%APP_OUT%" .
if errorlevel 1 goto :failed

echo Building StarConfig-Setup.exe...
"%DOTNET_EXE%" publish "%~dp0src\StarConfig.Installer\StarConfig.Installer.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o "%INSTALLER_OUT%"
if errorlevel 1 goto :failed

if not exist "%INSTALLER_OUT%\StarConfig-Setup.exe" goto :failed

del /q "%PAYLOAD%" >nul 2>nul

echo.
echo ============================================================
echo   BUILD COMPLETE
echo.
echo   USER DOWNLOAD:
echo   %INSTALLER_OUT%\StarConfig-Setup.exe
echo.
echo   PORTABLE APP:
echo   %APP_OUT%\StarConfig.exe
echo ============================================================
echo.
start "" "%INSTALLER_OUT%"
pause
exit /b 0

:failed
echo.
echo BUILD FAILED. Read the error above.
if exist "%PAYLOAD%" del /q "%PAYLOAD%" >nul 2>nul
pause
exit /b 1