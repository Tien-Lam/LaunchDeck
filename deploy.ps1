# LaunchPad build + deploy script
# Run from project root in PowerShell

$ErrorActionPreference = 'Stop'

# Find MSBuild
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
$vsPath = & $vswhere -latest -property installationPath
$msbuild = Join-Path $vsPath 'MSBuild\Current\Bin\MSBuild.exe'

if (-not (Test-Path $msbuild)) {
    Write-Error "MSBuild not found at $msbuild"
    exit 1
}

Write-Host "Using MSBuild: $msbuild" -ForegroundColor Cyan

# Kill running LaunchPad processes
$procs = Get-Process -Name 'LaunchPad.Companion', 'LaunchPad.Widget' -ErrorAction SilentlyContinue
if ($procs) {
    Write-Host "Stopping running LaunchPad processes..." -ForegroundColor Yellow
    $procs | Stop-Process -Force
    Start-Sleep -Seconds 1
}

# Build
Write-Host "Building solution..." -ForegroundColor Cyan
& $msbuild LaunchPad.sln -p:Configuration=Debug -p:Platform=x64 -restore -v:minimal
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed"
    exit 1
}

# Deploy
$manifest = Join-Path $PSScriptRoot 'LaunchPad.Package\bin\x64\Debug\AppX\AppxManifest.xml'
Write-Host "Registering package..." -ForegroundColor Cyan
Add-AppxPackage -Register $manifest -ForceApplicationShutdown

Write-Host "Deployed successfully. Open Game Bar (Win+G) to use the widget." -ForegroundColor Green
