param(
    [switch]$SkipBuild,
    [switch]$InstallTools
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$buildScript = Join-Path $root 'build.ps1'
$issPath = Join-Path $root 'installer\EbenTiler.iss'
$exePath = Join-Path $root 'build\EbenTiler.exe'
$distDir = Join-Path $root 'dist'
$setupPath = Join-Path $distDir 'EbenTiler-Setup.exe'
$hashPath = Join-Path $distDir 'EbenTiler-Setup.exe.sha256'

function Find-Iscc {
    $command = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($command) { return $command.Source }

    $candidates = @(
        (Join-Path $env:ProgramFiles 'Inno Setup 7\ISCC.exe'),
        (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 7\ISCC.exe'),
        (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe'),
        (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
        (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 7\ISCC.exe'),
        (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe')
    )

    foreach ($candidate in $candidates) {
        if ($candidate -and (Test-Path $candidate)) { return $candidate }
    }
    return $null
}

function Find-SignTool {
    $command = Get-Command signtool.exe -ErrorAction SilentlyContinue
    if ($command) { return $command.Source }

    $roots = @(
        (Join-Path ${env:ProgramFiles(x86)} 'Windows Kits\10\bin'),
        (Join-Path $env:ProgramFiles 'Windows Kits\10\bin')
    )

    foreach ($kitsRoot in $roots) {
        if (-not $kitsRoot -or -not (Test-Path $kitsRoot)) { continue }

        $versions = Get-ChildItem -Path $kitsRoot -Directory -ErrorAction SilentlyContinue |
            Sort-Object Name -Descending
        foreach ($versionDir in $versions) {
            $candidate = Join-Path $versionDir.FullName 'x64\signtool.exe'
            if (Test-Path $candidate) { return $candidate }
        }
    }

    return $null
}

if (-not $SkipBuild) {
    & powershell -ExecutionPolicy Bypass -File $buildScript
    if ($LASTEXITCODE -ne 0) { throw 'EbenTiler build failed.' }
}

if (-not (Test-Path $exePath)) {
    throw "Build output not found: $exePath"
}

$assemblyInfo = Get-Content (Join-Path $root 'src\AssemblyInfo.cs') -Raw
$version = '1.0.0'
if ($assemblyInfo -match 'AssemblyFileVersion\("(\d+)\.(\d+)\.(\d+)\.\d+"\)') {
    $version = "$($Matches[1]).$($Matches[2]).$($Matches[3])"
}

$iscc = Find-Iscc
if (-not $iscc -and $InstallTools) {
    $winget = Get-Command winget.exe -ErrorAction SilentlyContinue
    if (-not $winget) {
        throw 'winget was not found. Install Inno Setup 7 manually and run this script again.'
    }

    Write-Host 'Installing Inno Setup 7...'
    & $winget.Source install --id JRSoftware.InnoSetup.7 -e -s winget --accept-source-agreements --accept-package-agreements --silent
    if ($LASTEXITCODE -ne 0) { throw 'Inno Setup 7 installation failed.' }
    $iscc = Find-Iscc
}

if (-not $iscc) {
    throw @"
Inno Setup compiler (ISCC.exe) was not found.
Install Inno Setup 7 once with:

winget install --id JRSoftware.InnoSetup.7 -e -s winget -i

Or run:
powershell -ExecutionPolicy Bypass -File build-installer.ps1 -InstallTools
"@
}

$certThumbprint = $env:EBENTILER_SIGNING_CERT_SHA1
$timestampUrl = $env:EBENTILER_TIMESTAMP_URL
$signingEnabled = -not [string]::IsNullOrWhiteSpace($certThumbprint)
$signTool = $null

if ($signingEnabled) {
    $certThumbprint = ($certThumbprint -replace '\s', '').ToUpperInvariant()
    if ([string]::IsNullOrWhiteSpace($timestampUrl)) {
        $timestampUrl = 'http://timestamp.digicert.com'
    }

    $signTool = Find-SignTool
    if (-not $signTool) {
        throw 'signtool.exe was not found. Install the Windows SDK before signed builds.'
    }

    Write-Host ''
    Write-Host 'Signing EbenTiler.exe...'
    & $signTool sign /sha1 $certThumbprint /fd SHA256 /tr $timestampUrl /td SHA256 /d 'EbenTiler for Windows' $exePath
    if ($LASTEXITCODE -ne 0) { throw "EbenTiler.exe signing failed (exit $LASTEXITCODE)." }

    & $signTool verify /pa /v $exePath
    if ($LASTEXITCODE -ne 0) { throw "EbenTiler.exe signature verification failed (exit $LASTEXITCODE)." }
}

if (-not (Test-Path $distDir)) {
    New-Item -ItemType Directory -Path $distDir -Force | Out-Null
}
Remove-Item $setupPath -Force -ErrorAction SilentlyContinue
Remove-Item $hashPath -Force -ErrorAction SilentlyContinue

$isccArgs = @("/DAppVersion=$version")
if ($signingEnabled) {
    $signCommand = '"' + $signTool + '" sign /sha1 ' + $certThumbprint + ' /fd SHA256 /tr "' + $timestampUrl + '" /td SHA256 /d "EbenTiler for Windows" $f'
    $isccArgs += '/DEnableSigning=1'
    $isccArgs += "/Sebentiler=$signCommand"
}
$isccArgs += $issPath

Write-Host ''
Write-Host "Compiling installer... (EbenTiler $version)"
& $iscc @isccArgs
if ($LASTEXITCODE -ne 0) { throw "Installer compilation failed (exit $LASTEXITCODE)." }

if (-not (Test-Path $setupPath)) {
    throw "Installer was not created: $setupPath"
}

if ($signingEnabled) {
    Write-Host ''
    Write-Host 'Verifying EbenTiler-Setup.exe signature...'
    & $signTool verify /pa /v $setupPath
    if ($LASTEXITCODE -ne 0) { throw "Installer signature verification failed (exit $LASTEXITCODE)." }
}

$hash = Get-FileHash -Algorithm SHA256 $setupPath
Set-Content -Path $hashPath -Value ($hash.Hash.ToLowerInvariant() + '  EbenTiler-Setup.exe') -Encoding Ascii

$size = [Math]::Round((Get-Item $setupPath).Length / 1MB, 2)
Write-Host ''
Write-Host "Done: $setupPath ($size MB)"
Write-Host "SHA256: $($hash.Hash.ToLowerInvariant())"
if ($signingEnabled) {
    Write-Host 'Authenticode: signed and verified.'
} else {
    Write-Host 'Authenticode: unsigned (no signing certificate configured).'
}
Write-Host 'Share EbenTiler-Setup.exe with end users.'
