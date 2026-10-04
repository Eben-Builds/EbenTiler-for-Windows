param(
    [ValidateSet('Before','After','Current')]
    [string]$Mode = 'Current',
    [string]$ExpectedVersion = '1.1.2'
)

$ErrorActionPreference = 'Stop'

$qaRoot = Join-Path $env:TEMP 'Tessdeck-Upgrade-QA'
$baselinePath = Join-Path $qaRoot 'baseline.json'
$legacyInstallDir = Join-Path $env:LOCALAPPDATA 'Programs\EbenTiler'
$modernInstallDir = Join-Path $env:LOCALAPPDATA 'Programs\Tessdeck'
$legacyConfigDir = Join-Path $env:APPDATA 'EbenTiler'
$modernConfigDir = Join-Path $env:APPDATA 'Tessdeck'
$runKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'

function Get-RunValue {
    param([string]$Name)
    if (-not (Test-Path $runKey)) { return $null }
    $props = Get-ItemProperty -Path $runKey -ErrorAction SilentlyContinue
    if ($null -eq $props) { return $null }
    $p = $props.PSObject.Properties[$Name]
    if ($null -eq $p) { return $null }
    return [string]$p.Value
}

function Find-InstalledExe {
    $candidates = @(
        (Join-Path $modernInstallDir 'Tessdeck.exe'),
        (Join-Path $legacyInstallDir 'Tessdeck.exe'),
        (Join-Path $legacyInstallDir 'EbenTiler.exe')
    )
    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) { return $candidate }
    }
    return $null
}

function Find-Config {
    foreach ($candidate in @(
        (Join-Path $modernConfigDir 'config.ini'),
        (Join-Path $legacyConfigDir 'config.ini')
    )) {
        if (Test-Path $candidate) { return $candidate }
    }
    return $null
}

function Read-ConfigMap {
    param([string]$Path)
    $map = [ordered]@{}
    if ([string]::IsNullOrWhiteSpace($Path) -or -not (Test-Path $Path)) {
        return $map
    }

    foreach ($line in (Get-Content -LiteralPath $Path -Encoding UTF8)) {
        $trimmed = $line.Trim()
        if ($trimmed.Length -eq 0 -or
            $trimmed.StartsWith(';') -or
            $trimmed.StartsWith('#') -or
            $trimmed.StartsWith('[')) {
            continue
        }

        $eq = $trimmed.IndexOf('=')
        if ($eq -le 0) { continue }
        $key = $trimmed.Substring(0, $eq).Trim()
        $value = $trimmed.Substring($eq + 1).Trim()
        $map[$key] = $value
    }
    return $map
}

function Get-State {
    $exe = Find-InstalledExe
    $config = Find-Config
    $version = $null
    if ($exe) {
        $version = [Diagnostics.FileVersionInfo]::GetVersionInfo($exe).FileVersion
    }

    $startupEben = Get-RunValue 'EbenTiler'
    $startupTess = Get-RunValue 'Tessdeck'

    [PSCustomObject]@{
        CapturedAt = (Get-Date).ToString('o')
        Exe = $exe
        Version = $version
        Config = $config
        ConfigValues = Read-ConfigMap $config
        StartupEnabled = (-not [string]::IsNullOrWhiteSpace($startupEben)) -or (-not [string]::IsNullOrWhiteSpace($startupTess))
        StartupEbenTiler = $startupEben
        StartupTessdeck = $startupTess
        LegacyInstallExists = Test-Path $legacyInstallDir
        ModernInstallExists = Test-Path $modernInstallDir
        LegacyConfigExists = Test-Path $legacyConfigDir
        ModernConfigExists = Test-Path $modernConfigDir
    }
}

function Pass([string]$Message) {
    Write-Host "[PASS] $Message" -ForegroundColor Green
}

function Fail([string]$Message) {
    Write-Host "[FAIL] $Message" -ForegroundColor Red
    $script:Failed++
}

function Info([string]$Message) {
    Write-Host "[INFO] $Message" -ForegroundColor Cyan
}

function Compare-VersionAtLeast {
    param([string]$Actual, [string]$Expected)
    if ([string]::IsNullOrWhiteSpace($Actual)) { return $false }
    try {
        return ([Version]$Actual) -ge ([Version]$Expected)
    }
    catch {
        return $false
    }
}

function Show-State {
    param($State)

    Info "실행 파일: $($State.Exe)"
    Info "버전: $($State.Version)"
    Info "설정 파일: $($State.Config)"
    Info "자동 시작: $($State.StartupEnabled)"
    Info "EbenTiler 설치 폴더 존재: $($State.LegacyInstallExists)"
    Info "Tessdeck 설치 폴더 존재: $($State.ModernInstallExists)"
    Info "EbenTiler 설정 폴더 존재: $($State.LegacyConfigExists)"
    Info "Tessdeck 설정 폴더 존재: $($State.ModernConfigExists)"
}

New-Item -ItemType Directory -Path $qaRoot -Force | Out-Null
$Failed = 0

if ($Mode -eq 'Before') {
    $state = Get-State
    if (-not $state.Exe) {
        throw '현재 설치된 EbenTiler/Tessdeck 실행 파일을 찾지 못했습니다.'
    }

    $state | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $baselinePath -Encoding UTF8
    Write-Host ''
    Write-Host 'Tessdeck 업그레이드 전 상태를 저장했습니다.' -ForegroundColor Green
    Show-State $state
    Write-Host ''
    Write-Host "스냅샷: $baselinePath"
    Write-Host ''
    Write-Host '이제 앱에서 설정 > 정보 > 업데이트 확인을 누른 뒤 v1.1.2 설치파일을 실행하세요.'
    Write-Host '설치가 끝나면 다음 명령을 실행하세요:'
    Write-Host 'powershell -ExecutionPolicy Bypass -File tools\verify-local-upgrade.ps1 -Mode After'
    exit 0
}

if ($Mode -eq 'Current') {
    $state = Get-State
    Write-Host ''
    Show-State $state

    if ($state.Exe -and (Compare-VersionAtLeast $state.Version $ExpectedVersion)) {
        Pass "설치 버전이 $ExpectedVersion 이상입니다."
    }
    elseif ($state.Exe) {
        Fail "설치 버전이 $ExpectedVersion 미만입니다: $($state.Version)"
    }
    else {
        Fail '설치된 Tessdeck/EbenTiler 실행 파일을 찾지 못했습니다.'
    }

    if ($state.ModernInstallExists) { Pass 'Tessdeck 기본 설치 폴더가 존재합니다.' }
    if (-not $state.LegacyInstallExists) { Pass '기존 EbenTiler 기본 설치 폴더가 남아 있지 않습니다.' }
    if ([string]::IsNullOrWhiteSpace($state.StartupEbenTiler)) { Pass 'EbenTiler 자동시작 Registry 값이 없습니다.' }

    Write-Host ''
    if ($Failed -eq 0) {
        Write-Host '현재 설치 상태 자동 점검: PASS' -ForegroundColor Green
        exit 0
    }
    Write-Host "현재 설치 상태 자동 점검: FAIL ($Failed)" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $baselinePath)) {
    throw "업데이트 전 스냅샷이 없습니다. 먼저 -Mode Before 를 실행하세요: $baselinePath"
}

$before = Get-Content -LiteralPath $baselinePath -Raw -Encoding UTF8 | ConvertFrom-Json
$after = Get-State

Write-Host ''
Write-Host 'Tessdeck 실제 PC 업그레이드 QA' -ForegroundColor White
Write-Host '================================'
Show-State $after
Write-Host ''

if ($after.Exe -and $after.Exe.StartsWith($modernInstallDir, [StringComparison]::OrdinalIgnoreCase)) {
    Pass '실행 파일이 Tessdeck 기본 설치 경로에 있습니다.'
} else {
    Fail "실행 파일 경로가 예상과 다릅니다: $($after.Exe)"
}

if (Compare-VersionAtLeast $after.Version $ExpectedVersion) {
    Pass "버전이 $ExpectedVersion 이상입니다: $($after.Version)"
} else {
    Fail "버전이 $ExpectedVersion 이상이 아닙니다: $($after.Version)"
}

if (-not $after.LegacyInstallExists) {
    Pass '기존 EbenTiler 기본 설치 폴더가 정리되었습니다.'
} else {
    Fail "기존 EbenTiler 기본 설치 폴더가 남아 있습니다: $legacyInstallDir"
}

if ([string]::IsNullOrWhiteSpace($after.StartupEbenTiler)) {
    Pass '기존 EbenTiler 자동시작 Registry 값이 제거되었습니다.'
} else {
    Fail "기존 EbenTiler 자동시작 값이 남아 있습니다: $($after.StartupEbenTiler)"
}

if ([bool]$before.StartupEnabled -eq [bool]$after.StartupEnabled) {
    Pass "자동시작 ON/OFF 상태가 유지되었습니다: $($after.StartupEnabled)"
} else {
    Fail "자동시작 상태가 바뀌었습니다. 이전=$($before.StartupEnabled), 이후=$($after.StartupEnabled)"
}

if ($after.StartupEnabled) {
    $expectedRun = '"' + $after.Exe + '"'
    if ($after.StartupTessdeck -eq $expectedRun) {
        Pass '자동시작 경로가 현재 Tessdeck.exe와 일치합니다.'
    } else {
        Fail "자동시작 경로가 다릅니다. 예상=$expectedRun, 실제=$($after.StartupTessdeck)"
    }
}

if ($after.Config -and $after.Config.StartsWith($modernConfigDir, [StringComparison]::OrdinalIgnoreCase)) {
    Pass '설정 파일이 %APPDATA%\Tessdeck에 있습니다.'
} else {
    Fail "설정 파일 위치가 예상과 다릅니다: $($after.Config)"
}

$beforeMap = @{}
if ($before.ConfigValues) {
    foreach ($p in $before.ConfigValues.PSObject.Properties) {
        $beforeMap[$p.Name] = [string]$p.Value
    }
}
$afterMap = @{}
if ($after.ConfigValues) {
    foreach ($p in $after.ConfigValues.PSObject.Properties) {
        $afterMap[$p.Name] = [string]$p.Value
    }
}

$configMismatch = 0
foreach ($key in $beforeMap.Keys) {
    if (-not $afterMap.ContainsKey($key) -or $afterMap[$key] -ne $beforeMap[$key]) {
        Write-Host "[FAIL] 설정 불일치: $key / 이전='$($beforeMap[$key])' 이후='$($afterMap[$key])'" -ForegroundColor Red
        $configMismatch++
        $Failed++
    }
}
if ($configMismatch -eq 0) {
    Pass "기존 설정값 $($beforeMap.Count)개가 유지되었습니다."
}

Write-Host ''
Write-Host '사람이 눈으로 확인할 항목 3개:' -ForegroundColor Yellow
Write-Host '  1. 설정 > 정보에 버전 1.1.2가 표시되는가'
Write-Host '  2. 작업표시줄/트레이 아이콘이 새 Tessdeck 아이콘으로 보이는가'
Write-Host '  3. 평소 쓰던 단축키로 창 배치가 실제로 동작하는가'
Write-Host ''

if ($Failed -eq 0) {
    Write-Host '자동 업그레이드 QA: PASS' -ForegroundColor Green
    Remove-Item $baselinePath -Force -ErrorAction SilentlyContinue
    exit 0
}

Write-Host "자동 업그레이드 QA: FAIL ($Failed)" -ForegroundColor Red
Write-Host "스냅샷은 원인 확인을 위해 유지합니다: $baselinePath"
exit 1
