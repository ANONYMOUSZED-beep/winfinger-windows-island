@echo off
rem Build a self-contained single-file WinFinger.exe (win-x64)
cd /d "%~dp0.."
dotnet publish src\WinFinger\WinFinger.csproj -c Release -r win-x64 --self-contained ^
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:EnableCompressionInSingleFile=true
if errorlevel 1 exit /b 1
echo.
echo Output: src\WinFinger\bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\WinFinger.exe
