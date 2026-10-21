@echo off
pushd "%~dp0."
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0push_to_github.ps1"
if errorlevel 1 (
    echo.
    echo Script encountered an error. Press any key to close.
    pause > nul
)
popd
