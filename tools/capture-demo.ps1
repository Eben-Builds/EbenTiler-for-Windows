# 창 두 개를 좌/우 절반에 붙인 뒤 화면 전체를 캡처해서 눈으로 확인한다.
#
#   powershell -ExecutionPolicy Bypass -File tools\capture-demo.ps1

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$exe  = Join-Path $root 'build\RumiFlow.exe'
if (-not (Test-Path $exe)) { throw "먼저 build.ps1 로 빌드하세요." }

$outDir = Join-Path $root 'build\screenshots'
if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir | Out-Null }

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
Add-Type -Namespace Demo -Name U -MemberDefinition @'
[DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr value);
'@
try { [Demo.U]::SetProcessDpiAwarenessContext([IntPtr](-4)) | Out-Null } catch { }

$hostScript = Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) '_testwindow.ps1'
$procs = @()
$handles = @()

foreach ($i in 1..2) {
    $file = Join-Path $env:TEMP ("rumiflow-demo-$i-" + [Guid]::NewGuid().ToString('N') + ".txt")
    $p = Start-Process powershell -PassThru -ArgumentList @('-ExecutionPolicy','Bypass','-NoProfile','-File',$hostScript,$file)
    $procs += $p

    $h = [IntPtr]::Zero
    for ($k = 0; $k -lt 100; $k++) {
        Start-Sleep -Milliseconds 100
        if (Test-Path $file) {
            $raw = (Get-Content $file -Raw).Trim()
            if ($raw.Length -gt 0) { $h = [IntPtr][long]$raw; break }
        }
    }
    Remove-Item $file -Force -ErrorAction SilentlyContinue
    if ($h -eq [IntPtr]::Zero) { throw "검증용 창 $i 핸들을 얻지 못했습니다." }
    $handles += $h
}

$actions = @('LeftHalf', 'RightHalf')
for ($i = 0; $i -lt 2; $i++) {
    $arg = '0x' + $handles[$i].ToInt64().ToString('X')
    Start-Process -FilePath $exe -ArgumentList @('--apply', $actions[$i], '--hwnd', $arg) -Wait -WindowStyle Hidden | Out-Null
}
Start-Sleep -Milliseconds 800

$screen = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
$bmp = New-Object System.Drawing.Bitmap $screen.Width, $screen.Height
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.CopyFromScreen(0, 0, 0, 0, (New-Object System.Drawing.Size $screen.Width, $screen.Height))
$g.Dispose()
$path = Join-Path $outDir 'demo-halves.png'
$bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
Write-Host "저장: $path"

foreach ($p in $procs) { $p | Stop-Process -Force -ErrorAction SilentlyContinue }
Write-Host "완료"
