#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Uninstalls LaunchDeck Game Bar widget and cleans up all data.
.DESCRIPTION
    Stops running processes, removes the MSIX package, developer
    certificates, config, cached icons, and companion log.
#>

# Stop running processes
$procs = Get-Process -Name 'LaunchDeck.Companion', 'LaunchDeck.Widget' -ErrorAction SilentlyContinue
if ($procs) {
    Write-Host "Stopping LaunchDeck processes..." -ForegroundColor Yellow
    $procs | Stop-Process -Force
    Start-Sleep -Seconds 1
}

# Remove package
$pkg = Get-AppxPackage *LaunchDeck* -ErrorAction SilentlyContinue
if ($pkg) {
    Write-Host "Removing package: $($pkg.PackageFullName)" -ForegroundColor Cyan
    $pkg | Remove-AppxPackage -ErrorAction Stop
    Write-Host "Package removed." -ForegroundColor Green
} else {
    Write-Host "No package found." -ForegroundColor Yellow
}

# Remove developer certificates
Write-Host "Checking certificates..." -ForegroundColor Cyan
$stores = @('Cert:\LocalMachine\TrustedPeople', 'Cert:\LocalMachine\Root', 'Cert:\CurrentUser\My')
$subjects = @('CN=Developer', 'CN=E37AAF35-F870-4E74-8486-74BED9927C48')
$removed = 0
foreach ($store in $stores) {
    foreach ($subject in $subjects) {
        $certs = Get-ChildItem $store -ErrorAction SilentlyContinue | Where-Object { $_.Subject -eq $subject }
        if ($certs) {
            $certs | Remove-Item
            $removed += $certs.Count
        }
    }
}
if ($removed -gt 0) {
    Write-Host "$removed certificate(s) removed." -ForegroundColor Green
} else {
    Write-Host "No certificates found." -ForegroundColor Yellow
}

# Remove app data (real path + MSIX VFS-redirected path)
Write-Host "Removing app data..." -ForegroundColor Cyan
$paths = @(
    "$env:LOCALAPPDATA\LaunchDeck"
)
# MSIX VFS-redirected data lives under the package folder
Get-ChildItem "$env:LOCALAPPDATA\Packages" -Directory -Filter '*LaunchDeck*' -ErrorAction SilentlyContinue |
    ForEach-Object { $paths += $_.FullName }

$cleaned = 0
foreach ($path in $paths) {
    if (Test-Path $path) {
        Remove-Item $path -Recurse -Force -ErrorAction SilentlyContinue
        $cleaned++
        Write-Host "  Removed $path" -ForegroundColor DarkGray
    }
}
if ($cleaned -eq 0) {
    Write-Host "No app data found." -ForegroundColor Yellow
} else {
    Write-Host "$cleaned location(s) cleaned." -ForegroundColor Green
}

Write-Host ""
Write-Host "LaunchDeck fully uninstalled." -ForegroundColor Green
