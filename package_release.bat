@echo off
setlocal
pushd "%~dp0."

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0package_release.ps1"

popd
pause
