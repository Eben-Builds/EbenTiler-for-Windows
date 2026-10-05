param(
    [ValidateSet('Before', 'After', 'Current')]
    [string]$Mode = 'Current',
    [string]$ExpectedVersion = '1.3.0'
)

$ErrorActionPreference = 'Stop'

$QaDir = Join-Path $env:TEMP 'Tessdeck-Upgrade-QA'
$BaselineFile = Join-Path $QaDir 'baseline.json'
$LegacyInstallDir = Join-Path $env:LOCALAPPDATA 'Programs\EbenTiler'
$ModernInstallDir = Join-Path $env:LOCALAPPDATA 'Programs\Tessdeck'
$LegacyConfigDir = Join-Path $env:APPDATA 'EbenTiler'
$ModernConfigDir = Join-Path $env:APPDATA 'Tessdeck'
$QuickLayoutPath = Join-Path $ModernConfigDir 'quick-layout.ini'
$RunKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'

function Get-RunValue {
    param([string]$Name)

    if (-not (Test-Path $RunKey)) { return $null }
    $item = Get-ItemProperty -Path $RunKey -ErrorAction SilentlyContinue
    if ($null -eq $item) { return $null }

    $property = $item.PSObject.Properties[$Name]
    if ($null -eq $property) { return $null }
    return [string]$property.Value
}

function Get-InstalledExe {
    $paths = @(
        (Join-Path $ModernInstallDir 'Tessdeck.exe'),
        (Join-Path $LegacyInstallDir 'Tessdeck.exe'),
        (Join-Path $LegacyInstallDir 'EbenTiler.exe')
    )

    foreach ($path in $paths) {
        if (Test-Path $path) { return $path }
    }
    return $null
}

function Get-ConfigPath {
    $paths = @(
        (Join-Path $ModernConfigDir 'config.ini'),
        (Join-Path $LegacyConfigDir 'config.ini')
    )

    foreach ($path in $paths) {
        if (Test-Path $path) { return $path }
    }
    return $null
}

function Get-ConfigValues {
    param([string]$Path)

    $result = @{}
    if ([string]::IsNullOrWhiteSpace($Path) -or -not (Test-Path $Path)) {
        return $result
    }

    foreach ($line in Get-Content -LiteralPath $Path -Encoding UTF8) {
        $line = $line.Trim()
        if ($line.Length -eq 0) { continue }
        if ($line.StartsWith(';') -or $line.StartsWith('#') -or $line.StartsWith('[')) { continue }

        $eq = $line.IndexOf('=')
        if ($eq -le 0) { continue }

        $key = $line.Substring(0, $eq).Trim()
        $value = $line.Substring($eq + 1).Trim()
        $result[$key] = $value
    }

    return $result
}

function Get-State {
    $exe = Get-InstalledExe
    $config = Get-ConfigPath
    $version = $null

    if (-not [string]::IsNullOrWhiteSpace($exe)) {
        $version = [Diagnostics.FileVersionInfo]::GetVersionInfo($exe).FileVersion
    }

    $legacyStartup = Get-RunValue 'EbenTiler'
    $tessdeckStartup = Get-RunValue 'Tessdeck'

    return [PSCustomObject]@{
        Exe = $exe
        Version = $version
        Config = $config
        ConfigValues = Get-ConfigValues $config
        QuickLayoutPath = $QuickLayoutPath
        QuickLayoutExists = (Test-Path $QuickLayoutPath)
        QuickLayoutHash = if (Test-Path $QuickLayoutPath) {
            (Get-FileHash -Algorithm SHA256 -LiteralPath $QuickLayoutPath).Hash
        } else {
            $null
        }
        StartupEnabled = (
            -not [string]::IsNullOrWhiteSpace($legacyStartup) -or
            -not [string]::IsNullOrWhiteSpace($tessdeckStartup)
        )
        LegacyStartup = $legacyStartup
        TessdeckStartup = $tessdeckStartup
        LegacyInstallExists = (Test-Path $LegacyInstallDir)
        ModernInstallExists = (Test-Path $ModernInstallDir)
    }
}

function Write-State {
    param($State)

    Write-Host "Executable : $($State.Exe)"
    Write-Host "Version    : $($State.Version)"
    Write-Host "Config     : $($State.Config)"
    Write-Host "QuickLayout: $($State.QuickLayoutExists)"
    Write-Host "Startup    : $($State.StartupEnabled)"
}

function Test-VersionAtLeast {
    param([string]$Actual, [string]$Expected)

    if ([string]::IsNullOrWhiteSpace($Actual)) { return $false }

    try {
        return ([Version]$Actual -ge [Version]$Expected)
    }
    catch {
        return $false
    }
}

function Add-Pass {
    param([string]$Message)
    Write-Host "[PASS] $Message" -ForegroundColor Green
}

function Add-Fail {
    param([string]$Message)
    Write-Host "[FAIL] $Message" -ForegroundColor Red
    $script:Failures++
}

New-Item -ItemType Directory -Path $QaDir -Force | Out-Null
$Failures = 0

if ($Mode -eq 'Before') {
    $state = Get-State
    if ([string]::IsNullOrWhiteSpace($state.Exe)) {
        throw 'No installed EbenTiler or Tessdeck executable was found.'
    }

    $state | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $BaselineFile -Encoding UTF8

    Write-Host ''
    Write-Host 'Upgrade baseline saved.' -ForegroundColor Green
    Write-State $state
    Write-Host "Baseline   : $BaselineFile"
    Write-Host ''
    Write-Host 'Now open the public v1.3.0 Release from Settings > About > Check for updates, download Tessdeck-Setup.exe, and install it over the current version.'
    Write-Host 'Then run:'
    Write-Host 'powershell -ExecutionPolicy Bypass -File tools\verify-local-upgrade.ps1 -Mode After'
    exit 0
}

if ($Mode -eq 'Current') {
    $state = Get-State
    Write-Host ''
    Write-State $state

    if ([string]::IsNullOrWhiteSpace($state.Exe)) {
        Add-Fail 'No installed executable was found.'
    }
    elseif (Test-VersionAtLeast -Actual $state.Version -Expected $ExpectedVersion) {
        Add-Pass "Installed version is $ExpectedVersion or newer."
    }
    else {
        Add-Fail "Installed version is older than $ExpectedVersion."
    }

    if ($state.ModernInstallExists) {
        Add-Pass 'Tessdeck install directory exists.'
    }
    else {
        Add-Fail 'Tessdeck install directory is missing.'
    }

    if (-not $state.LegacyInstallExists) {
        Add-Pass 'Legacy EbenTiler install directory is absent.'
    }
    else {
        Add-Fail 'Legacy EbenTiler install directory still exists.'
    }

    if ([string]::IsNullOrWhiteSpace($state.LegacyStartup)) {
        Add-Pass 'Legacy EbenTiler startup value is absent.'
    }
    else {
        Add-Fail 'Legacy EbenTiler startup value still exists.'
    }

    Write-Host ''
    if ($Failures -eq 0) {
        Write-Host 'Current install check: PASS' -ForegroundColor Green
        exit 0
    }

    Write-Host "Current install check: FAIL ($Failures)" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $BaselineFile)) {
    throw "Baseline not found. Run -Mode Before first: $BaselineFile"
}

$before = Get-Content -LiteralPath $BaselineFile -Raw -Encoding UTF8 | ConvertFrom-Json
$after = Get-State

Write-Host ''
Write-Host 'Tessdeck local upgrade QA'
Write-Host '========================='
Write-State $after
Write-Host ''

if (-not [string]::IsNullOrWhiteSpace($after.Exe) -and
    $after.Exe.StartsWith($ModernInstallDir, [StringComparison]::OrdinalIgnoreCase)) {
    Add-Pass 'Executable is under the Tessdeck install directory.'
}
else {
    Add-Fail "Unexpected executable path: $($after.Exe)"
}

if (Test-VersionAtLeast -Actual $after.Version -Expected $ExpectedVersion) {
    Add-Pass "Version is $ExpectedVersion or newer."
}
else {
    Add-Fail "Version is not $ExpectedVersion or newer: $($after.Version)"
}

if (-not $after.LegacyInstallExists) {
    Add-Pass 'Legacy EbenTiler install directory was removed.'
}
else {
    Add-Fail 'Legacy EbenTiler install directory still exists.'
}

if ([string]::IsNullOrWhiteSpace($after.LegacyStartup)) {
    Add-Pass 'Legacy EbenTiler startup value was removed.'
}
else {
    Add-Fail 'Legacy EbenTiler startup value still exists.'
}

if ([bool]$before.StartupEnabled -eq [bool]$after.StartupEnabled) {
    Add-Pass "Startup preference was preserved: $($after.StartupEnabled)"
}
else {
    Add-Fail "Startup preference changed. Before=$($before.StartupEnabled) After=$($after.StartupEnabled)"
}

if ($after.StartupEnabled) {
    $expectedStartup = '"' + $after.Exe + '"'
    if ($after.TessdeckStartup -eq $expectedStartup) {
        Add-Pass 'Startup path points to the installed Tessdeck.exe.'
    }
    else {
        Add-Fail "Unexpected startup path: $($after.TessdeckStartup)"
    }
}

if (-not [string]::IsNullOrWhiteSpace($after.Config) -and
    $after.Config.StartsWith($ModernConfigDir, [StringComparison]::OrdinalIgnoreCase)) {
    Add-Pass 'Config is stored under APPDATA\Tessdeck.'
}
else {
    Add-Fail "Unexpected config path: $($after.Config)"
}

$beforeValues = @{}
if ($null -ne $before.ConfigValues) {
    foreach ($property in $before.ConfigValues.PSObject.Properties) {
        $beforeValues[$property.Name] = [string]$property.Value
    }
}

$afterValues = $after.ConfigValues
foreach ($key in $beforeValues.Keys) {
    if (-not $afterValues.ContainsKey($key)) {
        Add-Fail "Missing config key after upgrade: $key"
        continue
    }

    if ([string]$afterValues[$key] -ne [string]$beforeValues[$key]) {
        Add-Fail "Config value changed after upgrade: $key"
    }
}

if ($Failures -eq 0) {
    Add-Pass "All $($beforeValues.Count) previous config values were preserved."
}

$beforeQuickLayoutExists = [bool]$before.QuickLayoutExists
if ($beforeQuickLayoutExists) {
    if (-not $after.QuickLayoutExists) {
        Add-Fail 'quick-layout.ini existed before upgrade but is missing after upgrade.'
    }
    elseif ([string]$before.QuickLayoutHash -ne [string]$after.QuickLayoutHash) {
        Add-Fail 'quick-layout.ini content changed during upgrade.'
    }
    else {
        Add-Pass 'Existing quick-layout.ini was preserved byte-for-byte.'
    }
}
else {
    Add-Pass 'No pre-upgrade quick-layout.ini existed, so no snapshot preservation was required.'
}

Write-Host ''
Write-Host 'Manual visual checks:'
Write-Host '  1. Settings > About shows version 1.3.0.'
Write-Host '  2. Taskbar/tray shows the new Tessdeck icon.'
Write-Host '  3. Your usual window-layout hotkeys still work.'
Write-Host '  4. Quick Layout save/restore hotkeys are still assigned as expected.'
Write-Host '  5. Move saved windows to other monitors and confirm Quick Layout restore returns them to the saved monitors/positions.'
Write-Host ''

if ($Failures -eq 0) {
    Write-Host 'Local upgrade QA: PASS' -ForegroundColor Green
    Remove-Item $BaselineFile -Force -ErrorAction SilentlyContinue
    exit 0
}

Write-Host "Local upgrade QA: FAIL ($Failures)" -ForegroundColor Red
Write-Host "Baseline retained at: $BaselineFile"
exit 1
