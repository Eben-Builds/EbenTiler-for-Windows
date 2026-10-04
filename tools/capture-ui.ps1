# 설정 창과 알림 영역 아이콘을 화면 캡처해서 눈으로 확인할 수 있게 저장한다.
#
#   powershell -ExecutionPolicy Bypass -File tools\capture-ui.ps1 [저장폴더]

param([string]$OutDir)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$exe  = Join-Path $root 'build\Tessdeck.exe'
if (-not (Test-Path $exe)) { throw "먼저 build.ps1 로 빌드하세요." }

if ([string]::IsNullOrEmpty($OutDir)) { $OutDir = Join-Path $root 'build\screenshots' }
if (-not (Test-Path $OutDir)) { New-Item -ItemType Directory -Path $OutDir | Out-Null }

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

Add-Type -Namespace Cap -Name U -MemberDefinition @'
[DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr value);
[DllImport("user32.dll")] public static extern IntPtr FindWindow(string cls, string title);
public struct R { public int Left; public int Top; public int Right; public int Bottom; }
[DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out R r);
[DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
public struct P { public int X; public int Y; }
[DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr h, ref P p);
[DllImport("user32.dll")] public static extern uint GetDpiForWindow(IntPtr h);
[DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
[DllImport("user32.dll")] public static extern void mouse_event(uint flags, int dx, int dy, uint data, IntPtr extra);
'@
try { [Cap.U]::SetProcessDpiAwarenessContext([IntPtr](-4)) | Out-Null } catch { }

function Save-Region {
    param([int]$X, [int]$Y, [int]$W, [int]$H, [string]$Path)
    $bmp = New-Object System.Drawing.Bitmap $W, $H
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($X, $Y, 0, 0, (New-Object System.Drawing.Size $W, $H))
    $g.Dispose()
    $bmp.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Host "저장: $Path"
}

function Invoke-SettingsNavigation {
    param([IntPtr]$Handle, [string[]]$Names)

    $window = [System.Windows.Automation.AutomationElement]::FromHandle($Handle)
    $scope = [System.Windows.Automation.TreeScope]::Descendants

    foreach ($name in $Names) {
        $condition = New-Object System.Windows.Automation.PropertyCondition (
            [System.Windows.Automation.AutomationElement]::NameProperty, $name)
        $element = $window.FindFirst($scope, $condition)
        if ($null -eq $element) { continue }

        $pattern = $null
        if ($element.TryGetCurrentPattern(
            [System.Windows.Automation.InvokePattern]::Pattern,
            [ref]$pattern)) {
            ([System.Windows.Automation.InvokePattern]$pattern).Invoke()
            Start-Sleep -Milliseconds 500
            return $true
        }
    }

    $dpi = [Cap.U]::GetDpiForWindow($Handle)
    if ($dpi -le 0) { $dpi = 96 }
    $scale = $dpi / 96.0

    # 설정 창의 왼쪽 탐색에서 세 번째 항목(레이아웃) 중앙.
    $point = New-Object 'Cap.U+P'
    $point.X = [int][Math]::Round(101 * $scale)
    $point.Y = [int][Math]::Round(239 * $scale)
    [Cap.U]::ClientToScreen($Handle, [ref]$point) | Out-Null
    [Cap.U]::SetCursorPos($point.X, $point.Y) | Out-Null
    Start-Sleep -Milliseconds 100
    [Cap.U]::mouse_event(0x0002, 0, 0, 0, [IntPtr]::Zero)
    [Cap.U]::mouse_event(0x0004, 0, 0, 0, [IntPtr]::Zero)
    Start-Sleep -Milliseconds 500
    return $true
}

$settings = Start-Process -FilePath $exe -ArgumentList '--settings' -PassThru
Start-Sleep -Seconds 3

$hwnd = [IntPtr]::Zero
for ($i = 0; $i -lt 30; $i++) {
    $settings.Refresh()
    if ($settings.MainWindowHandle -ne [IntPtr]::Zero) { $hwnd = $settings.MainWindowHandle; break }
    Start-Sleep -Milliseconds 200
}
if ($hwnd -eq [IntPtr]::Zero) {
    $settings | Stop-Process -Force -ErrorAction SilentlyContinue
    throw "설정 창을 찾지 못했습니다."
}

$r = New-Object 'Cap.U+R'
[Cap.U]::GetWindowRect($hwnd, [ref]$r) | Out-Null
$w = $r.Right - $r.Left
$h = $r.Bottom - $r.Top
$bmp = New-Object System.Drawing.Bitmap $w, $h
$g = [System.Drawing.Graphics]::FromImage($bmp)
$hdc = $g.GetHdc()
[Cap.U]::PrintWindow($hwnd, $hdc, 2) | Out-Null
$g.ReleaseHdc($hdc)
$g.Dispose()
$settingsPng = Join-Path $OutDir 'settings.png'
$bmp.Save($settingsPng, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
Write-Host "저장: $settingsPng"

if (-not (Invoke-SettingsNavigation $hwnd @('레이아웃 설정', '레이아웃'))) {
    $settings | Stop-Process -Force -ErrorAction SilentlyContinue
    throw "레이아웃 설정 페이지로 이동하지 못했습니다."
}

[Cap.U]::GetWindowRect($hwnd, [ref]$r) | Out-Null
$w = $r.Right - $r.Left
$h = $r.Bottom - $r.Top
$layoutBmp = New-Object System.Drawing.Bitmap $w, $h
$layoutGraphics = [System.Drawing.Graphics]::FromImage($layoutBmp)
$layoutHdc = $layoutGraphics.GetHdc()
[Cap.U]::PrintWindow($hwnd, $layoutHdc, 2) | Out-Null
$layoutGraphics.ReleaseHdc($layoutHdc)
$layoutGraphics.Dispose()
$layoutPng = Join-Path $OutDir 'layout.png'
$layoutBmp.Save($layoutPng, [System.Drawing.Imaging.ImageFormat]::Png)
$layoutBmp.Dispose()
Write-Host "저장: $layoutPng"

$settings | Stop-Process -Force -ErrorAction SilentlyContinue

$app = Start-Process -FilePath $exe -PassThru
Start-Sleep -Seconds 3

$screen = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
$trayW = 420
$trayH = 60
$trayX = $screen.Width - $trayW
$trayY = $screen.Height - $trayH
Save-Region $trayX $trayY $trayW $trayH (Join-Path $OutDir 'tray.png')

$app | Stop-Process -Force -ErrorAction SilentlyContinue
Write-Host "완료"
