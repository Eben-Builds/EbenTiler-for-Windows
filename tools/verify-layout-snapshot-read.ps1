# quick-layout.ini parser safety verification.
#
# Uses isolated temporary snapshot files only. It never modifies
# %APPDATA%\Tessdeck\quick-layout.ini.
#
#   powershell -ExecutionPolicy Bypass -File tools\verify-layout-snapshot-read.ps1

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$exe = Join-Path $root 'build\Tessdeck.exe'
if (-not (Test-Path $exe)) {
    throw '먼저 build.ps1 로 빌드하세요.'
}

$tempDir = Join-Path $env:TEMP ('tessdeck-layout-read-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tempDir -Force | Out-Null

function Assert-Contains {
    param(
        [string]$Text,
        [string]$Expected,
        [string]$Label
    )

    if ($Text -notlike ('*' + $Expected + '*')) {
        throw "$Label 실패: '$Expected' 를 찾지 못했습니다.`n$Text"
    }
}

function Invoke-ReadCase {
    param(
        [string]$Name,
        [string]$SnapshotContent,
        [int]$ExpectedExitCode,
        [string[]]$ExpectedLines
    )

    $snapshotPath = Join-Path $tempDir ($Name + '.ini')
    $outputPath = Join-Path $tempDir ($Name + '.out.txt')

    [IO.File]::WriteAllText(
        $snapshotPath,
        $SnapshotContent,
        [Text.UTF8Encoding]::new($false)
    )

    $args = @(
        '--layout-read-file'
        ('"' + $snapshotPath + '"')
        '--out'
        ('"' + $outputPath + '"')
    )

    $process = Start-Process -FilePath $exe -ArgumentList $args -Wait -PassThru
    Start-Sleep -Milliseconds 100

    if ($process.ExitCode -ne $ExpectedExitCode) {
        throw "$Name 종료 코드 실패: expected=$ExpectedExitCode actual=$($process.ExitCode)"
    }

    if (-not (Test-Path $outputPath)) {
        throw "$Name 출력 파일이 생성되지 않았습니다."
    }

    $text = Get-Content -LiteralPath $outputPath -Raw
    foreach ($expected in $ExpectedLines) {
        Assert-Contains -Text $text -Expected $expected -Label $Name
    }

    Write-Host "[PASS] $Name"
}

$valid = @'
; isolated parser test
[Snapshot]
Version=1
CapturedUtc=2026-10-05T03:45:19.0035729Z
WindowCount=2

[Window0]
Process=WindowsTerminal
Class=CASCADIA_HOSTING_WINDOW_CLASS
Instance=0
Monitor=\\.\DISPLAY1
X=0
Y=0
Width=5000
Height=10000
Maximized=false

[Window1]
Process=GalaxyDisplay
Class=HwndWrapper[GalaxyDisplay;;runtime-guid-that-must-not-matter]
Instance=0
Monitor=\\.\DISPLAY2
X=1927
Y=1138
Width=6146
Height=7724
Maximized=false
'@

$partial = @'
[Snapshot]
Version=1
CapturedUtc=2026-10-05T03:45:19.0035729Z
WindowCount=2

[Window0]
Process=chrome
Class=Chrome_WidgetWin_1
Instance=0
Monitor=\\.\DISPLAY2
X=0
Y=0
Width=10000
Height=10000
Maximized=true

[Window1]
Process=BrokenApp
Class=BrokenWindow
Instance=0
Monitor=\\.\DISPLAY1
X=10001
Y=0
Width=5000
Height=5000
Maximized=false
'@

$badHeader = @'
[Snapshot]
Version=999
CapturedUtc=2026-10-05T03:45:19.0035729Z
WindowCount=1

[Window0]
Process=chrome
Class=Chrome_WidgetWin_1
Instance=0
Monitor=\\.\DISPLAY2
X=0
Y=0
Width=10000
Height=10000
Maximized=true
'@

try {
    Invoke-ReadCase -Name 'valid' -SnapshotContent $valid -ExpectedExitCode 0 -ExpectedLines @(
        'exists=1',
        'loaded=1',
        'declared=2',
        'valid=2',
        'skipped=0',
        'class=HwndWrapper[GalaxyDisplay]'
    )

    Invoke-ReadCase -Name 'partial-corruption' -SnapshotContent $partial -ExpectedExitCode 0 -ExpectedLines @(
        'exists=1',
        'loaded=1',
        'declared=2',
        'valid=1',
        'skipped=1'
    )

    Invoke-ReadCase -Name 'invalid-header' -SnapshotContent $badHeader -ExpectedExitCode 1 -ExpectedLines @(
        'exists=1',
        'loaded=0',
        'error=Unsupported or invalid snapshot version.'
    )

    Write-Host ''
    Write-Host 'Layout snapshot read safety: PASS'
}
finally {
    if (Test-Path $tempDir) {
        Remove-Item -LiteralPath $tempDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}
