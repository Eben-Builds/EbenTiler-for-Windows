# 설정 창을 실제 마우스 클릭과 키 입력으로 조작해서,
# 단축키 변경이 설정 파일까지 반영되는지 확인한다.
#
#   powershell -ExecutionPolicy Bypass -File tools\verify-settings.ps1

$ErrorActionPreference = 'Stop'

$toolsDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Split-Path -Parent $toolsDir
$exe  = Join-Path $root 'build\Tessdeck.exe'
if (-not (Test-Path $exe)) { throw "먼저 build.ps1 로 빌드하세요." }

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -Namespace SetUI -Name U -MemberDefinition @'
[DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr value);
[DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint flags, IntPtr extra);
[DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
[DllImport("user32.dll")] public static extern bool GetCursorPos(out int x, out int y);
[DllImport("user32.dll")] public static extern void mouse_event(uint flags, int dx, int dy, uint data, IntPtr extra);
[DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
[DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
[DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);
[DllImport("user32.dll")] public static extern bool AttachThreadInput(uint from, uint to, bool attach);
[DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, IntPtr pid);
[DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
'@
try { [SetUI.U]::SetProcessDpiAwarenessContext([IntPtr](-4)) | Out-Null } catch { }

$configDir = Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::ApplicationData)) 'Tessdeck'
$configPath = Join-Path $configDir 'config.ini'
$backupDir = Join-Path $env:TEMP ('tessdeck-settings-backup-' + [Guid]::NewGuid().ToString('N'))
$hadConfigDir = Test-Path $configDir

if ($hadConfigDir) {
    New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
    Get-ChildItem -LiteralPath $configDir -Force -ErrorAction SilentlyContinue | ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination $backupDir -Recurse -Force
    }
}

$cursorX = 0; $cursorY = 0
[SetUI.U]::GetCursorPos([ref]$cursorX, [ref]$cursorY) | Out-Null

$pass = 0
$fail = 0
function Ok  { param([string]$m) Write-Host "[통과] $m" -ForegroundColor Green; $script:pass++ }
function Bad { param([string]$m) Write-Host "[실패] $m" -ForegroundColor Red;   $script:fail++ }

$auto  = [System.Windows.Automation.AutomationElement]
$scope = [System.Windows.Automation.TreeScope]::Descendants

function Find-Element {
    param($Window, [string]$Name)
    $c = New-Object System.Windows.Automation.PropertyCondition ($auto::NameProperty, $Name)
    return $Window.FindFirst($scope, $c)
}

function Get-AllElements {
    param($Window)
    return $Window.FindAll($scope, [System.Windows.Automation.Condition]::TrueCondition)
}

function Find-ListView {
    param($Window)
    $best = $null
    $bestArea = 0
    foreach ($e in (Get-AllElements $Window)) {
        if ($e.Current.Name -ne '') { continue }
        $r = $e.Current.BoundingRectangle
        $area = $r.Width * $r.Height
        if ($area -gt $bestArea) { $bestArea = $area; $best = $e }
    }
    return $best
}

function Find-CaptureBox {
    param($Window)
    $label = Find-Element $Window '새 단축키'
    if ($null -eq $label) { return $null }
    $ly = $label.Current.BoundingRectangle.Y
    $lx = $label.Current.BoundingRectangle.X
    foreach ($e in (Get-AllElements $Window)) {
        $r = $e.Current.BoundingRectangle
        if ([Math]::Abs($r.Y - $ly) -le 8 -and $r.X -gt $lx -and $r.Width -gt 150) { return $e }
    }
    return $null
}

function Bring-ToFront {
    param([IntPtr]$Handle)
    $HWND_TOPMOST = [IntPtr](-1)
    $SWP = 0x0001 -bor 0x0002
    [SetUI.U]::SetWindowPos($Handle, $HWND_TOPMOST, 0, 0, 0, 0, $SWP) | Out-Null

    for ($try = 0; $try -lt 5; $try++) {
        [SetUI.U]::keybd_event(0x12, 0, 0, [IntPtr]::Zero)
        [SetUI.U]::keybd_event(0x12, 0, 2, [IntPtr]::Zero)
        $target = [SetUI.U]::GetWindowThreadProcessId($Handle, [IntPtr]::Zero)
        $mine = [SetUI.U]::GetCurrentThreadId()
        [SetUI.U]::AttachThreadInput($mine, $target, $true) | Out-Null
        [SetUI.U]::SetForegroundWindow($Handle) | Out-Null
        [SetUI.U]::AttachThreadInput($mine, $target, $false) | Out-Null
        Start-Sleep -Milliseconds 250
        if ([SetUI.U]::GetForegroundWindow() -eq $Handle) { return $true }
    }
    return $false
}

function Click-Element {
    param($Element, [int]$OffsetX = 0, [int]$OffsetY = 0)
    $r = $Element.Current.BoundingRectangle
    if ($OffsetX -eq 0 -and $OffsetY -eq 0) {
        $x = [int]($r.X + $r.Width / 2)
        $y = [int]($r.Y + $r.Height / 2)
    } else {
        $x = [int]($r.X + $OffsetX)
        $y = [int]($r.Y + $OffsetY)
    }
    [SetUI.U]::SetCursorPos($x, $y) | Out-Null
    Start-Sleep -Milliseconds 120
    [SetUI.U]::mouse_event(0x0002, 0, 0, 0, [IntPtr]::Zero)
    [SetUI.U]::mouse_event(0x0004, 0, 0, 0, [IntPtr]::Zero)
    Start-Sleep -Milliseconds 350
}

function Set-RangeValue {
    param($Element, [double]$Value)

    if ($null -eq $Element) { return $false }

    $pattern = $null
    if ($Element.TryGetCurrentPattern(
        [System.Windows.Automation.RangeValuePattern]::Pattern,
        [ref]$pattern)) {
        ([System.Windows.Automation.RangeValuePattern]$pattern).SetValue($Value)
        Start-Sleep -Milliseconds 150
        return $true
    }
    return $false
}

function Send-Key {
    param([byte]$Key, [switch]$Ctrl, [switch]$Alt, [switch]$Shift)
    if ($Ctrl)  { [SetUI.U]::keybd_event(0x11, 0, 0, [IntPtr]::Zero) }
    if ($Alt)   { [SetUI.U]::keybd_event(0x12, 0, 0, [IntPtr]::Zero) }
    if ($Shift) { [SetUI.U]::keybd_event(0x10, 0, 0, [IntPtr]::Zero) }
    [SetUI.U]::keybd_event($Key, 0, 0, [IntPtr]::Zero)
    Start-Sleep -Milliseconds 50
    [SetUI.U]::keybd_event($Key, 0, 2, [IntPtr]::Zero)
    if ($Shift) { [SetUI.U]::keybd_event(0x10, 0, 2, [IntPtr]::Zero) }
    if ($Alt)   { [SetUI.U]::keybd_event(0x12, 0, 2, [IntPtr]::Zero) }
    if ($Ctrl)  { [SetUI.U]::keybd_event(0x11, 0, 2, [IntPtr]::Zero) }
    Start-Sleep -Milliseconds 120
}

$installedExe = "$env:LOCALAPPDATA\Programs\Tessdeck\Tessdeck.exe"
$wasResident = $null -ne (Get-Process Tessdeck -ErrorAction SilentlyContinue)

$settings = $null
try {
    Get-Process Tessdeck -ErrorAction SilentlyContinue | Stop-Process -Force
    if (Test-Path $configDir) { Remove-Item $configDir -Recurse -Force }

    $settings = Start-Process -FilePath $exe -ArgumentList '--settings' -PassThru
    Start-Sleep -Seconds 3

    $hwnd = [IntPtr]::Zero
    for ($i = 0; $i -lt 30; $i++) {
        $settings.Refresh()
        if ($settings.MainWindowHandle -ne [IntPtr]::Zero) { $hwnd = $settings.MainWindowHandle; break }
        Start-Sleep -Milliseconds 200
    }
    if ($hwnd -eq [IntPtr]::Zero) { throw "설정 창을 찾지 못했습니다." }
    if (-not (Bring-ToFront $hwnd)) { throw "설정 창을 앞으로 가져오지 못했습니다." }

    $window = $auto::FromHandle($hwnd)
    Ok ("설정 창 열림: " + $window.Current.Name)

    $list = Find-ListView $window
    if ($null -eq $list) { throw "기능 목록을 찾지 못했습니다." }
    Click-Element $list 30 45
    Send-Key 0x24
    for ($k = 0; $k -lt 10; $k++) { Send-Key 0x28 }
    Start-Sleep -Milliseconds 300

    $current = (Find-CaptureBox $window).Current.Name
    if ($current -like '*H*' -and $current -notlike '*←*') {
        Ok "목록에서 '오른쪽 1/3' 선택됨 (현재 단축키 표시: $current)"
    } else {
        Bad "목록 선택이 옮겨지지 않음 (입력 상자 표시값 '$current')"
    }

    $capture = Find-CaptureBox $window
    if ($null -eq $capture) { throw "단축키 입력 상자를 찾지 못했습니다." }
    Click-Element $capture
    Send-Key 0x42 -Ctrl -Alt -Shift
    Start-Sleep -Milliseconds 300

    $shown = (Find-CaptureBox $window).Current.Name
    if ($shown -like '*Ctrl*' -and $shown -like '*Shift*' -and $shown -like '*B*') {
        Ok "누른 조합이 입력 상자에 표시됨: $shown"
    } else {
        Bad "입력 상자에 조합이 표시되지 않음 (표시값 '$shown')"
    }

    $assign = Find-Element $window '이 단축키로 지정'
    if ($null -eq $assign) { throw "'이 단축키로 지정' 버튼을 찾지 못했습니다." }
    Click-Element $assign
    Ok "'이 단축키로 지정' 버튼 클릭"

    $layoutNav = Find-Element $window '레이아웃 설정'
    if ($null -eq $layoutNav) { throw "'레이아웃' 탐색 버튼을 찾지 못했습니다." }
    Click-Element $layoutNav

    $ratio1 = Find-Element $window '첫 번째 순환 비율'
    $ratio2 = Find-Element $window '두 번째 순환 비율'
    $ratio3 = Find-Element $window '세 번째 순환 비율'
    if ($null -eq $ratio1 -or $null -eq $ratio2 -or $null -eq $ratio3) {
        throw '순환 비율 입력 상자를 찾지 못했습니다.'
    }

    $ratioSet = (Set-RangeValue $ratio1 50) -and
        (Set-RangeValue $ratio2 40) -and
        (Set-RangeValue $ratio3 60)
    if ($ratioSet) {
        Ok "레이아웃 순환 비율 입력: 50 / 40 / 60"
    } else {
        Bad "레이아웃 순환 비율 입력 실패"
    }

    $save = Find-Element $window '저장'
    if ($null -eq $save) { throw "'저장' 버튼을 찾지 못했습니다." }
    Click-Element $save
    Start-Sleep -Seconds 1

    if (-not (Test-Path $configPath)) {
        Bad "저장했는데 설정 파일이 없음"
    } else {
        $saved = Get-Content $configPath -Raw
        if ($saved -match 'LastThird=Ctrl\+Alt\+Shift\+B') {
            Ok "설정 파일에 반영됨: LastThird=Ctrl+Alt+Shift+B"
        } else {
            $line = ($saved -split "`r?`n" | Where-Object { $_ -like 'LastThird=*' }) -join ''
            Bad "설정 파일에 반영되지 않음 (현재 '$line')"
        }

        if ($saved -match 'CycleRatio1=50' -and
            $saved -match 'CycleRatio2=40' -and
            $saved -match 'CycleRatio3=60') {
            Ok "순환 비율 저장됨: 50 / 40 / 60"
        } else {
            Bad "순환 비율이 설정 파일에 정확히 저장되지 않음"
        }
    }

    $tmp = [System.IO.Path]::GetTempFileName()
    Start-Process -FilePath $exe -ArgumentList @('--check', '--out', $tmp) -Wait -WindowStyle Hidden | Out-Null
    $check = [System.IO.File]::ReadAllText($tmp, [System.Text.Encoding]::UTF8)
    Remove-Item $tmp -Force -ErrorAction SilentlyContinue
    if ($check -match 'failed=0' -and $check -match 'assigned=21') {
        Ok "바뀐 설정으로 단축키 21개 모두 정상 등록"
    } else {
        $conf = ($check -split "`r?`n" | Where-Object { $_ -like 'conflict=*' }) -join ', '
        Bad "등록 실패가 있음: $conf"
    }
}
finally {
    if ($null -ne $settings) { $settings | Stop-Process -Force -ErrorAction SilentlyContinue }
    Get-Process Tessdeck -ErrorAction SilentlyContinue | Stop-Process -Force

    if (Test-Path $configDir) {
        Remove-Item $configDir -Recurse -Force -ErrorAction SilentlyContinue
    }
    if ($hadConfigDir) {
        New-Item -ItemType Directory -Path $configDir -Force | Out-Null
        Get-ChildItem -LiteralPath $backupDir -Force -ErrorAction SilentlyContinue | ForEach-Object {
            Copy-Item -LiteralPath $_.FullName -Destination $configDir -Recurse -Force
        }
    }
    if (Test-Path $backupDir) {
        Remove-Item $backupDir -Recurse -Force -ErrorAction SilentlyContinue
    }
    [SetUI.U]::SetCursorPos($cursorX, $cursorY) | Out-Null

    if ($wasResident -and (Test-Path $installedExe) -and -not (Get-Process Tessdeck -ErrorAction SilentlyContinue)) {
        Start-Process -FilePath $installedExe | Out-Null
        Start-Sleep -Seconds 2
        Write-Host "상주 중이던 Tessdeck 을 다시 띄웠습니다."
    }

    Write-Host ""
    Write-Host "테스트용 설정을 제거하고 원래 Tessdeck 설정과 마우스 위치로 되돌렸습니다."
}

Write-Host ""
Write-Host ("결과: 통과 {0} / 실패 {1}" -f $pass, $fail)
if ($fail -gt 0) { exit 1 } else { exit 0 }
