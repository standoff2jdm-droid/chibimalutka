$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptDir

Write-Host "========================================================" -ForegroundColor Magenta
Write-Host "  Chibi Desktop Pet - Full Release Packager" -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Magenta

$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$lib = "C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.1"
$refs = "System.dll,System.Core.dll,System.Drawing.dll,WindowsBase.dll,PresentationCore.dll,PresentationFramework.dll,System.Xaml.dll,System.IO.Compression.dll,System.IO.Compression.FileSystem.dll"

# 1. Compile Main Application
Write-Host ""
Write-Host "[1/4] Compiling DesktopPet.exe..." -ForegroundColor Yellow
$outExe = Join-Path $scriptDir "DesktopPet.exe"
$src = Join-Path $scriptDir "src\DesktopPet.cs"

$proc = Start-Process -FilePath $csc -ArgumentList "/target:exe", "/out:`"$outExe`"", "/lib:`"$lib`"", "/reference:$refs", "`"$src`"" -NoNewWindow -PassThru -Wait
if ($proc.ExitCode -ne 0) {
    Write-Host "[ERROR] Main app compilation failed!" -ForegroundColor Red
    exit 1
}
Write-Host "DesktopPet.exe compiled successfully!" -ForegroundColor Green

# 2. Prepare staging
Write-Host ""
Write-Host "[2/4] Preparing distribution files..." -ForegroundColor Yellow
$dist = Join-Path $scriptDir "release_dist\ChibiDesktopPet"
if (Test-Path $dist) {
    Remove-Item $dist -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $dist | Out-Null

Copy-Item (Join-Path $scriptDir "DesktopPet.exe") $dist
Copy-Item (Join-Path $scriptDir "Запустить_Питомца.bat") $dist -ErrorAction SilentlyContinue
Copy-Item (Join-Path $scriptDir "pet_config.json") $dist -ErrorAction SilentlyContinue
Copy-Item (Join-Path $scriptDir "README.md") $dist -ErrorAction SilentlyContinue
Copy-Item (Join-Path $scriptDir "assets") $dist -Recurse

Write-Host "Files staged to $dist" -ForegroundColor Green

# 3. Create zip
Write-Host ""
Write-Host "[3/4] Creating ZIP archive for GitHub Releases..." -ForegroundColor Yellow
$zip = Join-Path $scriptDir "release_dist\ChibiDesktopPet-v0.8.0-beta.zip"
if (Test-Path $zip) {
    Remove-Item $zip -Force
}
Compress-Archive -Path "$dist\*" -DestinationPath $zip -Force
Write-Host "ZIP archive created: $zip" -ForegroundColor Green

# 4. Compile Standalone Installer Setup.exe
Write-Host ""
Write-Host "[4/4] Compiling Standalone Installer (Setup.exe)..." -ForegroundColor Yellow
$installerSrc = Join-Path $scriptDir "src\Installer.cs"
$installerExe = Join-Path $scriptDir "release_dist\ChibiDesktopPet-Setup.exe"
$installerRefs = "System.dll,System.Core.dll,System.Drawing.dll,WindowsBase.dll,PresentationCore.dll,PresentationFramework.dll,System.Xaml.dll,System.IO.Compression.dll,System.IO.Compression.FileSystem.dll,System.Windows.Forms.dll,Microsoft.CSharp.dll"

$procInst = Start-Process -FilePath $csc -ArgumentList "/target:winexe", "/out:`"$installerExe`"", "/lib:`"$lib`"", "/reference:$installerRefs", "/resource:`"$zip`",ChibiPayload.zip", "`"$installerSrc`"" -NoNewWindow -PassThru -Wait
if ($procInst.ExitCode -ne 0) {
    Write-Host "[ERROR] Installer compilation failed!" -ForegroundColor Red
    exit 1
}
Write-Host "Standalone Installer compiled successfully: $installerExe" -ForegroundColor Green

Write-Host ""
Write-Host "========================================================" -ForegroundColor Magenta
Write-Host "  ✨ Release & Installer successfully built!" -ForegroundColor Green
Write-Host "  1. Portable ZIP: $zip" -ForegroundColor White
Write-Host "  2. Installer EXE: $installerExe" -ForegroundColor White
Write-Host "========================================================" -ForegroundColor Magenta
