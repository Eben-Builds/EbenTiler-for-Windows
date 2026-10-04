param(
    [string]$Tag = 'v9.9.9',
    [switch]$Restore
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$exePath = Join-Path $root 'build\Tessdeck.exe'
$configPath = Join-Path $root 'build\Tessdeck.exe.config'
$configDir = Join-Path $env:APPDATA 'Tessdeck'
$badgePath = Join-Path $configDir 'update-badge.ini'
$backupPath = Join-Path $env:TEMP 'Tessdeck-update-badge.backup.ini'

function Stop-Tessdeck {
    Get-Process Tessdeck -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Milliseconds 300
}

if ($Restore) {
    Stop-Tessdeck

    if (Test-Path $backupPath) {
        if (-not (Test-Path $configDir)) {
            New-Item -ItemType Directory -Path $configDir -Force | Out-Null
        }
        Copy-Item -Path $backupPath -Destination $badgePath -Force
        Remove-Item $backupPath -Force
        Write-Host 'Restored the previous update badge state.'
    }
    else {
        Remove-Item $badgePath -Force -ErrorAction SilentlyContinue
        Write-Host 'Cleared the update badge test state.'
    }

    if (Test-Path $exePath) {
        Start-Process -FilePath $exePath | Out-Null
    }
    return
}

if (-not (Test-Path $exePath)) {
    throw "Build output was not found: $exePath`nRun: powershell -ExecutionPolicy Bypass -File build.ps1"
}
if (-not (Test-Path $configPath)) {
    throw "Runtime config was not found: $configPath`nRun build.ps1 again before testing."
}

$versionText = $Tag.TrimStart('v', 'V')
$version = $null
if (-not [Version]::TryParse($versionText, [ref]$version)) {
    throw "Tag must be a numeric release version such as v1.0.1 or v9.9.9: $Tag"
}

$exeVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($exePath).FileVersion
$current = $null
if ([Version]::TryParse($exeVersion, [ref]$current) -and $version -le $current) {
    throw "Test tag $Tag must be newer than the current app version $current."
}

Stop-Tessdeck

if (Test-Path $badgePath) {
    Copy-Item -Path $badgePath -Destination $backupPath -Force
}
else {
    Remove-Item $backupPath -Force -ErrorAction SilentlyContinue
}

if (-not (Test-Path $configDir)) {
    New-Item -ItemType Directory -Path $configDir -Force | Out-Null
}

@(
    '; Tessdeck pending update badge - local visual test'
    "AvailableTag=$Tag"
) | Set-Content -Path $badgePath -Encoding UTF8

Write-Host ''
Write-Host "Injected update badge test state: $Tag"
Write-Host 'Starting Tessdeck...'
Start-Process -FilePath $exePath | Out-Null

Write-Host ''
Write-Host 'Check these items:'
Write-Host '  1. Tray icon has a small orange ! badge.'
Write-Host "  2. Tray menu shows: 업데이트 있음 · $Tag"
Write-Host '  3. Clicking the update menu opens Settings > Info.'
Write-Host ''
Write-Host 'When finished, restore the previous state with:'
Write-Host '  powershell -ExecutionPolicy Bypass -File tools\test-update-badge.ps1 -Restore'
