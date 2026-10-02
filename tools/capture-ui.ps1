# 설정 창과 알림 영역 아이콘을 화면 캡처해서 눈으로 확인할 수 있게 저장한다.
#
#   powershell -ExecutionPolicy Bypass -File tools\capture-ui.ps1 [저장폴더]

param([string]$OutDir)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$exe  = Join-Path $root 'build\RumiFlow.exe'
if (-not (Test-Path $exe)) { throw "먼저 build.ps1 로 빌드하세요." }

if ([string]::IsNullOrEmpty($OutDir)) { $OutDir = Join-Path $root 'build\screenshots' }
if (-not (Test-Path $OutDir)) { New-Item -ItemType Directory -Path $OutDir | Out-Null }

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms

Add-Type -Namespace Cap -Name U -MemberDefinition @'
[DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr value);
[DllImport("user32.dll")] public static extern IntPtr FindWindow(string cls, string title);
public struct R { public int Left; public int Top; public int Right; public int Bottom; }
[DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out R r);
[DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
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
