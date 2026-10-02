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
    if ($LASTEXITCODE -ne 0) { throw "EbenTiler 빌드에 실패했습니다." }
}

if (-not (Test-Path $exePath)) {
    throw "빌드 결과가 없습니다: $exePath"
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
        throw "winget 을 찾지 못했습니다. Inno Setup 7을 직접 설치한 뒤 다시 실행하세요."
    }

    Write-Host "Inno Setup 7 설치 중..."
    & $winget.Source install --id JRSoftware.InnoSetup.7 -e -s winget --accept-source-agreements --accept-package-agreements --silent
    if ($LASTEXITCODE -ne 0) { throw "Inno Setup 7 설치에 실패했습니다." }
    $iscc = Find-Iscc
}

if (-not $iscc) {
    throw @"
Inno Setup 컴파일러(ISCC.exe)를 찾지 못했습니다.
처음 한 번만 아래 명령으로 Inno Setup 7을 설치하거나,
이 스크립트를 -InstallTools 옵션과 함께 실행하세요.

winget install --id JRSoftware.InnoSetup.7 -e -s winget -i

예: powershell -ExecutionPolicy Bypass -File build-installer.ps1 -InstallTools
"@
}

if (-not (Test-Path $distDir)) {
    New-Item -ItemType Directory -Path $distDir -Force | Out-Null
}
Remove-Item $setupPath -Force -ErrorAction SilentlyContinue
Remove-Item $hashPath -Force -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "인스톨러 컴파일 중... (EbenTiler $version)"
& $iscc "/DAppVersion=$version" $issPath
if ($LASTEXITCODE -ne 0) { throw "인스톨러 컴파일 실패 (exit $LASTEXITCODE)" }

if (-not (Test-Path $setupPath)) {
    throw "인스톨러가 생성되지 않았습니다: $setupPath"
}

$hash = Get-FileHash -Algorithm SHA256 $setupPath
Set-Content -Path $hashPath -Value ($hash.Hash.ToLowerInvariant() + '  EbenTiler-Setup.exe') -Encoding Ascii

$size = [Math]::Round((Get-Item $setupPath).Length / 1MB, 2)
Write-Host ""
Write-Host "완료: $setupPath ($size MB)"
Write-Host "SHA256: $($hash.Hash.ToLowerInvariant())"
Write-Host "지인에게는 EbenTiler-Setup.exe 파일만 전달하면 됩니다."
