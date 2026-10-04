param(
    [Parameter(Mandatory=$true)][string]$FromVersion,
    [Parameter(Mandatory=$true)][string]$BaseInstaller,
    [Parameter(Mandatory=$true)][string]$TargetInstaller,
    [Parameter(Mandatory=$true)][string]$TargetVersion,
    [string]$OutDir = '.\build\upgrade-check'
)

$ErrorActionPreference = 'Stop'

$legacyInstallDir = Join-Path $env:LOCALAPPDATA 'Programs\EbenTiler'
$modernInstallDir = Join-Path $env:LOCALAPPDATA 'Programs\Tessdeck'
$legacyConfigDir = Join-Path $env:APPDATA 'EbenTiler'
$modernConfigDir = Join-Path $env:APPDATA 'Tessdeck'
$runKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'

New-Item -ItemType Directory -Path $OutDir -Force | Out-Null
$reportPath = Join-Path $OutDir ("upgrade-{0}-to-{1}.txt" -f ($FromVersion -replace '\.','-'), ($TargetVersion -replace '\.','-'))
$report = New-Object System.Collections.Generic.List[string]

function Add-Report([string]$Line) {
    $report.Add($Line)
    Write-Host $Line
}

function Get-RunValue([string]$Name) {
    if (-not (Test-Path $runKey)) { return $null }
    $props = Get-ItemProperty -Path $runKey -ErrorAction SilentlyContinue
    if ($null -eq $props) { return $null }
    $p = $props.PSObject.Properties[$Name]
    if ($null -eq $p) { return $null }
    return [string]$p.Value
}

function Get-InstalledExe {
    $candidates = @(
        (Join-Path $modernInstallDir 'Tessdeck.exe'),
        (Join-Path $legacyInstallDir 'Tessdeck.exe'),
        (Join-Path $legacyInstallDir 'EbenTiler.exe'),
        (Join-Path $modernInstallDir 'EbenTiler.exe')
    )
    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) { return $candidate }
    }
    return $null
}

function Clean-TestState {
    Get-Process Tessdeck,EbenTiler -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

    foreach ($dir in @($legacyInstallDir,$modernInstallDir,$legacyConfigDir,$modernConfigDir)) {
        if (Test-Path $dir) {
            Remove-Item $dir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    if (Test-Path $runKey) {
        Remove-ItemProperty $runKey -Name 'EbenTiler' -ErrorAction SilentlyContinue
        Remove-ItemProperty $runKey -Name 'Tessdeck' -ErrorAction SilentlyContinue
    }
}

$baseInstallerPath = [IO.Path]::GetFullPath((Join-Path (Get-Location) $BaseInstaller))
$targetInstallerPath = [IO.Path]::GetFullPath((Join-Path (Get-Location) $TargetInstaller))
if (-not (Test-Path $baseInstallerPath)) { throw "Base installer not found: $baseInstallerPath" }
if (-not (Test-Path $targetInstallerPath)) { throw "Target installer not found: $targetInstallerPath" }

Clean-TestState

try {
    Add-Report "Scenario: $FromVersion -> $TargetVersion"
    Add-Report "Base installer: $baseInstallerPath"
    Add-Report "Target installer: $targetInstallerPath"
    Add-Report ""

    $base = Start-Process -FilePath $baseInstallerPath -ArgumentList @(
        '/VERYSILENT',
        '/SUPPRESSMSGBOXES',
        '/NORESTART'
    ) -Wait -PassThru
    if ($base.ExitCode -ne 0) { throw "Base installer exited with code $($base.ExitCode)." }

    $baseExe = Get-InstalledExe
    if ($null -eq $baseExe) { throw "Base executable was not found after installing $FromVersion." }
    $baseInfo = [Diagnostics.FileVersionInfo]::GetVersionInfo($baseExe)

    Add-Report "Base executable: $baseExe"
    Add-Report "Base file version: $($baseInfo.FileVersion)"
    $baseStartupEben = Get-RunValue 'EbenTiler'
    $baseStartupTess = Get-RunValue 'Tessdeck'
    $baseHadStartup = (-not [string]::IsNullOrWhiteSpace($baseStartupEben)) -or (-not [string]::IsNullOrWhiteSpace($baseStartupTess))
    Add-Report "Base startup EbenTiler: $baseStartupEben"
    Add-Report "Base startup Tessdeck: $baseStartupTess"
    Add-Report "Base startup enabled: $baseHadStartup"

    $baseConfigDir = $modernConfigDir
    if ($FromVersion -eq '1.0.1') { $baseConfigDir = $legacyConfigDir }
    New-Item -ItemType Directory -Path $baseConfigDir -Force | Out-Null
    $baseConfig = Join-Path $baseConfigDir 'config.ini'
    Set-Content -LiteralPath $baseConfig -Value @(
        'Gap=17',
        'CycleFractions=true'
    ) -Encoding UTF8
    Set-Content -LiteralPath (Join-Path $baseConfigDir 'upgrade-sentinel.txt') -Value "created-by-$FromVersion" -Encoding UTF8

    $target = Start-Process -FilePath $targetInstallerPath -ArgumentList @(
        '/VERYSILENT',
        '/SUPPRESSMSGBOXES',
        '/NORESTART'
    ) -Wait -PassThru
    if ($target.ExitCode -ne 0) { throw "Target installer exited with code $($target.ExitCode)." }

    $targetExe = Get-InstalledExe
    if ($null -eq $targetExe) { throw 'Tessdeck executable was not found after upgrade.' }
    if ([IO.Path]::GetFileName($targetExe) -ne 'Tessdeck.exe') {
        throw "Upgrade did not install Tessdeck.exe: $targetExe"
    }

    $targetInfo = [Diagnostics.FileVersionInfo]::GetVersionInfo($targetExe)
    if (-not $targetInfo.FileVersion.StartsWith($TargetVersion)) {
        throw "Expected Tessdeck $TargetVersion but found $($targetInfo.FileVersion)."
    }

    Add-Report ""
    Add-Report "Target executable: $targetExe"
    Add-Report "Target file version: $($targetInfo.FileVersion)"
    Add-Report "Legacy install directory exists: $(Test-Path $legacyInstallDir)"
    Add-Report "Modern install directory exists: $(Test-Path $modernInstallDir)"
    Add-Report "Legacy EbenTiler.exe exists: $(Test-Path (Join-Path $legacyInstallDir 'EbenTiler.exe'))"
    $targetStartupEben = Get-RunValue 'EbenTiler'
    $targetStartupTess = Get-RunValue 'Tessdeck'
    Add-Report "Target startup EbenTiler: $targetStartupEben"
    Add-Report "Target startup Tessdeck: $targetStartupTess"

    if (-not [string]::IsNullOrWhiteSpace($targetStartupEben)) {
        throw 'Legacy EbenTiler startup value remains after upgrade.'
    }
    if ($baseHadStartup) {
        $expectedStartup = '"' + $targetExe + '"'
        if ($targetStartupTess -ne $expectedStartup) {
            throw "Startup preference was not preserved. Expected $expectedStartup but got $targetStartupTess"
        }
    } elseif (-not [string]::IsNullOrWhiteSpace($targetStartupTess)) {
        throw 'Upgrade unexpectedly enabled startup.'
    }

    $out = Join-Path $env:TEMP ("tessdeck-upgrade-" + [Guid]::NewGuid().ToString('N') + '.txt')
    $cli = Start-Process -FilePath $targetExe -ArgumentList @('--list','--out',$out) -Wait -PassThru
    if ($cli.ExitCode -ne 0) { throw "Upgraded Tessdeck CLI exited with code $($cli.ExitCode)." }
    if (-not (Test-Path $out)) { throw 'Upgraded Tessdeck CLI output was not created.' }
    Remove-Item $out -Force -ErrorAction SilentlyContinue

    $modernConfig = Join-Path $modernConfigDir 'config.ini'
    if (-not (Test-Path $modernConfig)) {
        throw 'Tessdeck config.ini does not exist after launching the upgraded app.'
    }
    $configText = Get-Content -LiteralPath $modernConfig -Raw
    if ($configText -notmatch 'Gap=17') {
        throw 'User Gap setting was not preserved through the upgrade.'
    }

    Add-Report "Modern config exists: True"
    Add-Report "Gap=17 preserved: True"
    $legacySentinel = Join-Path $legacyConfigDir 'upgrade-sentinel.txt'
    Add-Report "Legacy config directory still exists: $(Test-Path $legacyConfigDir)"
    Add-Report "Unrelated legacy sentinel preserved: $(Test-Path $legacySentinel)"

    if ($FromVersion -eq '1.0.1') {
        if (-not $targetExe.StartsWith($modernInstallDir, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Legacy default install path was not migrated to Tessdeck: $targetExe"
        }
        if (Test-Path $legacyInstallDir) {
            throw "Legacy EbenTiler install directory remains after upgrade: $legacyInstallDir"
        }
        if (-not (Test-Path $legacySentinel)) {
            throw 'Upgrade removed an unrelated file from the legacy config directory.'
        }
    }

    Add-Report "Upgrade result: PASS"
}
finally {
    Set-Content -LiteralPath $reportPath -Value $report -Encoding utf8

    $uninstallers = @(
        (Join-Path $modernInstallDir 'unins000.exe'),
        (Join-Path $legacyInstallDir 'unins000.exe')
    )
    foreach ($uninstaller in $uninstallers) {
        if (Test-Path $uninstaller) {
            try {
                Start-Process -FilePath $uninstaller -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART') -Wait | Out-Null
            } catch { }
        }
    }

    Clean-TestState
}
