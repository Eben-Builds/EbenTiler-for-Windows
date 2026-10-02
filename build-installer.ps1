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

if (-not (Test-Path $distDir)) {
    New-Item -ItemType Directory -Path $distDir -Force | Out-Null
}
Remove-Item $setupPath -Force -ErrorAction SilentlyContinue
Remove-Item $hashPath -Force -ErrorAction SilentlyContinue

Write-Host ''
Write-Host "Compiling installer... (EbenTiler $version)"
& $iscc "/DAppVersion=$version" $issPath
if ($LASTEXITCODE -ne 0) { throw "Installer compilation failed (exit $LASTEXITCODE)." }

if (-not (Test-Path $setupPath)) {
    throw "Installer was not created: $setupPath"
}

$hash = Get-FileHash -Algorithm SHA256 $setupPath
Set-Content -Path $hashPath -Value ($hash.Hash.ToLowerInvariant() + '  EbenTiler-Setup.exe') -Encoding Ascii

$size = [Math]::Round((Get-Item $setupPath).Length / 1MB, 2)
Write-Host ''
Write-Host "Done: $setupPath ($size MB)"
Write-Host "SHA256: $($hash.Hash.ToLowerInvariant())"
Write-Host 'Share EbenTiler-Setup.exe with end users.'
