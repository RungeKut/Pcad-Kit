@echo off
rem Installs Pcad-Kit by double click.
rem Wrapper around tools\setup.ps1: execution policy is lifted for this run
rem only. All messages are printed by setup.ps1 itself; echo here is Latin
rem only - cmd reads this file in the current code page and Russian text
rem would be garbled on some machines.
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\setup.ps1"
set RC=%ERRORLEVEL%
if not %RC% equ 0 (
    echo.
    echo SETUP FAILED, exit code %RC% -- see the message above.
)
echo.
pause
endlocal & exit /b %RC%
