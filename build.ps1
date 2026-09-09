# Rectangle for Windows 빌드 스크립트
# .NET SDK 없이 Windows 에 기본 포함된 .NET Framework 4.8 컴파일러로 바로 빌드한다.
#
#   powershell -ExecutionPolicy Bypass -File build.ps1
#
# 결과물: build\Rectangle.exe (단일 실행 파일, 별도 런타임 설치 불필요)

$ErrorActionPreference = 'Stop'

$root      = Split-Path -Parent $MyInvocation.MyCommand.Path
$srcDir    = Join-Path $root 'src'
$outDir    = Join-Path $root 'build'
$exePath   = Join-Path $outDir 'Rectangle.exe'

# 정식 아이콘이 있으면 그것을 쓴다. 없으면 아래에서 임시 아이콘을 그려 만든다.
$assetIcon = Join-Path $root 'assets\app.ico'
if (Test-Path $assetIcon) {
    $iconPath = $assetIcon
} else {
    $iconPath = Join-Path $outDir 'app.ico'
}

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

# 실행 파일 아이콘을 그려서 만든다 (별도 이미지 파일이 필요 없도록).
if (-not (Test-Path $iconPath)) {
    Add-Type -AssemblyName System.Drawing
    $bmp = New-Object System.Drawing.Bitmap 32, 32
    $g   = [System.Drawing.Graphics]::FromImage($bmp)
    $g.Clear([System.Drawing.Color]::Transparent)

    $fill   = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 245, 245, 245))
    $accent = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 35, 35, 35))
    $pen    = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 35, 35, 35)), 2

    $g.FillRectangle($fill, 3, 5, 26, 22)
    $g.FillRectangle($accent, 4, 6, 12, 20)
    $g.DrawRectangle($pen, 3, 5, 26, 22)
    $g.DrawLine($pen, 16, 5, 16, 27)

    $g.Dispose()
    $hicon = $bmp.GetHicon()
    $icon  = [System.Drawing.Icon]::FromHandle($hicon)
    $fs    = [System.IO.File]::Create($iconPath)
    $icon.Save($fs)
    $fs.Close()
    $icon.Dispose()
    $bmp.Dispose()
    Write-Host "아이콘 생성: $iconPath"
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
