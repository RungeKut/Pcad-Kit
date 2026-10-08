@echo off
rem Сборка DbxHost.exe штатным компилятором .NET Framework 4 (есть на любой
rem Windows). /platform:x86 обязателен: Dbx32.dll — 32-битная.
setlocal
set CSC=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe
if not exist "%CSC%" (
    echo csc.exe not found at %CSC%
    exit /b 1
)
"%CSC%" /nologo /platform:x86 /target:exe /out:"%~dp0DbxHost.exe" /r:System.Web.Extensions.dll "%~dp0DbxHost.cs"
if errorlevel 1 (
    echo BUILD FAILED
    exit /b 1
)
echo OK: %~dp0DbxHost.exe
endlocal
