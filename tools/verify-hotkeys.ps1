# 전역 단축키가 실제로 먹히는지 확인한다.
# Tessdeck.exe 를 상주 모드로 띄우고, 검증용 창을 활성화한 뒤 키 입력을 실제로 보낸다.
#
#   powershell -ExecutionPolicy Bypass -File tools\verify-hotkeys.ps1

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$exe  = Join-Path $root 'build\Tessdeck.exe'
if (-not (Test-Path $exe)) { throw "먼저 build.ps1 로 빌드하세요." }

Add-Type -Namespace HK -Name U -MemberDefinition @'
[DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint flags, IntPtr extra);
[DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
[DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
[DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int cmd);
[DllImport("user32.dll")] public static extern bool AttachThreadInput(uint from, uint to, bool attach);
[DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, IntPtr pid);
[DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
[DllImport("user32.dll")] public static extern bool IsZoomed(IntPtr h);
[DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
[DllImport("user32.dll")] public static extern void mouse_event(uint flags, int dx, int dy, uint data, IntPtr extra);
'@

$VK_CONTROL = 0x11
$VK_MENU    = 0x12
$KEYUP      = 0x0002

# --info 를 부를 때마다 Tessdeck.exe 가 잠깐 떠서 활성 창이 바뀔 수 있다.
# 키를 보내기 직전에 매번 대상 창을 다시 활성화한다.
function Activate-Target {
    param([IntPtr]$Handle)

    for ($try = 0; $try -lt 3; $try++) {
        [HK.U]::keybd_event(0x12, 0, 0, [IntPtr]::Zero)
        [HK.U]::keybd_event(0x12, 0, 2, [IntPtr]::Zero)

        $targetThread = [HK.U]::GetWindowThreadProcessId($Handle, [IntPtr]::Zero)
        $thisThread = [HK.U]::GetCurrentThreadId()
        [HK.U]::AttachThreadInput($thisThread, $targetThread, $true) | Out-Null
        [HK.U]::SetForegroundWindow($Handle) | Out-Null
        [HK.U]::AttachThreadInput($thisThread, $targetThread, $false) | Out-Null
        Start-Sleep -Milliseconds 200

        if ([HK.U]::GetForegroundWindow() -eq $Handle) {
            return $true
        }
    }

    $rect = Parse-Xywh (Get-Rect ('0x' + $Handle.ToInt64().ToString('X')))['window']
    $cx = $rect.X + [int]($rect.Width / 2)
    $cy = $rect.Y + [int]($rect.Height / 2)
    [HK.U]::SetCursorPos($cx, $cy) | Out-Null
    Start-Sleep -Milliseconds 80
    [HK.U]::mouse_event(0x0002, 0, 0, 0, [IntPtr]::Zero)
    [HK.U]::mouse_event(0x0004, 0, 0, 0, [IntPtr]::Zero)
    Start-Sleep -Milliseconds 250

    return ([HK.U]::GetForegroundWindow() -eq $Handle)
}

function Send-Hotkey {
    param([int]$Key)
    if (-not (Activate-Target $script:hwnd)) {
        Write-Host "  (경고) 대상 창을 활성화하지 못했습니다." -ForegroundColor Yellow
    }
    [HK.U]::keybd_event($VK_CONTROL, 0, 0, [IntPtr]::Zero)
    [HK.U]::keybd_event($VK_MENU, 0, 0, [IntPtr]::Zero)
    [HK.U]::keybd_event($Key, 0, 0, [IntPtr]::Zero)
    Start-Sleep -Milliseconds 40
    [HK.U]::keybd_event($Key, 0, $KEYUP, [IntPtr]::Zero)
    [HK.U]::keybd_event($VK_MENU, 0, $KEYUP, [IntPtr]::Zero)
    [HK.U]::keybd_event($VK_CONTROL, 0, $KEYUP, [IntPtr]::Zero)
    Start-Sleep -Milliseconds 350
}

function Get-Rect {
    param([string]$HandleArg)
    $tmp = [System.IO.Path]::GetTempFileName()
    try {
        Start-Process -FilePath $exe -ArgumentList @('--info', '--hwnd', $HandleArg, '--out', $tmp) -Wait -WindowStyle Hidden | Out-Null
        $text = [System.IO.File]::ReadAllText($tmp, [System.Text.Encoding]::UTF8)
        $result = @{}
        foreach ($line in $text -split "`r?`n") {
            if ($line -match '^(\w+)=(.*)$') { $result[$matches[1]] = $matches[2] }
        }
        return $result
    }
    finally { Remove-Item $tmp -Force -ErrorAction SilentlyContinue }
}

function Parse-Xywh {
    param([string]$Value)
    $n = $Value -split ','
    return [PSCustomObject]@{ X = [int]$n[0]; Y = [int]$n[1]; Width = [int]$n[2]; Height = [int]$n[3] }
}

Get-Process Tessdeck -ErrorAction SilentlyContinue | Stop-Process -Force
$app = Start-Process -FilePath $exe -PassThru
Start-Sleep -Seconds 2
if ($app.HasExited) { throw "Tessdeck.exe 가 바로 종료되었습니다." }
Write-Host "Tessdeck.exe 상주 실행 중 (PID $($app.Id))"

$handleFile = Join-Path $env:TEMP ("ebentiler-hk-" + [Guid]::NewGuid().ToString('N') + ".txt")
$hostScript = Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) '_testwindow.ps1'
$win = Start-Process powershell -PassThru -ArgumentList @('-ExecutionPolicy','Bypass','-NoProfile','-File',$hostScript,$handleFile)

$hwnd = [IntPtr]::Zero
for ($i = 0; $i -lt 100; $i++) {
    Start-Sleep -Milliseconds 100
    if (Test-Path $handleFile) {
        $raw = (Get-Content $handleFile -Raw).Trim()
        if ($raw.Length -gt 0) { $hwnd = [IntPtr][long]$raw; break }
    }
}
if ($hwnd -eq [IntPtr]::Zero) { throw "검증용 창 핸들을 얻지 못했습니다." }
$handleArg = '0x' + $hwnd.ToInt64().ToString('X')

[HK.U]::ShowWindow($hwnd, 9) | Out-Null
$activated = $false
for ($i = 0; $i -lt 10; $i++) {
    if (Activate-Target $hwnd) { $activated = $true; break }
    Start-Sleep -Milliseconds 300
}
if (-not $activated) {
    $fg = [HK.U]::GetForegroundWindow()
    $win | Stop-Process -Force -ErrorAction SilentlyContinue
    $app | Stop-Process -Force -ErrorAction SilentlyContinue
    throw "검증용 창을 활성화하지 못했습니다. (활성 창 핸들 $fg)"
}
Write-Host "검증용 창 활성화 완료: $handleArg"
Write-Host ""

$info = Get-Rect $handleArg
$work = Parse-Xywh $info['work']
$pass = 0
$fail = 0

function Check {
    param([string]$Name, [int]$Key, [int]$EX, [int]$EY, [int]$EW, [int]$EH)
    Send-Hotkey $Key
    $r = Parse-Xywh (Get-Rect $handleArg)['window']
    if ([Math]::Abs($r.X - $EX) -le 2 -and [Math]::Abs($r.Y - $EY) -le 2 -and
        [Math]::Abs($r.Width - $EW) -le 2 -and [Math]::Abs($r.Height - $EH) -le 2) {
        Write-Host ("[통과] {0,-22} {1},{2} {3}x{4}" -f $Name, $r.X, $r.Y, $r.Width, $r.Height) -ForegroundColor Green
        $script:pass++
    } else {
        Write-Host ("[실패] {0,-22} 실제 {1},{2} {3}x{4} / 기대 {5},{6} {7}x{8}" -f `
            $Name, $r.X, $r.Y, $r.Width, $r.Height, $EX, $EY, $EW, $EH) -ForegroundColor Red
        $script:fail++
    }
}

$hw = [int][Math]::Round($work.Width * 0.5)
$hh = [int][Math]::Round($work.Height * 0.5)
$qw = [int][Math]::Truncate($work.Width / 2)
$qh = [int][Math]::Truncate($work.Height / 2)
$third = [int][Math]::Truncate($work.Width / 3)

Check 'Ctrl+Alt+Left'  0x25 $work.X $work.Y $hw $work.Height
Check 'Ctrl+Alt+Right' 0x27 ($work.X + $work.Width - $hw) $work.Y $hw $work.Height
Check 'Ctrl+Alt+Up'    0x26 $work.X $work.Y $work.Width $hh
Check 'Ctrl+Alt+Down'  0x28 $work.X ($work.Y + $work.Height - $hh) $work.Width $hh
Check 'Ctrl+Alt+U'     0x55 $work.X $work.Y $qw $qh
Check 'Ctrl+Alt+I'     0x49 ($work.X + $qw) $work.Y ($work.Width - $qw) $qh
Check 'Ctrl+Alt+J'     0x4A $work.X ($work.Y + $qh) $qw ($work.Height - $qh)
Check 'Ctrl+Alt+K'     0x4B ($work.X + $qw) ($work.Y + $qh) ($work.Width - $qw) ($work.Height - $qh)
Check 'Ctrl+Alt+D'     0x44 $work.X $work.Y $third $work.Height

Send-Hotkey 0x25
$c1 = (Parse-Xywh (Get-Rect $handleArg)['window']).Width
Send-Hotkey 0x25
$c2 = (Parse-Xywh (Get-Rect $handleArg)['window']).Width
Send-Hotkey 0x25
$c3 = (Parse-Xywh (Get-Rect $handleArg)['window']).Width
if ($c1 -gt $c2 -and $c3 -gt $c1) {
    Write-Host ("[통과] 폭 순환 (1/2->1/3->2/3)  {0} -> {1} -> {2}" -f $c1, $c2, $c3) -ForegroundColor Green
    $pass++
} else {
    Write-Host ("[실패] 폭 순환                  {0} -> {1} -> {2}" -f $c1, $c2, $c3) -ForegroundColor Red
    $fail++
}

Send-Hotkey 0x0D
if ([HK.U]::IsZoomed($hwnd)) {
    Write-Host "[통과] Ctrl+Alt+Enter          최대화됨" -ForegroundColor Green
    $pass++
} else {
    Write-Host "[실패] Ctrl+Alt+Enter          최대화되지 않음" -ForegroundColor Red
    $fail++
}

Send-Hotkey 0x08
$restored = Parse-Xywh (Get-Rect $handleArg)['window']
if (-not [HK.U]::IsZoomed($hwnd)) {
    Write-Host ("[통과] Ctrl+Alt+Backspace      복원됨 {0},{1} {2}x{3}" -f $restored.X, $restored.Y, $restored.Width, $restored.Height) -ForegroundColor Green
    $pass++
} else {
    Write-Host "[실패] Ctrl+Alt+Backspace      여전히 최대화" -ForegroundColor Red
    $fail++
}

Write-Host ""
Write-Host ("결과: 통과 {0} / 실패 {1}" -f $pass, $fail)

$win | Stop-Process -Force -ErrorAction SilentlyContinue
$app | Stop-Process -Force -ErrorAction SilentlyContinue
Remove-Item $handleFile -Force -ErrorAction SilentlyContinue
Write-Host "정리 완료 (검증용 창과 Tessdeck.exe 종료)"

if ($fail -gt 0) { exit 1 } else { exit 0 }
