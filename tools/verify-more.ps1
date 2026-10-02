# 나머지 기능 검증: 설정 파일 반영, 여백, 세로만 최대, 다중 모니터 이동,
# 중복 실행 방지, 자동 실행 등록, 첫 실행 시 설정 파일 생성.
#
#   powershell -ExecutionPolicy Bypass -File tools\verify-more.ps1
#
# 검사 도중 사용자의 실제 설정 파일과 자동 실행 등록 상태는 백업했다가 그대로 되돌린다.

$ErrorActionPreference = 'Stop'

$toolsDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Split-Path -Parent $toolsDir
$exe  = Join-Path $root 'build\EbenTiler.exe'
if (-not (Test-Path $exe)) { throw "먼저 build.ps1 로 빌드하세요." }

Add-Type -AssemblyName System.Windows.Forms
Add-Type -Namespace More -Name U -MemberDefinition @'
[DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr value);
[DllImport("user32.dll")] public static extern bool IsZoomed(IntPtr h);
'@
try { [More.U]::SetProcessDpiAwarenessContext([IntPtr](-4)) | Out-Null } catch { }

$configDir  = Join-Path $env:APPDATA 'EbenTiler'
$configPath = Join-Path $configDir 'config.ini'
$backupPath = Join-Path $env:TEMP ('ebentiler-config-backup-' + [Guid]::NewGuid().ToString('N') + '.ini')
$hadConfig  = Test-Path $configPath
if ($hadConfig) { Copy-Item $configPath $backupPath -Force }

$runKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
$hadStartup = $null -ne (Get-ItemProperty $runKey -ErrorAction SilentlyContinue).EbenTiler

$pass = 0
$fail = 0
function Ok   { param([string]$m) Write-Host "[통과] $m" -ForegroundColor Green; $script:pass++ }
function Bad  { param([string]$m) Write-Host "[실패] $m" -ForegroundColor Red;   $script:fail++ }
function Skip { param([string]$m) Write-Host "[생략] $m" -ForegroundColor Yellow }

function Invoke-EbenTiler {
    param([string[]]$Arguments)
    $tmp = [System.IO.Path]::GetTempFileName()
    try {
        Start-Process -FilePath $exe -ArgumentList ($Arguments + @('--out', $tmp)) -Wait -WindowStyle Hidden | Out-Null
        if (Test-Path $tmp) { return [System.IO.File]::ReadAllText($tmp, [System.Text.Encoding]::UTF8) }
        return ''
    } finally { Remove-Item $tmp -Force -ErrorAction SilentlyContinue }
}

function Get-Field {
    param([string]$Text, [string]$Name)
    foreach ($line in $Text -split "`r?`n") {
        if ($line.Trim() -match "^$Name=(.*)$") { return $matches[1] }
    }
    return $null
}

function Parse-Xywh {
    param([string]$Value)
    $n = $Value -split ','
    return [PSCustomObject]@{ X = [int]$n[0]; Y = [int]$n[1]; Width = [int]$n[2]; Height = [int]$n[3] }
}

function New-TestWindow {
    $file = Join-Path $env:TEMP ("ebentiler-more-" + [Guid]::NewGuid().ToString('N') + ".txt")
    $hostScript = Join-Path $script:toolsDir '_testwindow.ps1'
    $p = Start-Process powershell -PassThru -ArgumentList @('-ExecutionPolicy','Bypass','-NoProfile','-File',$hostScript,$file)
    $h = [IntPtr]::Zero
    for ($k = 0; $k -lt 100; $k++) {
        Start-Sleep -Milliseconds 100
        if (Test-Path $file) {
            $raw = (Get-Content $file -Raw).Trim()
            if ($raw.Length -gt 0) { $h = [IntPtr][long]$raw; break }
        }
    }
    Remove-Item $file -Force -ErrorAction SilentlyContinue
    if ($h -eq [IntPtr]::Zero) { throw "검증용 창 핸들을 얻지 못했습니다." }
    return [PSCustomObject]@{ Process = $p; Handle = $h; Arg = ('0x' + $h.ToInt64().ToString('X')) }
}

try {
    # ── 1. 첫 실행 시 설정 파일이 만들어지는지 ──────────────────────
    Get-Process EbenTiler -ErrorAction SilentlyContinue | Stop-Process -Force
    if (Test-Path $configPath) { Remove-Item $configPath -Force }

    $app = Start-Process -FilePath $exe -PassThru
    Start-Sleep -Seconds 3
    if (Test-Path $configPath) {
        Ok "첫 실행 시 설정 파일 생성: $configPath"
    } else {
        Bad "첫 실행인데 설정 파일이 만들어지지 않음"
    }
    $app | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 500

    $saved = Get-Content $configPath -Raw
    if ($saved -match 'LeftHalf=Ctrl\+Alt\+Left' -and $saved -match 'CycleHalves=') {
        Ok "설정 파일에 단축키와 옵션이 제대로 기록됨"
    } else {
        Bad "설정 파일 내용이 예상과 다름"
    }

    # ── 2. 창 사이 여백(Gap) 옵션이 실제 배치에 반영되는지 ──────────
    $win = New-TestWindow
    try {
        $info = Invoke-EbenTiler @('--info', '--hwnd', $win.Arg)
        $work = Parse-Xywh (Get-Field $info 'work')

        (Get-Content $configPath -Raw) -replace 'Gap=\d+', 'Gap=0' | Set-Content $configPath -Encoding UTF8
        Invoke-EbenTiler @('--apply', 'LeftHalf', '--hwnd', $win.Arg) | Out-Null
        Start-Sleep -Milliseconds 200
        $noGap = Parse-Xywh (Get-Field (Invoke-EbenTiler @('--info', '--hwnd', $win.Arg)) 'window')

        (Get-Content $configPath -Raw) -replace 'Gap=\d+', 'Gap=20' | Set-Content $configPath -Encoding UTF8
        Invoke-EbenTiler @('--apply', 'LeftHalf', '--hwnd', $win.Arg) | Out-Null
        Start-Sleep -Milliseconds 200
        $gapped = Parse-Xywh (Get-Field (Invoke-EbenTiler @('--info', '--hwnd', $win.Arg)) 'window')

        if ($gapped.X -eq ($noGap.X + 20) -and $gapped.Y -eq ($noGap.Y + 20) -and
            $gapped.Width -lt $noGap.Width -and $gapped.Height -lt $noGap.Height) {
            Ok ("여백 옵션 반영: 여백0 {0},{1} {2}x{3} -> 여백20 {4},{5} {6}x{7}" -f `
                $noGap.X, $noGap.Y, $noGap.Width, $noGap.Height, $gapped.X, $gapped.Y, $gapped.Width, $gapped.Height)
        } else {
            Bad ("여백 옵션이 반영되지 않음: 여백0 {0},{1} {2}x{3} / 여백20 {4},{5} {6}x{7}" -f `
                $noGap.X, $noGap.Y, $noGap.Width, $noGap.Height, $gapped.X, $gapped.Y, $gapped.Width, $gapped.Height)
        }

        (Get-Content $configPath -Raw) -replace 'Gap=\d+', 'Gap=0' | Set-Content $configPath -Encoding UTF8

        # ── 3. 세로만 최대 ──────────────────────────────────────────
        Invoke-EbenTiler @('--apply', 'Center', '--hwnd', $win.Arg) | Out-Null
        Start-Sleep -Milliseconds 200
        $before = Parse-Xywh (Get-Field (Invoke-EbenTiler @('--info', '--hwnd', $win.Arg)) 'window')
        Invoke-EbenTiler @('--apply', 'MaximizeHeight', '--hwnd', $win.Arg) | Out-Null
        Start-Sleep -Milliseconds 200
        $tall = Parse-Xywh (Get-Field (Invoke-EbenTiler @('--info', '--hwnd', $win.Arg)) 'window')

        if ($tall.Height -eq $work.Height -and $tall.Y -eq $work.Y -and
            [Math]::Abs($tall.Width - $before.Width) -le 2 -and [Math]::Abs($tall.X - $before.X) -le 2) {
            Ok ("세로만 최대: 폭 {0} 유지, 높이 {1} 로 꽉 참" -f $tall.Width, $tall.Height)
        } else {
            Bad ("세로만 최대 실패: 실제 {0},{1} {2}x{3} / 폭은 {4} 유지, 높이는 {5} 여야 함" -f `
                $tall.X, $tall.Y, $tall.Width, $tall.Height, $before.Width, $work.Height)
        }

        # ── 4. 다중 모니터 이동 ─────────────────────────────────────
        $screens = [System.Windows.Forms.Screen]::AllScreens | Sort-Object { $_.Bounds.X }
        if ($screens.Count -lt 2) {
            Skip "모니터가 하나뿐이라 모니터 이동은 확인할 수 없음"
        } else {
            Invoke-EbenTiler @('--apply', 'LeftHalf', '--hwnd', $win.Arg) | Out-Null
            Start-Sleep -Milliseconds 200
            $start = Parse-Xywh (Get-Field (Invoke-EbenTiler @('--info', '--hwnd', $win.Arg)) 'window')

            $visited = @()
            $moveOk = $true
            for ($m = 1; $m -lt $screens.Count; $m++) {
                Invoke-EbenTiler @('--apply', 'NextDisplay', '--hwnd', $win.Arg) | Out-Null
                Start-Sleep -Milliseconds 400
                $now = Parse-Xywh (Get-Field (Invoke-EbenTiler @('--info', '--hwnd', $win.Arg)) 'window')
                $expected = $screens[$m].WorkingArea
                $inside = ($now.X -ge $expected.X - 4) -and (($now.X + $now.Width) -le ($expected.X + $expected.Width + 4))
                $visited += ("{0}: {1},{2} {3}x{4}" -f $screens[$m].DeviceName, $now.X, $now.Y, $now.Width, $now.Height)
                if (-not $inside) { $moveOk = $false }
            }

            if ($moveOk) {
                Ok ("다음 모니터로 이동 ({0}대): {1}" -f $screens.Count, ($visited -join '  |  '))
            } else {
                Bad ("모니터 이동이 대상 화면 범위를 벗어남: {0}" -f ($visited -join '  |  '))
            }

            Invoke-EbenTiler @('--apply', 'PreviousDisplay', '--hwnd', $win.Arg) | Out-Null
            Start-Sleep -Milliseconds 400
            $back = Parse-Xywh (Get-Field (Invoke-EbenTiler @('--info', '--hwnd', $win.Arg)) 'window')
            $prevScreen = $screens[$screens.Count - 2].WorkingArea
            if ($back.X -ge $prevScreen.X - 4 -and ($back.X + $back.Width) -le ($prevScreen.X + $prevScreen.Width + 4)) {
                Ok ("이전 모니터로 이동: {0},{1} {2}x{3}" -f $back.X, $back.Y, $back.Width, $back.Height)
            } else {
                Bad ("이전 모니터 이동 실패: {0},{1} {2}x{3}" -f $back.X, $back.Y, $back.Width, $back.Height)
            }
        }

        # ── 5. 설정 파일에 적은 사용자 단축키가 읽히는지 ────────────
        $text = Get-Content $configPath -Raw
        $text = $text -replace 'LeftHalf=Ctrl\+Alt\+Left', 'LeftHalf=Ctrl+Alt+Shift+Oem5'
        Set-Content $configPath $text -Encoding UTF8
        $reloaded = Get-Content $configPath -Raw
        if ($reloaded -match 'LeftHalf=Ctrl\+Alt\+Shift\+Oem5') {
            $app2 = Start-Process -FilePath $exe -PassThru
            Start-Sleep -Seconds 2
            if (-not $app2.HasExited) {
                Ok "설정 파일에 직접 적은 단축키로 정상 기동"
            } else {
                Bad "바꾼 설정 파일로 실행했더니 프로그램이 종료됨"
            }

            # ── 6. 중복 실행 방지 ───────────────────────────────────
            $app3 = Start-Process -FilePath $exe -PassThru
            Start-Sleep -Seconds 2
            $app3.Refresh()
            if (-not $app3.HasExited -and $app3.MainWindowTitle -like '*EbenTiler*') {
                Ok "두 번째 실행 시 안내 창을 띄우고 중복 상주하지 않음"
            } elseif ($app3.HasExited) {
                Ok "두 번째 실행이 그대로 종료됨 (중복 상주 없음)"
            } else {
                Bad "중복 실행 방지가 동작하지 않는 것으로 보임"
            }
            $app3 | Stop-Process -Force -ErrorAction SilentlyContinue
            $app2 | Stop-Process -Force -ErrorAction SilentlyContinue
        } else {
            Bad "설정 파일 수정이 반영되지 않음"
        }
    }
    finally {
        $win.Process | Stop-Process -Force -ErrorAction SilentlyContinue
    }

    # ── 7. Windows 자동 실행 등록 ───────────────────────────────────
    $on = Get-Field (Invoke-EbenTiler @('--startup', 'on')) 'startup'
    $regValue = (Get-ItemProperty $runKey -ErrorAction SilentlyContinue).EbenTiler
    if ($on -eq 'on' -and $regValue -like "*EbenTiler.exe*") {
        Ok "자동 실행 등록됨: $regValue"
    } else {
        Bad "자동 실행 등록 실패 (보고값 '$on', 레지스트리 '$regValue')"
    }

    $off = Get-Field (Invoke-EbenTiler @('--startup', 'off')) 'startup'
    $regValue2 = (Get-ItemProperty $runKey -ErrorAction SilentlyContinue).EbenTiler
    if ($off -eq 'off' -and $null -eq $regValue2) {
        Ok "자동 실행 해제됨"
    } else {
        Bad "자동 실행 해제 실패 (보고값 '$off', 레지스트리 '$regValue2')"
    }
}
finally {
    Get-Process EbenTiler -ErrorAction SilentlyContinue | Stop-Process -Force

    if ($hadConfig) {
        Copy-Item $backupPath $configPath -Force
        Remove-Item $backupPath -Force -ErrorAction SilentlyContinue
    } elseif (Test-Path $configPath) {
        Remove-Item $configPath -Force
    }

    if ($hadStartup) {
        Invoke-EbenTiler @('--startup', 'on') | Out-Null
    } else {
        Remove-ItemProperty $runKey -Name EbenTiler -ErrorAction SilentlyContinue
    }
    Write-Host ""
    Write-Host "원래 설정 상태로 되돌렸습니다."
}

Write-Host ""
Write-Host ("결과: 통과 {0} / 실패 {1}" -f $pass, $fail)
if ($fail -gt 0) { exit 1 } else { exit 0 }