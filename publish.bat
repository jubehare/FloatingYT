@echo off
echo ====================================
echo   FloatingYT - Build EXE
echo ====================================
echo.

where dotnet >/dev/null 2>&1
if %errorlevel% neq 0 (
    echo ERROR: .NET SDK not found.
    echo Download from: https://dot.net
    pause
    exit /b 1
)

set OUTDIR=%~dp0publish

echo Cleaning previous build...
if exist "%OUTDIR%" rmdir /s /q "%OUTDIR%"

echo Building portable EXE...
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none --output "%OUTDIR%"

if %errorlevel% neq 0 (
    echo ERROR: Build failed.
    pause
    exit /b 1
)

del /q "%OUTDIR%\*.xml" 2>nul

echo.
echo ====================================
echo   Done! Portable file:
echo   %OUTDIR%\FloatingYT.exe
echo ====================================
pause