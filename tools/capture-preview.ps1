# 설정 창의 미리보기가 기능마다 제대로 그려지는지 캡처해서 확인한다.
#
#   powershell -ExecutionPolicy Bypass -File tools\capture-preview.ps1

$ErrorActionPreference = 'Stop'

$toolsDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Split-Path -Parent $toolsDir
$exe  = Join-Path $root 'build\SnapFlow.exe'
$outDir = Join-Path $root 'build\screenshots'
if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir -Force | Out-Null }

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Drawing
Add-Type -Namespace Pv -Name U -MemberDefinition @'
[DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr v);
[DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
[DllImport("user32.dll")] public static extern void mouse_event(uint f, int dx, int dy, uint d, IntPtr e);
[DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte s, uint f, IntPtr e);
[DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
[DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
[DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr a, int x, int y, int cx, int cy, uint f);
[DllImport("user32.dll")] public static extern bool AttachThreadInput(uint a, uint b, bool c);
[DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, IntPtr p);
[DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
'@
[Pv.U]::SetProcessDpiAwarenessContext([IntPtr](-4)) | Out-Null

$auto  = [System.Windows.Automation.AutomationElement]
$scope = [System.Windows.Automation.TreeScope]::Descendants

$targets = @(
    @{ Index = 0;  Name = '왼쪽 절반' }
    @{ Index = 4;  Name = '왼쪽 위 1/4' }
    @{ Index = 12; Name = '오른쪽 2/3' }
    @{ Index = 15; Name = '가운데 정렬' }
    @{ Index = 16; Name = '크기 키우기' }
    @{ Index = 18; Name = '원래 크기 복원' }
    @{ Index = 19; Name = '다음 모니터로' }
)

function Bring-ToFront {
    param([IntPtr]$Handle)
    [Pv.U]::SetWindowPos($Handle, [IntPtr](-1), 0, 0, 0, 0, (0x0001 -bor 0x0002)) | Out-Null
    for ($i = 0; $i -lt 5; $i++) {
        [Pv.U]::keybd_event(0x12, 0, 0, [IntPtr]::Zero)
        [Pv.U]::keybd_event(0x12, 0, 2, [IntPtr]::Zero)
        $t = [Pv.U]::GetWindowThreadProcessId($Handle, [IntPtr]::Zero)
        $m = [Pv.U]::GetCurrentThreadId()
        [Pv.U]::AttachThreadInput($m, $t, $true) | Out-Null
        [Pv.U]::SetForegroundWindow($Handle) | Out-Null
        [Pv.U]::AttachThreadInput($m, $t, $false) | Out-Null
        Start-Sleep -Milliseconds 250
        if ([Pv.U]::GetForegroundWindow() -eq $Handle) { return $true }
    }
    return $false
}

function Send-Key {
    param([byte]$Key)
    [Pv.U]::keybd_event($Key, 0, 0, [IntPtr]::Zero)
    Start-Sleep -Milliseconds 40
    [Pv.U]::keybd_event($Key, 0, 2, [IntPtr]::Zero)
    Start-Sleep -Milliseconds 170
}

$proc = Start-Process -FilePath $exe -ArgumentList '--settings' -PassThru
try {
    Start-Sleep -Seconds 3
    $hwnd = [IntPtr]::Zero
    for ($i = 0; $i -lt 30; $i++) {
        $proc.Refresh()
        if ($proc.MainWindowHandle -ne [IntPtr]::Zero) { $hwnd = $proc.MainWindowHandle; break }
        Start-Sleep -Milliseconds 200
    }
    if ($hwnd -eq [IntPtr]::Zero) { throw "설정 창을 찾지 못했습니다." }
    if (-not (Bring-ToFront $hwnd)) { throw "설정 창을 앞으로 가져오지 못했습니다." }

    $window = $auto::FromHandle($hwnd)
    $panes = $window.FindAll($scope, [System.Windows.Automation.Condition]::TrueCondition)

    $list = $null; $best = 0
    foreach ($e in $panes) {
        if ($e.Current.Name -ne '') { continue }
        $r = $e.Current.BoundingRectangle
        $a = $r.Width * $r.Height
        if ($a -gt $best) { $best = $a; $list = $e }
    }
    if ($null -eq $list) { throw "기능 목록을 찾지 못했습니다." }
    $lr = $list.Current.BoundingRectangle

    $preview = $null; $best = 0
    foreach ($e in $panes) {
        if ($e.Current.Name -ne '') { continue }
        $r = $e.Current.BoundingRectangle
        if ($r.X -lt ($lr.X + $lr.Width - 5)) { continue }
        $a = $r.Width * $r.Height
        if ($a -gt $best) { $best = $a; $preview = $e }
    }
    if ($null -eq $preview) { throw "미리보기 칸을 찾지 못했습니다." }
    $pr = $preview.Current.BoundingRectangle
    Write-Host ("미리보기 칸: {0},{1} {2}x{3}" -f [int]$pr.X, [int]$pr.Y, [int]$pr.Width, [int]$pr.Height)

    $capX = [int]$pr.X - 6
    $capY = [int]$pr.Y - 6
    $capW = [int]$pr.Width + 12
    $capH = [int]$pr.Height + 108

    [Pv.U]::SetCursorPos([int]($lr.X + 30), [int]($lr.Y + 45)) | Out-Null
    Start-Sleep -Milliseconds 150
    [Pv.U]::mouse_event(0x0002, 0, 0, 0, [IntPtr]::Zero)
    [Pv.U]::mouse_event(0x0004, 0, 0, 0, [IntPtr]::Zero)
    Start-Sleep -Milliseconds 400

    $shots = @()
    foreach ($t in $targets) {
        Send-Key 0x24
        for ($k = 0; $k -lt $t.Index; $k++) { Send-Key 0x28 }
        Start-Sleep -Milliseconds 350

        $bmp = New-Object System.Drawing.Bitmap $capW, $capH
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        $g.CopyFromScreen($capX, $capY, 0, 0, (New-Object System.Drawing.Size $capW, $capH))
        $g.Dispose()
        $shots += , @{ Name = $t.Name; Image = $bmp }
        Write-Host ("  캡처: {0}" -f $t.Name)
    }

    $cols = 4
    $rows = [Math]::Ceiling($shots.Count / $cols)
    $cellW = $capW + 24
    $cellH = $capH + 46
    $sheet = New-Object System.Drawing.Bitmap ($cellW * $cols + 20), ($cellH * $rows + 50)
    $sg = [System.Drawing.Graphics]::FromImage($sheet)
    $sg.Clear([System.Drawing.Color]::FromArgb(255, 250, 250, 250))
    $sg.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $fT = New-Object System.Drawing.Font 'Malgun Gothic', 12, ([System.Drawing.FontStyle]::Bold)
    $fN = New-Object System.Drawing.Font 'Malgun Gothic', 10, ([System.Drawing.FontStyle]::Bold)
    $brInk = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 24, 28, 35))
    $sg.DrawString('설정 창 미리보기 - 기능별 실제 렌더', $fT, $brInk, 16, 12)

    for ($i = 0; $i -lt $shots.Count; $i++) {
        $cx = 16 + ($i % $cols) * $cellW
        $cy = 46 + [Math]::Floor($i / $cols) * $cellH
        $sg.DrawString($shots[$i].Name, $fN, $brInk, $cx, $cy)
        $sg.DrawImage($shots[$i].Image, $cx, ($cy + 22))
        $shots[$i].Image.Dispose()
    }
    $sg.Dispose()
    $path = Join-Path $outDir 'settings-preview.png'
    $sheet.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $sheet.Dispose()
    Write-Host "저장: $path"
}
finally {
    $proc | Stop-Process -Force -ErrorAction SilentlyContinue
}
