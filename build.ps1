# EbenTiler for Windows 빌드 스크립트
# .NET SDK 없이 Windows 에 기본 포함된 .NET Framework 4.8 컴파일러로 바로 빌드한다.
#
#   powershell -ExecutionPolicy Bypass -File build.ps1
#
# 결과물: build\EbenTiler.exe (단일 실행 파일, 별도 런타임 설치 불필요)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$srcDir = Join-Path $root 'src'
$outDir = Join-Path $root 'build'
$exePath = Join-Path $outDir 'EbenTiler.exe'
$iconPath = Join-Path $root 'assets\app.ico'
$iconScript = Join-Path $root 'tools\make-appicon.ps1'

$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) {
    $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
if (-not (Test-Path $csc)) {
    throw ".NET Framework 컴파일러(csc.exe)를 찾지 못했습니다. Windows 기능에서 .NET Framework 4.x 를 켜 주세요."
}

if (-not (Test-Path $outDir)) {
    New-Item -ItemType Directory -Path $outDir | Out-Null
}

# 웹 파비콘과 동일한 디자인을 단일 소스로 유지하기 위해 빌드 때 앱 아이콘을 재생성한다.
if (-not (Test-Path $iconScript)) {
    throw "앱 아이콘 생성 스크립트를 찾지 못했습니다: $iconScript"
}
& powershell -NoProfile -ExecutionPolicy Bypass -File $iconScript -NoPreview
if ($LASTEXITCODE -ne 0 -or -not (Test-Path $iconPath)) {
    throw "앱 아이콘 생성에 실패했습니다."
}

$sources = Get-ChildItem -Path $srcDir -Filter *.cs | ForEach-Object { $_.FullName }
if ($sources.Count -eq 0) {
    throw "src 폴더에 소스 파일이 없습니다."
}

$cscArgs = @(
    '/nologo'
    '/target:winexe'
    '/platform:x64'
    '/optimize+'
    '/warn:4'
    '/warnaserror-'
    "/out:$exePath"
    "/win32icon:$iconPath"
    '/reference:System.dll'
    '/reference:System.Core.dll'
    '/reference:System.Drawing.dll'
    '/reference:System.Windows.Forms.dll'
) + $sources

Write-Host "컴파일 중..."
& $csc $cscArgs
if ($LASTEXITCODE -ne 0) {
    throw "컴파일 실패 (exit $LASTEXITCODE)"
}

$size = [Math]::Round((Get-Item $exePath).Length / 1KB, 1)
Write-Host ""
Write-Host "빌드 완료: $exePath  ($size KB)"
Write-Host "실행하려면: $exePath"
