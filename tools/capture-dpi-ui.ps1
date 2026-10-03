# 현재 Windows 배율에서 설정 창과 시작 가이드를 실제로 띄우고 캡처한다.
# 100% / 125% / 150% 각각 Windows 디스플레이 배율을 바꾼 뒤 한 번씩 실행한다.
#
#   powershell -ExecutionPolicy Bypass -File tools\capture-dpi-ui.ps1
#
# 결과:
#   build\screenshots\dpi-100\welcome-first-run.png
#   build\screenshots\dpi-100\settings-general.png
#   build\screenshots\dpi-100\welcome-from-settings.png

param([string]$OutDir)

$ErrorActionPreference = 'Stop'

$toolsDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Split-Path -Parent $toolsDir
$exe = Join-Path $root 'build\EbenTiler.exe'
if (-not (Test-Path $exe)) { throw "먼저 build.ps1 로 빌드하세요." }

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

Add-Type -Namespace DpiUi -Name Native -MemberDefinition @'
[DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr value);
[DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern IntPtr FindWindow(string cls, string title);
public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }
[DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
[DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr h, out RECT r);
[DllImport("user32.dll")] public static extern uint GetDpiForWindow(IntPtr h);
[DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
'@
try { [DpiUi.Native]::SetProcessDpiAwarenessContext([IntPtr](-4)) | Out-Null } catch { }

$auto = [System.Windows.Automation.AutomationElement]
$scope = [System.Windows.Automation.TreeScope]::Descendants
$buttonType = [System.Windows.Automation.ControlType]::Button

function Wait-Window {
    param([string]$Title, [int]$TimeoutSeconds = 10)
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        $hwnd = [DpiUi.Native]::FindWindow($null, $Title)
        if ($hwnd -ne [IntPtr]::Zero) { return $hwnd }
        Start-Sleep -Milliseconds 200
    } while ((Get-Date) -lt $deadline)
    return [IntPtr]::Zero
}

function Get-ScaleInfo {
    param([IntPtr]$Handle)
    $dpi = [int][DpiUi.Native]::GetDpiForWindow($Handle)
    if ($dpi -le 0) { $dpi = 96 }
    $percent = [int][Math]::Round(($dpi / 96.0) * 100)
    return @{ Dpi = $dpi; Percent = $percent; Scale = $dpi / 96.0 }
}

function Assert-ClientSize {
    param([IntPtr]$Handle, [int]$BaseWidth, [int]$BaseHeight, [string]$Name)
    $info = Get-ScaleInfo $Handle
    $r = New-Object 'DpiUi.Native+RECT'
    [DpiUi.Native]::GetClientRect($Handle, [ref]$r) | Out-Null
    $actualW = $r.Right - $r.Left
    $actualH = $r.Bottom - $r.Top
    $expectedW = [int][Math]::Round($BaseWidth * $info.Scale)
    $expectedH = [int][Math]::Round($BaseHeight * $info.Scale)
    $tol = 3

    if ([Math]::Abs($actualW - $expectedW) -gt $tol -or [Math]::Abs($actualH - $expectedH) -gt $tol) {
        throw "$Name 크기 검증 실패: 실제 ${actualW}x${actualH}, 기대 ${expectedW}x${expectedH}, DPI $($info.Dpi)"
    }
    Write-Host "[통과] $Name client ${actualW}x${actualH} / DPI $($info.Dpi) ($($info.Percent)%)" -ForegroundColor Green
    return $info
}

function Save-Window {
    param([IntPtr]$Handle, [string]$Path)
    $r = New-Object 'DpiUi.Native+RECT'
    [DpiUi.Native]::GetWindowRect($Handle, [ref]$r) | Out-Null
    $w = $r.Right - $r.Left
    $h = $r.Bottom - $r.Top
    if ($w -le 0 -or $h -le 0) { throw "캡처할 창 크기가 올바르지 않습니다." }

    $bmp = New-Object System.Drawing.Bitmap $w, $h
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $hdc = $g.GetHdc()
    try {
        [DpiUi.Native]::PrintWindow($Handle, $hdc, 2) | Out-Null
    }
    finally {
        $g.ReleaseHdc($hdc)
        $g.Dispose()
    }
    $bmp.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Host "저장: $Path"
}

function Find-Button {
    param([IntPtr]$WindowHandle, [string]$Name)
    $window = $auto::FromHandle($WindowHandle)
    $condition = New-Object System.Windows.Automation.PropertyCondition ($auto::NameProperty, $Name)
    $items = $window.FindAll($scope, $condition)
    for ($i = 0; $i -lt $items.Count; $i++) {
        if ($items.Item($i).Current.ControlType -eq $buttonType) { return $items.Item($i) }
    }
    return $null
}

function Find-Element {
    param([IntPtr]$WindowHandle, [string]$Name)
    $window = $auto::FromHandle($WindowHandle)
    $condition = New-Object System.Windows.Automation.PropertyCondition ($auto::NameProperty, $Name)
    return $window.FindFirst($scope, $condition)
}

function Invoke-Button {
    param($Element)
    if ($null -eq $Element) { throw "버튼을 찾지 못했습니다." }
    $pattern = $null
    if (-not $Element.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$pattern)) {
        throw "버튼 InvokePattern을 사용할 수 없습니다: $($Element.Current.Name)"
    }
    ([System.Windows.Automation.InvokePattern]$pattern).Invoke()
    Start-Sleep -Milliseconds 500
}

$configPath = Join-Path (Join-Path $env:APPDATA 'EbenTiler') 'config.ini'
$configDir = Split-Path -Parent $configPath
$backupPath = Join-Path $env:TEMP ('ebentiler-dpi-backup-' + [Guid]::NewGuid().ToString('N') + '.ini')
$hadConfig = Test-Path $configPath
if ($hadConfig) { Copy-Item $configPath $backupPath -Force }

$firstProcess = $null
$settingsProcess = $null

try {
    Get-Process EbenTiler -ErrorAction SilentlyContinue | Stop-Process -Force
    if (Test-Path $configPath) { Remove-Item $configPath -Force }

    # 1) 실제 첫 실행 가이드
    $firstProcess = Start-Process -FilePath $exe -PassThru
    $welcomeHwnd = Wait-Window 'EbenTiler for Windows - 시작하기'
    if ($welcomeHwnd -eq [IntPtr]::Zero) { throw "첫 실행 시작 가이드 창을 찾지 못했습니다." }

    $scaleInfo = Assert-ClientSize $welcomeHwnd 650 454 '첫 실행 시작 가이드'
    $targetPercents = @(100, 125, 150)
    if ($targetPercents -notcontains $scaleInfo.Percent) {
        Write-Warning "현재 배율은 $($scaleInfo.Percent)%입니다. 최종 검증은 100%, 125%, 150%에서 각각 실행하세요."
    }

    if ([string]::IsNullOrEmpty($OutDir)) {
        $OutDir = Join-Path $root ('build\screenshots\dpi-' + $scaleInfo.Percent)
    }
    if (-not (Test-Path $OutDir)) { New-Item -ItemType Directory -Path $OutDir -Force | Out-Null }
    Save-Window $welcomeHwnd (Join-Path $OutDir 'welcome-first-run.png')

    $firstProcess | Stop-Process -Force -ErrorAction SilentlyContinue
    $firstProcess = $null
    Start-Sleep -Milliseconds 500

    # 2) 설정 > 일반 화면과 다시 보기 버튼
    $settingsProcess = Start-Process -FilePath $exe -ArgumentList '--settings' -PassThru
    $settingsHwnd = Wait-Window 'EbenTiler for Windows - 설정'
    if ($settingsHwnd -eq [IntPtr]::Zero) { throw "설정 창을 찾지 못했습니다." }
    Assert-ClientSize $settingsHwnd 960 660 '설정 창' | Out-Null

    $general = Find-Button $settingsHwnd '일반'
    if ($null -eq $general) { throw "설정의 '일반' 메뉴를 찾지 못했습니다." }
    Invoke-Button $general

    $reopen = Find-Button $settingsHwnd '시작 가이드 다시 보기'
    if ($null -eq $reopen) { throw "'시작 가이드 다시 보기' 버튼을 찾지 못했습니다." }
    Write-Host "[통과] 설정 > 일반에 시작 가이드 다시 보기 버튼이 있습니다." -ForegroundColor Green
    Save-Window $settingsHwnd (Join-Path $OutDir 'settings-general.png')

    # 3) 설정에서 다시 연 가이드는 첫 실행용 체크박스를 노출하지 않는다.
    Invoke-Button $reopen
    $manualWelcome = Wait-Window 'EbenTiler for Windows - 시작하기'
    if ($manualWelcome -eq [IntPtr]::Zero) { throw "설정에서 시작 가이드를 다시 열지 못했습니다." }
    Assert-ClientSize $manualWelcome 650 454 '설정에서 연 시작 가이드' | Out-Null

    if ($null -ne (Find-Element $manualWelcome '다시 표시하지 않기')) {
        throw "설정에서 다시 연 가이드에 첫 실행 전용 체크박스가 노출됩니다."
    }
    if ($null -eq (Find-Button $manualWelcome '닫기')) {
        throw "설정에서 다시 연 가이드의 닫기 버튼을 찾지 못했습니다."
    }
    Write-Host "[통과] 다시 보기 모드는 설정을 변경하지 않는 단순 안내 화면입니다." -ForegroundColor Green
    Save-Window $manualWelcome (Join-Path $OutDir 'welcome-from-settings.png')

    Write-Host ""
    Write-Host "현재 배율 $($scaleInfo.Percent)% 검증 및 캡처 완료" -ForegroundColor Green
    Write-Host "100% / 125% / 150%에서 각각 실행한 뒤 PNG의 글자 잘림, 겹침, 버튼 잘림을 마지막으로 눈으로 확인하세요."
}
finally {
    if ($null -ne $firstProcess) { $firstProcess | Stop-Process -Force -ErrorAction SilentlyContinue }
    if ($null -ne $settingsProcess) { $settingsProcess | Stop-Process -Force -ErrorAction SilentlyContinue }
    Get-Process EbenTiler -ErrorAction SilentlyContinue | Stop-Process -Force

    if ($hadConfig) {
        if (-not (Test-Path $configDir)) { New-Item -ItemType Directory -Path $configDir -Force | Out-Null }
        Copy-Item $backupPath $configPath -Force
        Remove-Item $backupPath -Force -ErrorAction SilentlyContinue
    }
    elseif (Test-Path $configPath) {
        Remove-Item $configPath -Force
    }
}
