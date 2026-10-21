$ErrorActionPreference = "Continue"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptDir

$gitExe = "C:\Users\xzen\AppData\Local\Microsoft\WinGet\Packages\Git.MinGit_Microsoft.Winget.Source_8wekyb3d8bbwe\cmd\git.exe"
$ghExe = "C:\Users\xzen\AppData\Local\Microsoft\WinGet\Packages\GitHub.cli_Microsoft.Winget.Source_8wekyb3d8bbwe\bin\gh.exe"

if (-not (Test-Path $gitExe)) {
    $gitCmd = Get-Command git.exe -ErrorAction SilentlyContinue
    if ($gitCmd) { $gitExe = $gitCmd.Source }
}
if (-not (Test-Path $ghExe)) {
    $ghCmd = Get-Command gh.exe -ErrorAction SilentlyContinue
    if ($ghCmd) { $ghExe = $ghCmd.Source }
}

Write-Host "========================================================" -ForegroundColor Magenta
Write-Host "  Chibi Desktop Pet - GitHub Publisher" -ForegroundColor Cyan
Write-Host "  Repository: standoff2jdm-droid/chibimalutka" -ForegroundColor White
Write-Host "========================================================" -ForegroundColor Magenta

# 1. Check Auth
Write-Host ""
Write-Host "[1/3] Checking GitHub login..." -ForegroundColor Yellow
$authCheck = & $ghExe auth status 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "Action required: Logging in via browser..." -ForegroundColor Cyan
    Write-Host "A browser window will open. Please confirm authorization for standoff2jdm-droid." -ForegroundColor Yellow
    & $ghExe auth login -h github.com -p https -w -s repo
    & $ghExe auth setup-git
} else {
    Write-Host "GitHub authorization confirmed!" -ForegroundColor Green
    & $ghExe auth setup-git
}

# 2. Push Code and Tags
Write-Host ""
Write-Host "[2/3] Pushing code and tag v0.8.5-beta to GitHub..." -ForegroundColor Yellow
& $gitExe push -u origin main --tags

if ($LASTEXITCODE -ne 0) {
    Write-Host "Retrying push with credentials setup..." -ForegroundColor Yellow
    & $ghExe auth setup-git
    & $gitExe push -u origin main --tags
}

# 3. Create GitHub Release with Installer
Write-Host ""
Write-Host "[3/3] Publishing Release v0.8.5-beta with Installer on GitHub..." -ForegroundColor Yellow
$setupExe = Join-Path $scriptDir "release_dist\ChibiDesktopPet-Setup.exe"
$zipPkg = Join-Path $scriptDir "release_dist\ChibiDesktopPet-v0.8.5-beta.zip"

if (Test-Path $setupExe) {
    & $ghExe release create v0.8.5-beta $setupExe $zipPkg --title "🌸 Chibi Desktop Pet v0.8.5-beta" --notes-file (Join-Path $scriptDir "RELEASE_NOTES_v0.8.5.md") --prerelease
    Write-Host "GitHub Release v0.8.5-beta created successfully!" -ForegroundColor Green
}

Write-Host ""
Write-Host "========================================================" -ForegroundColor Magenta
Write-Host "  Success! Your repository and release are now live:" -ForegroundColor Green
Write-Host "  https://github.com/standoff2jdm-droid/chibimalutka" -ForegroundColor White
Write-Host "========================================================" -ForegroundColor Magenta
Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
