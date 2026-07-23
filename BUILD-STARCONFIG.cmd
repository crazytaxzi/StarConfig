@echo off
setlocal EnableExtensions
cd /d "%~dp0"
title StarConfig Windows Builder

echo.
echo ============================================================
echo   STARCONFIG - WINDOWS APPLICATION BUILDER
echo ============================================================
echo.

where dotnet >nul 2>nul
if errorlevel 1 (
    echo .NET SDK was not found. Installing Microsoft .NET 8 SDK...
    where winget >nul 2>nul
    if errorlevel 1 (
        echo.
        echo ERROR: Windows Package Manager was not found.
        echo Install the .NET 8 SDK from Microsoft, then run this file again.
        pause
        exit /b 1
    )
    winget install --id Microsoft.DotNet.SDK.8 --exact --accept-package-agreements --accept-source-agreements
    if errorlevel 1 (
        echo.
        echo ERROR: .NET SDK installation failed.
        pause
        exit /b 1
    )
    set "PATH=%ProgramFiles%\dotnet;%PATH%"
)

echo Building standalone Windows x64 application...
if exist "%~dp0dist\StarConfig" rmdir /s /q "%~dp0dist\StarConfig"

dotnet restore "%~dp0StarConfig.sln"
if errorlevel 1 goto :failed

dotnet publish "%~dp0src\StarConfig.App\StarConfig.App.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o "%~dp0dist\StarConfig"
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
