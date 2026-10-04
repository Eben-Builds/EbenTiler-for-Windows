param(
    [string]$OutDir
)

$ErrorActionPreference = 'Stop'

$toolsDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Split-Path -Parent $toolsDir
$exe = Join-Path $root 'build\Tessdeck.exe'
$installer = Join-Path $root 'dist\Tessdeck-Setup.exe'
$iconScript = Join-Path $toolsDir 'make-appicon.ps1'
$uiScript = Join-Path $toolsDir 'capture-ui.ps1'

if ([string]::IsNullOrWhiteSpace($OutDir)) {
    $OutDir = Join-Path $root 'build\brand-check'
}
New-Item -ItemType Directory -Path $OutDir -Force | Out-Null

if (-not (Test-Path $exe)) { throw "Tessdeck.exe was not found: $exe" }
if (-not (Test-Path $installer)) { throw "Tessdeck-Setup.exe was not found: $installer" }

Add-Type -AssemblyName System.Drawing

function Save-Window {
    param([System.Diagnostics.Process]$Process, [string]$Path, [int]$TimeoutSeconds = 12)

    Add-Type -Namespace BrandCap -Name Native -MemberDefinition @'
[DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
[DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }
'@ -ErrorAction SilentlyContinue

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $hwnd = [IntPtr]::Zero
    do {
        $Process.Refresh()
        if ($Process.MainWindowHandle -ne [IntPtr]::Zero) {
            $hwnd = $Process.MainWindowHandle
            break
        }
        Start-Sleep -Milliseconds 250
    } while ((Get-Date) -lt $deadline)

    if ($hwnd -eq [IntPtr]::Zero) {
        throw "Visible window was not found for process $($Process.ProcessName)."
    }

    $r = New-Object 'BrandCap.Native+RECT'
    [BrandCap.Native]::GetWindowRect($hwnd, [ref]$r) | Out-Null
    $w = $r.Right - $r.Left
    $h = $r.Bottom - $r.Top
    if ($w -le 0 -or $h -le 0) {
        throw "Invalid window size: $($w)x$($h)"
    }

    $bmp = New-Object System.Drawing.Bitmap $w, $h
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $hdc = $g.GetHdc()
    try {
        if (-not [BrandCap.Native]::PrintWindow($hwnd, $hdc, 2)) {
            throw "PrintWindow failed for $($Process.ProcessName)."
        }
    }
    finally {
        $g.ReleaseHdc($hdc)
        $g.Dispose()
    }

    $bmp.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Host "Captured: $Path"
}

$report = New-Object System.Collections.Generic.List[string]
$report.Add("Tessdeck visual brand check")
$report.Add("Generated: $(Get-Date -Format s)")
$report.Add("")

& powershell -NoProfile -ExecutionPolicy Bypass -File $iconScript
if ($LASTEXITCODE -ne 0) { throw "App icon generation failed." }

$preview = Join-Path $root 'build\icons\appicon-preview.png'
if (-not (Test-Path $preview)) { throw "App icon preview was not generated." }
Copy-Item $preview (Join-Path $OutDir 'appicon-256.png') -Force

$icoPath = Join-Path $root 'assets\app.ico'
$bytes = [IO.File]::ReadAllBytes($icoPath)
$count = [BitConverter]::ToUInt16($bytes, 4)
$sizes = @()
for ($i = 0; $i -lt $count; $i++) {
    $offset = 6 + ($i * 16)
    $w = [int]$bytes[$offset]
    $h = [int]$bytes[$offset + 1]
    if ($w -eq 0) { $w = 256 }
    if ($h -eq 0) { $h = 256 }
    $sizes += ("{0}x{1}" -f $w, $h)
}
$report.Add("ICO entries: " + ($sizes -join ', '))

$required = @('16x16','20x20','24x24','32x32','40x40','48x48','64x64','96x96','128x128','256x256')
foreach ($size in $required) {
    if ($sizes -notcontains $size) { throw "Missing ICO size: $size" }
}

$associated = [System.Drawing.Icon]::ExtractAssociatedIcon($exe)
if ($null -eq $associated) { throw "Windows could not extract the Tessdeck executable icon." }

$associatedBitmap = $associated.ToBitmap()
$associatedPath = Join-Path $OutDir 'exe-associated-icon.png'
$associatedBitmap.Save($associatedPath, [System.Drawing.Imaging.ImageFormat]::Png)
$associatedBitmap.Dispose()
$associated.Dispose()
$report.Add("EXE associated icon: OK")

$uiOut = Join-Path $OutDir 'windows-ui'
& powershell -NoProfile -ExecutionPolicy Bypass -File $uiScript $uiOut
if ($LASTEXITCODE -ne 0) { throw "Windows UI capture failed." }

foreach ($requiredShot in @('settings.png','tray.png')) {
    if (-not (Test-Path (Join-Path $uiOut $requiredShot))) {
        throw "Missing Windows UI capture: $requiredShot"
    }
}

$report.Add("Settings window capture: OK")
$report.Add("Tray region capture: OK")

$setup = Start-Process -FilePath $installer -ArgumentList '/SP-' -PassThru
try {
    Save-Window -Process $setup -Path (Join-Path $OutDir 'installer-wizard.png')
    $report.Add("Installer wizard capture: OK")
}
finally {
    if (-not $setup.HasExited) {
        $setup | Stop-Process -Force -ErrorAction SilentlyContinue
    }
}

$report.Add("")
$report.Add("Review PNG files at 100% zoom before publishing v1.1.1.")
Set-Content -LiteralPath (Join-Path $OutDir 'visual-report.txt') -Value $report -Encoding utf8
$report | ForEach-Object { Write-Host $_ }
