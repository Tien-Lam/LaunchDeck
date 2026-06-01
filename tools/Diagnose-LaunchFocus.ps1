param(
    [ValidateSet("exe", "url", "store")]
    [string]$Type = "exe",

    [Parameter(Mandatory = $true)]
    [string]$Path,

    [string]$LaunchArgs,

    [switch]$Focus,

    [int]$FocusDelayMs = 300,

    [int]$TimeoutMs = 7000,

    [int]$PostFocusObserveMs = 300,

    [int]$SimulateFocusStealAfterMs = -1,

    [int]$SimulateFocusStealDurationMs = 3000,

    [string]$Configuration = "Debug",

    [ValidateSet("x64", "x86", "ARM")]
    [string]$Platform = "x64",

    [switch]$NoBuild,

    [string]$ReportPath
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$framework = "net10.0-windows10.0.19041.0"
$companionProject = Join-Path $repoRoot "LaunchDeck.Companion\LaunchDeck.Companion.csproj"
$companionDll = Join-Path $repoRoot "LaunchDeck.Companion\bin\$Platform\$Configuration\$framework\LaunchDeck.Companion.dll"

if (-not $NoBuild) {
    dotnet build $companionProject --configuration $Configuration --no-restore -p:Platform=$Platform | Write-Output
    if ($LASTEXITCODE -ne 0) {
        throw "Companion build failed with exit code $LASTEXITCODE."
    }
}

if (-not (Test-Path -LiteralPath $companionDll)) {
    throw "Companion assembly not found: $companionDll"
}

if ([string]::IsNullOrWhiteSpace($ReportPath)) {
    $reportDir = Join-Path $env:LOCALAPPDATA "LaunchDeck\Diagnostics"
    New-Item -ItemType Directory -Force -Path $reportDir | Out-Null
    $ReportPath = Join-Path $reportDir ("launch-focus-{0}.json" -f (Get-Date -Format "yyyyMMdd-HHmmss-fff"))
}

$diagnosticArgs = @(
    "--diagnose-launch",
    "--type", $Type,
    "--path", $Path,
    "--timeout-ms", $TimeoutMs.ToString(),
    "--focus-delay-ms", $FocusDelayMs.ToString(),
    "--post-focus-observe-ms", $PostFocusObserveMs.ToString(),
    "--report", $ReportPath
)

if (-not [string]::IsNullOrWhiteSpace($LaunchArgs)) {
    $diagnosticArgs += @("--args", $LaunchArgs)
}

if ($Focus) {
    $diagnosticArgs += "--focus"
}
else {
    $diagnosticArgs += "--no-focus"
}

if ($SimulateFocusStealAfterMs -ge 0) {
    $diagnosticArgs += @(
        "--simulate-focus-steal-after-ms", $SimulateFocusStealAfterMs.ToString(),
        "--simulate-focus-steal-duration-ms", $SimulateFocusStealDurationMs.ToString()
    )
}

& dotnet $companionDll @diagnosticArgs
$diagnosticExitCode = $LASTEXITCODE

if (Test-Path -LiteralPath $ReportPath) {
    Get-Content -Path $ReportPath -Raw
}
else {
    Write-Output "No diagnostic report was written."
}

exit $diagnosticExitCode
