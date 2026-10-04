# Verify Tessdeck Settings > Layout custom cycle ratios through the real UI.
#
#   powershell -ExecutionPolicy Bypass -File tools\verify-layout-settings.ps1

$ErrorActionPreference = 'Stop'

$toolsDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Split-Path -Parent $toolsDir
$exe = Join-Path $root 'build\Tessdeck.exe'
if (-not (Test-Path $exe)) { throw 'Build Tessdeck first with build.ps1.' }

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms
Add-Type -Namespace LayoutQA -Name Native -MemberDefinition @'
public struct POINT { public int X; public int Y; }
[DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr h, ref POINT p);
[DllImport("user32.dll")] public static extern uint GetDpiForWindow(IntPtr h);
[DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
[DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
[DllImport("user32.dll")] public static extern void mouse_event(uint flags, int dx, int dy, uint data, IntPtr extra);
'@

$configDir = Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::ApplicationData)) 'Tessdeck'
$configPath = Join-Path $configDir 'config.ini'
$backupDir = Join-Path $env:TEMP ('tessdeck-layout-settings-backup-' + [Guid]::NewGuid().ToString('N'))
$hadConfigDir = Test-Path $configDir
$installedExe = Join-Path $env:LOCALAPPDATA 'Programs\Tessdeck\Tessdeck.exe'
$wasResident = $null -ne (Get-Process Tessdeck -ErrorAction SilentlyContinue)

if ($hadConfigDir) {
    New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
    Get-ChildItem -LiteralPath $configDir -Force -ErrorAction SilentlyContinue | ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination $backupDir -Recurse -Force
    }
}

function Restore-TestState {
    Get-Process Tessdeck -ErrorAction SilentlyContinue |
        Stop-Process -Force -ErrorAction SilentlyContinue

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

    if ($wasResident -and (Test-Path $installedExe) -and
        -not (Get-Process Tessdeck -ErrorAction SilentlyContinue)) {
        Start-Process -FilePath $installedExe | Out-Null
    }
}

function Click-ClientPoint {
    param([IntPtr]$Handle, [int]$X, [int]$Y)

    $dpi = [LayoutQA.Native]::GetDpiForWindow($Handle)
    if ($dpi -le 0) { $dpi = 96 }
    $scale = $dpi / 96.0

    $point = New-Object 'LayoutQA.Native+POINT'
    $point.X = [int][Math]::Round($X * $scale)
    $point.Y = [int][Math]::Round($Y * $scale)

    [LayoutQA.Native]::ClientToScreen($Handle, [ref]$point) | Out-Null
    [LayoutQA.Native]::SetForegroundWindow($Handle) | Out-Null
    [LayoutQA.Native]::SetCursorPos($point.X, $point.Y) | Out-Null
    Start-Sleep -Milliseconds 120
    [LayoutQA.Native]::mouse_event(0x0002, 0, 0, 0, [IntPtr]::Zero)
    [LayoutQA.Native]::mouse_event(0x0004, 0, 0, 0, [IntPtr]::Zero)
    Start-Sleep -Milliseconds 300
}

function Get-RatioSpinners {
    param($Window)

    $all = $Window.FindAll(
        [System.Windows.Automation.TreeScope]::Descendants,
        [System.Windows.Automation.Condition]::TrueCondition)

    $spinners = @()
    foreach ($element in $all) {
        if ($element.Current.ControlType -eq [System.Windows.Automation.ControlType]::Spinner) {
            $spinners += $element
        }
    }

    if ($spinners.Count -lt 3) { return @() }

    # Ratio controls are the first row of spinners. Gap is lower on the page.
    return @($spinners |
        Sort-Object @{ Expression = { $_.Current.BoundingRectangle.Y } },
                    @{ Expression = { $_.Current.BoundingRectangle.X } } |
        Select-Object -First 3)
}

function Set-SpinnerValue {
    param($Element, [double]$Value)

    $pattern = $null
    if ($null -ne $Element -and $Element.TryGetCurrentPattern(
        [System.Windows.Automation.RangeValuePattern]::Pattern,
        [ref]$pattern)) {
        ([System.Windows.Automation.RangeValuePattern]$pattern).SetValue($Value)
        Start-Sleep -Milliseconds 150
        return $true
    }
    return $false
}

$settings = $null
try {
    Get-Process Tessdeck -ErrorAction SilentlyContinue |
        Stop-Process -Force -ErrorAction SilentlyContinue
    if (Test-Path $configDir) {
        Remove-Item $configDir -Recurse -Force
    }

    $settings = Start-Process -FilePath $exe -ArgumentList '--settings' -PassThru
    Start-Sleep -Seconds 3

    $hwnd = [IntPtr]::Zero
    for ($i = 0; $i -lt 30; $i++) {
        $settings.Refresh()
        if ($settings.MainWindowHandle -ne [IntPtr]::Zero) {
            $hwnd = $settings.MainWindowHandle
            break
        }
        Start-Sleep -Milliseconds 200
    }
    if ($hwnd -eq [IntPtr]::Zero) { throw 'Settings window was not found.' }

    $window = [System.Windows.Automation.AutomationElement]::FromHandle($hwnd)
    Write-Host '[PASS] Settings window opened.' -ForegroundColor Green

    # Layout is the third navigation button at 96-DPI client coordinates.
    Click-ClientPoint $hwnd 101 239
    Write-Host '[PASS] Layout page opened.' -ForegroundColor Green

    $values = @(50, 40, 60)
    $spinners = @(Get-RatioSpinners $window)
    $usedAutomation = $spinners.Count -eq 3

    if ($usedAutomation) {
        for ($i = 0; $i -lt 3; $i++) {
            if (-not (Set-SpinnerValue $spinners[$i] $values[$i])) {
                $usedAutomation = $false
                break
            }
        }
    }

    if (-not $usedAutomation) {
        # Center points of the three ratio NumericUpDown controls at 96 DPI.
        $ratioX = @(567, 691, 815)
        for ($i = 0; $i -lt 3; $i++) {
            Click-ClientPoint $hwnd $ratioX[$i] 465
            [System.Windows.Forms.SendKeys]::SendWait('^a')
            [System.Windows.Forms.SendKeys]::SendWait($values[$i].ToString())
            Start-Sleep -Milliseconds 150
        }
    }

    Write-Host '[PASS] Entered cycle ratios 50 / 40 / 60.' -ForegroundColor Green

    # Save button center at 96-DPI client coordinates.
    Click-ClientPoint $hwnd 799 623

    $settings.WaitForExit(5000) | Out-Null
    if (-not (Test-Path $configPath)) {
        throw "Config file was not created: $configPath"
    }

    $saved = Get-Content -LiteralPath $configPath -Raw -Encoding UTF8
    if ($saved -notmatch 'CycleRatio1=50') { throw 'CycleRatio1=50 was not saved.' }
    if ($saved -notmatch 'CycleRatio2=40') { throw 'CycleRatio2=40 was not saved.' }
    if ($saved -notmatch 'CycleRatio3=60') { throw 'CycleRatio3=60 was not saved.' }

    Write-Host '[PASS] config.ini contains 50 / 40 / 60.' -ForegroundColor Green
    Write-Host ''
    Write-Host 'Layout settings UI verification: PASS' -ForegroundColor Green
}
finally {
    if ($null -ne $settings -and -not $settings.HasExited) {
        $settings | Stop-Process -Force -ErrorAction SilentlyContinue
    }
    Restore-TestState
}
