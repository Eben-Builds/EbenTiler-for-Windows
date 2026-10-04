# Tessdeck 설정 > 레이아웃의 사용자 지정 순환 비율을 실제 UI로 저장해 검증한다.
#
#   powershell -ExecutionPolicy Bypass -File tools\verify-layout-settings.ps1

$ErrorActionPreference = 'Stop'

$toolsDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Split-Path -Parent $toolsDir
$exe = Join-Path $root 'build\Tessdeck.exe'
if (-not (Test-Path $exe)) { throw "먼저 build.ps1 로 빌드하세요." }

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms

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
    Get-Process Tessdeck -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

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

    if ($wasResident -and (Test-Path $installedExe) -and -not (Get-Process Tessdeck -ErrorAction SilentlyContinue)) {
        Start-Process -FilePath $installedExe | Out-Null
    }
}

function Find-ByName {
    param($Window, [string[]]$Names)

    $scope = [System.Windows.Automation.TreeScope]::Descendants
    foreach ($name in $Names) {
        $condition = New-Object System.Windows.Automation.PropertyCondition (
            [System.Windows.Automation.AutomationElement]::NameProperty, $name)
        $element = $Window.FindFirst($scope, $condition)
        if ($null -ne $element) { return $element }
    }
    return $null
}

function Invoke-Element {
    param($Element)

    if ($null -eq $Element) { return $false }
    $pattern = $null
    if ($Element.TryGetCurrentPattern(
        [System.Windows.Automation.InvokePattern]::Pattern,
        [ref]$pattern)) {
        ([System.Windows.Automation.InvokePattern]$pattern).Invoke()
        Start-Sleep -Milliseconds 400
        return $true
    }
    return $false
}

function Find-RatioInputs {
    param($Window)

    $result = @()
    foreach ($name in @('첫 번째 순환 비율','두 번째 순환 비율','세 번째 순환 비율')) {
        $element = Find-ByName $Window @($name)
        if ($null -ne $element) { $result += $element }
    }
    if ($result.Count -eq 3) { return $result }

    $spinners = @()
    $all = $Window.FindAll(
        [System.Windows.Automation.TreeScope]::Descendants,
        [System.Windows.Automation.Condition]::TrueCondition)
    foreach ($element in $all) {
        if ($element.Current.ControlType -eq [System.Windows.Automation.ControlType]::Spinner) {
            $spinners += $element
        }
    }

    if ($spinners.Count -ge 3) {
        return @($spinners | Sort-Object { $_.Current.BoundingRectangle.X } | Select-Object -First 3)
    }
    return @()
}

function Set-RatioValue {
    param($Element, [double]$Value)

    $pattern = $null
    if ($Element.TryGetCurrentPattern(
        [System.Windows.Automation.RangeValuePattern]::Pattern,
        [ref]$pattern)) {
        ([System.Windows.Automation.RangeValuePattern]$pattern).SetValue($Value)
        Start-Sleep -Milliseconds 150
        return $true
    }

    try {
        $Element.SetFocus()
        [System.Windows.Forms.SendKeys]::SendWait('^a')
        [System.Windows.Forms.SendKeys]::SendWait(([int]$Value).ToString())
        Start-Sleep -Milliseconds 150
        return $true
    }
    catch {
        return $false
    }
}

$settings = $null
try {
    Get-Process Tessdeck -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    if (Test-Path $configDir) { Remove-Item $configDir -Recurse -Force }

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
    if ($hwnd -eq [IntPtr]::Zero) { throw '설정 창을 찾지 못했습니다.' }

    $window = [System.Windows.Automation.AutomationElement]::FromHandle($hwnd)
    Write-Host "[통과] 설정 창 열림: $($window.Current.Name)" -ForegroundColor Green

    $layout = Find-ByName $window @('레이아웃 설정','레이아웃')
    if (-not (Invoke-Element $layout)) {
        throw '레이아웃 설정 페이지를 열지 못했습니다.'
    }
    Write-Host '[통과] 레이아웃 설정 페이지 열림' -ForegroundColor Green

    $ratios = @(Find-RatioInputs $window)
    if ($ratios.Count -ne 3) {
        throw "순환 비율 입력 상자 3개를 찾지 못했습니다. 발견: $($ratios.Count)"
    }

    $values = @(50,40,60)
    for ($i = 0; $i -lt 3; $i++) {
        if (-not (Set-RatioValue $ratios[$i] $values[$i])) {
            throw "순환 비율 입력 실패: $($values[$i])"
        }
    }
    Write-Host '[통과] 순환 비율 입력: 50 / 40 / 60' -ForegroundColor Green

    $save = Find-ByName $window @('저장')
    if (-not (Invoke-Element $save)) {
        throw '저장 버튼을 누르지 못했습니다.'
    }

    $settings.WaitForExit(5000) | Out-Null
    if (-not (Test-Path $configPath)) {
        throw "설정 파일이 생성되지 않았습니다: $configPath"
    }

    $saved = Get-Content -LiteralPath $configPath -Raw -Encoding UTF8
    if ($saved -notmatch 'CycleRatio1=50') { throw 'CycleRatio1=50 저장 실패' }
    if ($saved -notmatch 'CycleRatio2=40') { throw 'CycleRatio2=40 저장 실패' }
    if ($saved -notmatch 'CycleRatio3=60') { throw 'CycleRatio3=60 저장 실패' }

    Write-Host '[통과] config.ini 저장: CycleRatio1=50 / CycleRatio2=40 / CycleRatio3=60' -ForegroundColor Green
    Write-Host ''
    Write-Host '레이아웃 설정 UI 검증: PASS' -ForegroundColor Green
}
finally {
    if ($null -ne $settings -and -not $settings.HasExited) {
        $settings | Stop-Process -Force -ErrorAction SilentlyContinue
    }
    Restore-TestState
}
