param(
    [string]$Installer = '.\dist\EbenTiler-Setup.exe'
)

$ErrorActionPreference = 'Stop'

$installerPath = [IO.Path]::GetFullPath((Join-Path (Get-Location) $Installer))
$installDir = Join-Path $env:LOCALAPPDATA 'Programs\EbenTiler'
$installedExe = Join-Path $installDir 'EbenTiler.exe'
$uninstaller = Join-Path $installDir 'unins000.exe'
$configDir = Join-Path $env:APPDATA 'EbenTiler'
$configFile = Join-Path $configDir 'config.ini'
$sentinelFile = Join-Path $configDir 'user-file-must-survive.txt'
$runKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
$outputPath = Join-Path $env:TEMP ('ebentiler-smoke-' + [Guid]::NewGuid().ToString('N') + '.txt')

if (-not (Test-Path $installerPath)) {
    throw "Installer not found: $installerPath"
}
if (Test-Path $installDir) {
    throw "Smoke test requires a clean install directory: $installDir"
}
if (Test-Path $configDir) {
    throw "Smoke test requires a clean config directory: $configDir"
}

try {
    $install = Start-Process -FilePath $installerPath -ArgumentList @(
        '/VERYSILENT',
        '/SUPPRESSMSGBOXES',
        '/NORESTART',
        '/TASKS=""'
    ) -Wait -PassThru
    if ($install.ExitCode -ne 0) {
        throw "Installer exited with code $($install.ExitCode)."
    }
    if (-not (Test-Path $installedExe)) {
        throw "Installed executable not found: $installedExe"
    }

    $cli = Start-Process -FilePath $installedExe -ArgumentList @('--list', '--out', $outputPath) -Wait -PassThru
    if ($cli.ExitCode -ne 0) {
        throw "Installed executable CLI smoke test exited with code $($cli.ExitCode)."
    }
    if (-not (Test-Path $outputPath)) {
        throw 'CLI smoke-test output was not created.'
    }

    $output = Get-Content $outputPath -Raw
    if ($output -notmatch 'LeftHalf' -or $output -notmatch 'RightHalf') {
        throw 'CLI smoke test did not return the expected action list.'
    }

    $startupValue = Get-ItemPropertyValue -Path $runKey -Name 'EbenTiler' -ErrorAction SilentlyContinue
    if ($null -ne $startupValue) {
        throw 'Silent test install unexpectedly registered Windows startup.'
    }

    $startupOn = Start-Process -FilePath $installedExe -ArgumentList @('--startup', 'on', '--out', $outputPath) -Wait -PassThru
    if ($startupOn.ExitCode -ne 0) {
        throw "Startup enable test exited with code $($startupOn.ExitCode)."
    }
    $startupValue = Get-ItemPropertyValue -Path $runKey -Name 'EbenTiler' -ErrorAction SilentlyContinue
    $expectedStartupValue = '"' + $installedExe + '"'
    if ($startupValue -ne $expectedStartupValue) {
        throw "Windows startup value is unexpected. Expected $expectedStartupValue but got $startupValue"
    }

    $startupOff = Start-Process -FilePath $installedExe -ArgumentList @('--startup', 'off', '--out', $outputPath) -Wait -PassThru
    if ($startupOff.ExitCode -ne 0) {
        throw "Startup disable test exited with code $($startupOff.ExitCode)."
    }
    $startupAfterOff = Get-ItemPropertyValue -Path $runKey -Name 'EbenTiler' -ErrorAction SilentlyContinue
    if ($null -ne $startupAfterOff) {
        throw 'Windows startup registry value remains after --startup off.'
    }

    if (-not (Test-Path $uninstaller)) {
        throw "Uninstaller not found: $uninstaller"
    }

    New-Item -ItemType Directory -Path $configDir -Force | Out-Null
    Set-Content -Path $configFile -Value 'Gap=0' -Encoding UTF8
    Set-Content -Path $sentinelFile -Value 'This file is not owned by EbenTiler.' -Encoding UTF8

    $uninstall = Start-Process -FilePath $uninstaller -ArgumentList @(
        '/VERYSILENT',
        '/SUPPRESSMSGBOXES',
        '/NORESTART'
    ) -Wait -PassThru
    if ($uninstall.ExitCode -ne 0) {
        throw "Uninstaller exited with code $($uninstall.ExitCode)."
    }

    Start-Sleep -Milliseconds 500
    if (Test-Path $installedExe) {
        throw 'Installed executable remains after uninstall.'
    }

    $startupAfter = Get-ItemPropertyValue -Path $runKey -Name 'EbenTiler' -ErrorAction SilentlyContinue
    if ($null -ne $startupAfter) {
        throw 'Windows startup registry value remains after uninstall.'
    }

    if (Test-Path $configFile) {
        throw 'EbenTiler-owned config.ini remains after uninstall.'
    }
    if (-not (Test-Path $sentinelFile)) {
        throw 'Uninstaller removed an unrelated file from the EbenTiler config directory.'
    }

    Write-Host 'Installer smoke test passed: install, CLI launch, startup on/off, safe config cleanup, uninstall.'
}
finally {
    Remove-Item $outputPath -Force -ErrorAction SilentlyContinue
    Remove-Item $sentinelFile -Force -ErrorAction SilentlyContinue
    Remove-Item $configFile -Force -ErrorAction SilentlyContinue
    if (Test-Path $configDir) {
        Remove-Item $configDir -Force -ErrorAction SilentlyContinue
    }
}
