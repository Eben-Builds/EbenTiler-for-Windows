# 알림 영역 아이콘을 실제로 눌러 메뉴를 띄우고,
# 체크 표시가 나오는지 / 눌렀을 때 실제 설정이 바뀌는지 확인한다.
#
#   powershell -ExecutionPolicy Bypass -File tools\verify-tray-menu.ps1

$ErrorActionPreference = 'Stop'

$toolsDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Split-Path -Parent $toolsDir
$outDir = Join-Path $root 'build\screenshots'
if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir | Out-Null }

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Drawing
Add-Type -Namespace Tray -Name U -MemberDefinition @'
[DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr v);
[DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
[DllImport("user32.dll")] public static extern bool GetCursorPos(out int x, out int y);
[DllImport("user32.dll")] public static extern void mouse_event(uint f, int dx, int dy, uint d, IntPtr e);
[DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte s, uint f, IntPtr e);
public struct R { public int L; public int T; public int Rt; public int B; }
public delegate bool EnumProc(IntPtr h, IntPtr p);
[DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc cb, IntPtr p);
[DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
[DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out R r);
[DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetClassName(IntPtr h, System.Text.StringBuilder s, int n);
[DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
'@
[Tray.U]::SetProcessDpiAwarenessContext([IntPtr](-4)) | Out-Null

$auto    = [System.Windows.Automation.AutomationElement]
$scope   = [System.Windows.Automation.TreeScope]::Descendants
$desktop = $auto::RootElement
$runKey  = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'

$pass = 0
$fail = 0
function Ok  { param([string]$m) Write-Host "[통과] $m" -ForegroundColor Green; $script:pass++ }
function Bad { param([string]$m) Write-Host "[실패] $m" -ForegroundColor Red;   $script:fail++ }

$cx = 0; $cy = 0
[Tray.U]::GetCursorPos([ref]$cx, [ref]$cy) | Out-Null

function Find-Button {
    param([string]$Name)
    $c1 = New-Object System.Windows.Automation.PropertyCondition ($auto::NameProperty, $Name)
    $c2 = New-Object System.Windows.Automation.PropertyCondition ($auto::ControlTypeProperty, [System.Windows.Automation.ControlType]::Button)
    return $desktop.FindFirst($scope, (New-Object System.Windows.Automation.AndCondition @($c1, $c2)))
}

function Click-Point {
    param([int]$X, [int]$Y)
    [Tray.U]::SetCursorPos($X, $Y) | Out-Null
    Start-Sleep -Milliseconds 250
    [Tray.U]::mouse_event(0x0002, 0, 0, 0, [IntPtr]::Zero)
    [Tray.U]::mouse_event(0x0004, 0, 0, 0, [IntPtr]::Zero)
    Start-Sleep -Milliseconds 800
}

# Rectangle 프로세스가 띄운 보이는 팝업 창(= 트레이 메뉴)을 Win32 로 찾는다.
# WinForms 팝업 메뉴는 UI Automation 트리에 잡히지 않는다.
function Find-MenuWindow {
    $target = (Get-Process Rectangle -ErrorAction SilentlyContinue).Id
    if (-not $target) { return $null }
    $script:found = $null
    $cb = [Tray.U+EnumProc]{
        param([IntPtr]$h, [IntPtr]$p)
        if (-not [Tray.U]::IsWindowVisible($h)) { return $true }
        $ownerPid = 0
        [Tray.U]::GetWindowThreadProcessId($h, [ref]$ownerPid) | Out-Null
        if ($ownerPid -ne $target) { return $true }
        $sb = New-Object System.Text.StringBuilder 256
        [Tray.U]::GetClassName($h, $sb, 256) | Out-Null
        if ($sb.ToString() -like 'WindowsForms10.Window*') {
            $r = New-Object 'Tray.U+R'
            [Tray.U]::GetWindowRect($h, [ref]$r) | Out-Null
            if (($r.Rt - $r.L) -gt 60 -and ($r.B - $r.T) -gt 30) {
                $script:found = [PSCustomObject]@{ X=$r.L; Y=$r.T; W=($r.Rt-$r.L); H=($r.B-$r.T) }
                return $false
            }
        }
        return $true
    }
    [Tray.U]::EnumWindows($cb, [IntPtr]::Zero) | Out-Null
    return $script:found
}

# 아이콘을 찾아 누르고, 열린 메뉴 창을 돌려준다.
# 아이콘이 숨김 목록에 들어가 있으면 먼저 펼친다.
function Open-TrayMenu {
    $icon = Find-Button 'Rectangle for Windows'
    if ($null -eq $icon) {
        # 펼치기 단추는 상태에 따라 이름이 바뀐다.
        $chevron = Find-Button '숨겨진 아이콘 표시'
        if ($null -eq $chevron) { $chevron = Find-Button '숨겨진 아이콘 표시 숨기기' }
        if ($null -eq $chevron) {
            Write-Host "  (진단) 펼치기 단추를 찾지 못했습니다." -ForegroundColor DarkGray
            return $null
        }
        $cr = $chevron.Current.BoundingRectangle
        Click-Point ([int]($cr.X + $cr.Width / 2)) ([int]($cr.Y + $cr.Height / 2))
        for ($i = 0; $i -lt 10; $i++) {
            Start-Sleep -Milliseconds 500
            $icon = Find-Button 'Rectangle for Windows'
            if ($null -ne $icon) { break }
        }
        # 이미 열려 있어서 방금 클릭으로 닫혔을 수 있다. 한 번 더 눌러 본다.
        if ($null -eq $icon) {
            Click-Point ([int]($cr.X + $cr.Width / 2)) ([int]($cr.Y + $cr.Height / 2))
            for ($i = 0; $i -lt 10; $i++) {
                Start-Sleep -Milliseconds 500
                $icon = Find-Button 'Rectangle for Windows'
                if ($null -ne $icon) { break }
            }
        }
    }
    if ($null -eq $icon) {
        Write-Host "  (진단) 알림 영역에서 아이콘을 찾지 못했습니다." -ForegroundColor DarkGray
        return $null
    }

    $ir = $icon.Current.BoundingRectangle
    Click-Point ([int]($ir.X + $ir.Width / 2)) ([int]($ir.Y + $ir.Height / 2))
    Start-Sleep -Milliseconds 600
    return Find-MenuWindow
}

function Save-Menu {
    param($Menu, [string]$Path)
    # 마우스가 항목 위에 있으면 툴팁이 메뉴를 가리므로 옆으로 치운다.
    [Tray.U]::SetCursorPos($Menu.X - 60, $Menu.Y + $Menu.H + 40) | Out-Null
    Start-Sleep -Milliseconds 700
    $w = $Menu.W + 8
    $h = $Menu.H + 8
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen(($Menu.X - 4), ($Menu.Y - 4), 0, 0, (New-Object System.Drawing.Size $w, $h))
    $g.Dispose()
    $bmp.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}

function Close-Menu {
    [Tray.U]::keybd_event(0x1B, 0, 0, [IntPtr]::Zero)
    [Tray.U]::keybd_event(0x1B, 0, 2, [IntPtr]::Zero)
    Start-Sleep -Milliseconds 500
}

function Get-Startup {
    return ($null -ne (Get-ItemProperty $runKey -ErrorAction SilentlyContinue).RectangleWindows)
}

$originalStartup = Get-Startup

try {
    if (-not (Get-Process Rectangle -ErrorAction SilentlyContinue)) {
        $installedExe = "$env:LOCALAPPDATA\Programs\Rectangle\Rectangle.exe"
        if (-not (Test-Path $installedExe)) {
            throw "Rectangle 이 실행 중이 아니고 설치본도 없습니다. install.ps1 로 설치하세요."
        }
        Start-Process -FilePath $installedExe | Out-Null
        Start-Sleep -Seconds 3
        if (-not (Get-Process Rectangle -ErrorAction SilentlyContinue)) {
            throw "Rectangle 을 띄우지 못했습니다."
        }
        Write-Host "Rectangle 이 꺼져 있어 설치본을 실행했습니다."
    }

    # 1) 아이콘 클릭 -> 메뉴 열림
    $menu = Open-TrayMenu
    if ($null -eq $menu) {
        Bad "왼쪽 클릭으로 메뉴가 뜨지 않음"
    } else {
        Ok ("왼쪽 클릭으로 메뉴 열림: {0},{1} {2}x{3}" -f $menu.X, $menu.Y, $menu.W, $menu.H)

        # 2) 체크 표시가 보이는지 캡처로 남긴다
        $checkedPng = Join-Path $outDir 'tray-menu.png'
        Save-Menu $menu $checkedPng
        Ok "체크된 상태 캡처: $checkedPng"

        # 3) 체크 항목을 눌러 실제 설정이 바뀌는지
        $before = Get-Startup
        Click-Point ([int]($menu.X + $menu.W / 2)) ([int]($menu.Y + 37))
        $after = Get-Startup
        if ($after -ne $before) {
            Ok ("체크 항목을 누르니 실제 설정이 바뀜: 자동 실행 {0} -> {1}" -f $before, $after)
        } else {
            Bad "체크 항목을 눌러도 자동 실행 설정이 그대로임"
        }

        # 4) 다시 열었을 때 체크가 바뀐 상태를 따라오는지
        $menu2 = Open-TrayMenu
        if ($null -eq $menu2) {
            Bad "메뉴를 다시 열지 못함"
        } else {
            $uncheckedPng = Join-Path $outDir 'tray-menu-unchecked.png'
            Save-Menu $menu2 $uncheckedPng
            Ok "체크 해제 상태 캡처: $uncheckedPng"

            # 원래대로 되돌린다
            Click-Point ([int]($menu2.X + $menu2.W / 2)) ([int]($menu2.Y + 37))
            if ((Get-Startup) -eq $before) {
                Ok ("다시 눌러 원래 상태로 복구됨 (자동 실행 {0})" -f $before)
            } else {
                Bad "원래 상태로 되돌리지 못함"
            }
        }
        Close-Menu
    }
}
finally {
    Close-Menu
    # 자동 실행 상태를 검사 전으로 확실히 맞춰 둔다.
    $exe = "$env:LOCALAPPDATA\Programs\Rectangle\Rectangle.exe"
    if ((Test-Path $exe) -and ((Get-Startup) -ne $originalStartup)) {
        $want = 'off'
        if ($originalStartup) { $want = 'on' }
        Start-Process -FilePath $exe -ArgumentList @('--startup', $want) -Wait -WindowStyle Hidden | Out-Null
    }
    [Tray.U]::SetCursorPos($cx, $cy) | Out-Null
    Write-Host ""
    Write-Host ("자동 실행 상태: {0} / 검사 전과 같음: {1}" -f (Get-Startup), ((Get-Startup) -eq $originalStartup))
}

Write-Host ""
Write-Host ("결과: 통과 {0} / 실패 {1}" -f $pass, $fail)
if ($fail -gt 0) { exit 1 } else { exit 0 }
