param(
    [string]$Installer = '.\dist\EbenTiler-Setup.exe'
)

$ErrorActionPreference = 'Stop'

$installerPath = [IO.Path]::GetFullPath((Join-Path (Get-Location) $Installer))
$installDir = Join-Path $env:LOCALAPPDATA 'Programs\EbenTiler'
$installedExe = Join-Path $installDir 'EbenTiler.exe'
$uninstaller = Join-Path $installDir 'unins000.exe'
$runKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
$outputPath = Join-Path $env:TEMP ('ebentiler-smoke-' + [Guid]::NewGuid().ToString('N') + '.txt')

if (-not (Test-Path $installerPath)) {
    throw "Installer not found: $installerPath"
}
if (Test-Path $installDir) {
    throw "Smoke test requires a clean install directory: $installDir"
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

    if (-not (Test-Path $uninstaller)) {
        throw "Uninstaller not found: $uninstaller"
    }

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

    Write-Host 'Installer smoke test passed: install, CLI launch, startup check, uninstall.'
}
finally {
    Remove-Item $outputPath -Force -ErrorAction SilentlyContinue
}
