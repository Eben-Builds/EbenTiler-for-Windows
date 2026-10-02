# RumiFlow for Windows 설치 / 제거
#
#   powershell -ExecutionPolicy Bypass -File install.ps1              설치
#   powershell -ExecutionPolicy Bypass -File install.ps1 -NoStartup   설치하되 자동 실행은 끄기
#   powershell -ExecutionPolicy Bypass -File install.ps1 -Uninstall   제거
#
# 관리자 권한이 필요 없다. 현재 사용자 계정에만 설치된다.

param(
    [switch]$Uninstall,
    [switch]$NoStartup,
    [switch]$KeepConfig
)

$ErrorActionPreference = 'Stop'

$root       = Split-Path -Parent $MyInvocation.MyCommand.Path
$builtExe   = Join-Path $root 'build\RumiFlow.exe'
$installDir = Join-Path $env:LOCALAPPDATA 'Programs\RumiFlow'
$installExe = Join-Path $installDir 'RumiFlow.exe'
$startMenu  = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs'
$shortcut   = Join-Path $startMenu 'RumiFlow for Windows.lnk'
$configDir  = Join-Path $env:APPDATA 'RumiFlow'

function Stop-Running {
    $running = Get-Process RumiFlow -ErrorAction SilentlyContinue
    if ($running) {
        $running | Stop-Process -Force
        Start-Sleep -Milliseconds 700
        Write-Host "실행 중이던 RumiFlow 을 종료했습니다."
    }
}

if ($Uninstall) {
    Write-Host "RumiFlow for Windows 를 제거합니다."
    Stop-Running

    if (Test-Path $installExe) {
        # 자동 실행 등록은 프로그램 자신이 지우게 한다.
        Start-Process -FilePath $installExe -ArgumentList @('--startup', 'off') -Wait -WindowStyle Hidden | Out-Null
    }
    Remove-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name RumiFlow -ErrorAction SilentlyContinue

    if (Test-Path $shortcut)   { Remove-Item $shortcut -Force;            Write-Host "시작 메뉴 바로 가기 삭제" }
    if (Test-Path $installDir) { Remove-Item $installDir -Recurse -Force; Write-Host "프로그램 폴더 삭제: $installDir" }

    if (-not $KeepConfig -and (Test-Path $configDir)) {
        Remove-Item $configDir -Recurse -Force
        Write-Host "설정 폴더 삭제: $configDir"
    } elseif (Test-Path $configDir) {
        Write-Host "설정은 그대로 두었습니다: $configDir"
    }

    Write-Host ""
    Write-Host "제거 완료."
    exit 0
}

# ── 설치 ────────────────────────────────────────────────────────────
if (-not (Test-Path $builtExe)) {
    Write-Host "빌드된 파일이 없어 먼저 빌드합니다."
    & powershell -ExecutionPolicy Bypass -File (Join-Path $root 'build.ps1')
    if (-not (Test-Path $builtExe)) { throw "빌드에 실패했습니다." }
}

Stop-Running

if (-not (Test-Path $installDir)) {
    New-Item -ItemType Directory -Path $installDir -Force | Out-Null
}
Copy-Item $builtExe $installExe -Force
Write-Host "설치 위치: $installExe"

# 시작 메뉴 바로 가기
$shell = New-Object -ComObject WScript.Shell
$link = $shell.CreateShortcut($shortcut)
$link.TargetPath = $installExe
$link.WorkingDirectory = $installDir
$link.Description = "단축키로 창을 빠르게 배치"
$link.Save()
Write-Host "시작 메뉴 바로 가기: $shortcut"

# 윈도우 시작 시 자동 실행
if ($NoStartup) {
    Start-Process -FilePath $installExe -ArgumentList @('--startup', 'off') -Wait -WindowStyle Hidden | Out-Null
    Write-Host "자동 실행: 등록하지 않음"
} else {
    Start-Process -FilePath $installExe -ArgumentList @('--startup', 'on') -Wait -WindowStyle Hidden | Out-Null
    Write-Host "자동 실행: 등록됨 (윈도우 켤 때 같이 실행)"
}

# 단축키 충돌 확인
$tmp = [System.IO.Path]::GetTempFileName()
Start-Process -FilePath $installExe -ArgumentList @('--check', '--out', $tmp) -Wait -WindowStyle Hidden | Out-Null
$check = [System.IO.File]::ReadAllText($tmp, [System.Text.Encoding]::UTF8)
Remove-Item $tmp -Force -ErrorAction SilentlyContinue

$conflicts = ($check -split "`r?`n") | Where-Object { $_ -like 'conflict=*' }
if ($conflicts) {
    Write-Host ""
    Write-Host "아래 단축키는 다른 프로그램이 이미 쓰고 있어 등록되지 않습니다:" -ForegroundColor Yellow
    foreach ($c in $conflicts) { Write-Host ("  - " + $c.Substring(9)) -ForegroundColor Yellow }
    Write-Host "  알림 영역 아이콘 > 단축키 설정 에서 다른 조합으로 바꾸세요." -ForegroundColor Yellow
} else {
    Write-Host "단축키 충돌 없음 (21개 모두 등록 가능)"
}

# 실행
Start-Process -FilePath $installExe | Out-Null
Start-Sleep -Seconds 2
if (Get-Process RumiFlow -ErrorAction SilentlyContinue) {
    Write-Host ""
    Write-Host "설치 완료. 지금 실행 중입니다."
    Write-Host "알림 영역(작업표시줄 오른쪽 ∧) 안에 아이콘이 있습니다."
    Write-Host "Ctrl+Alt+방향키 로 창을 배치해 보세요."
} else {
    throw "설치는 됐지만 실행에 실패했습니다."
}
