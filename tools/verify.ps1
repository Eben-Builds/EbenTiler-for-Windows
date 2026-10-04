# 실제 창을 띄워 배치 명령이 제대로 동작하는지 자동으로 확인한다.
#
#   powershell -ExecutionPolicy Bypass -File tools\verify.ps1
#
# 메모장 창을 하나 띄운 뒤 명령을 하나씩 적용하고, 창이 실제로 놓인 자리를
# 화면 작업 영역으로부터 계산한 기대 위치와 비교한다.

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$exe  = Join-Path $root 'build\Tessdeck.exe'

if (-not (Test-Path $exe)) {
    throw "먼저 build.ps1 로 빌드하세요: $exe 없음"
}

$tolerance = 2

# Tessdeck.exe 는 창 프로그램이라 표준 출력이 파이프로 잡히지 않는다.
# --out 으로 임시 파일에 결과를 남기게 하고 그 파일을 읽는다.
function Invoke-Tessdeck {
    param([string[]]$Arguments)
    $tmp = [System.IO.Path]::GetTempFileName()
    try {
        Start-Process -FilePath $exe -ArgumentList ($Arguments + @('--out', $tmp)) -Wait -WindowStyle Hidden | Out-Null
        if (Test-Path $tmp) { return [System.IO.File]::ReadAllText($tmp, [System.Text.Encoding]::UTF8) }
        return ''
    } finally { Remove-Item $tmp -Force -ErrorAction SilentlyContinue }
}

function Parse-Rect {
    param([string]$Text, [string]$Prefix)
    foreach ($line in $Text -split "`r?`n") {
        $line = $line.Trim()
        if ($line.StartsWith($Prefix)) {
            $nums = $line.Substring($Prefix.Length) -split ','
            if ($nums.Count -ge 4) {
                return [PSCustomObject]@{ X=[int]$nums[0]; Y=[int]$nums[1]; Width=[int]$nums[2]; Height=[int]$nums[3] }
            }
        }
    }
    return $null
}

Write-Host "검증용 창을 띄웁니다..."
$handleFile = Join-Path $env:TEMP ("ebentiler-verify-" + [Guid]::NewGuid().ToString('N') + ".txt")
$hostScript = Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) '_testwindow.ps1'
$proc = Start-Process powershell -PassThru -ArgumentList @('-ExecutionPolicy','Bypass','-NoProfile','-File',$hostScript,$handleFile)

$hwnd = [IntPtr]::Zero
for ($i=0; $i -lt 100; $i++) { Start-Sleep -Milliseconds 100; if (Test-Path $handleFile) { $raw=(Get-Content $handleFile -Raw).Trim(); if ($raw.Length -gt 0) { $hwnd=[IntPtr][long]$raw; break } } }
if ($hwnd -eq [IntPtr]::Zero) { $proc | Stop-Process -Force -ErrorAction SilentlyContinue; throw "검증용 창 핸들을 얻지 못했습니다." }
$handleArg = '0x' + $hwnd.ToInt64().ToString('X')
$infoText = Invoke-Tessdeck @('--info','--hwnd',$handleArg)
$work = Parse-Rect $infoText 'work='
if ($null -eq $work) { $proc | Stop-Process -Force; throw "작업 영역을 읽지 못했습니다.`n$infoText" }

$x=$work.X; $y=$work.Y; $w=$work.Width; $h=$work.Height
$halfW=[int][Math]::Truncate($w/2); $halfH=[int][Math]::Truncate($h/2); $thirdW=[int][Math]::Truncate($w/3); $twoThird=[int][Math]::Truncate(($w*2)/3); $roundW=[int][Math]::Round($w*0.5); $roundH=[int][Math]::Round($h*0.5)
$cases=@(
@{Action='LeftHalf';Expect=@($x,$y,$roundW,$h)},@{Action='RightHalf';Expect=@(($x+$w-$roundW),$y,$roundW,$h)},@{Action='TopHalf';Expect=@($x,$y,$w,$roundH)},@{Action='BottomHalf';Expect=@($x,($y+$h-$roundH),$w,$roundH)},@{Action='TopLeft';Expect=@($x,$y,$halfW,$halfH)},@{Action='TopRight';Expect=@(($x+$halfW),$y,($w-$halfW),$halfH)},@{Action='BottomLeft';Expect=@($x,($y+$halfH),$halfW,($h-$halfH))},@{Action='BottomRight';Expect=@(($x+$halfW),($y+$halfH),($w-$halfW),($h-$halfH))},@{Action='FirstThird';Expect=@($x,$y,$thirdW,$h)},@{Action='CenterThird';Expect=@(($x+$thirdW),$y,$thirdW,$h)},@{Action='LastThird';Expect=@(($x+$twoThird),$y,($w-$twoThird),$h)},@{Action='FirstTwoThirds';Expect=@($x,$y,$twoThird,$h)},@{Action='LastTwoThirds';Expect=@(($x+$thirdW),$y,($w-$thirdW),$h)})
$pass=0; $fail=0
foreach($case in $cases){ Invoke-Tessdeck @('--apply',$case.Action,'--hwnd',$handleArg)|Out-Null; Start-Sleep -Milliseconds 120; $after=Parse-Rect (Invoke-Tessdeck @('--info','--hwnd',$handleArg)) 'window='; $e=$case.Expect; if($null -ne $after -and [Math]::Abs($after.X-$e[0]) -le $tolerance -and [Math]::Abs($after.Y-$e[1]) -le $tolerance -and [Math]::Abs($after.Width-$e[2]) -le $tolerance -and [Math]::Abs($after.Height-$e[3]) -le $tolerance){Write-Host ("[통과] {0}" -f $case.Action) -ForegroundColor Green;$pass++}else{Write-Host ("[실패] {0}" -f $case.Action) -ForegroundColor Red;$fail++}}
Add-Type -Namespace Win -Name U -MemberDefinition '[DllImport("user32.dll")] public static extern bool IsZoomed(IntPtr h);'
Invoke-Tessdeck @('--apply','Maximize','--hwnd',$handleArg)|Out-Null; Start-Sleep -Milliseconds 200; if([Win.U]::IsZoomed($hwnd)){$pass++;Write-Host '[통과] Maximize' -ForegroundColor Green}else{$fail++;Write-Host '[실패] Maximize' -ForegroundColor Red}
Invoke-Tessdeck @('--apply','Restore','--hwnd',$handleArg)|Out-Null; Start-Sleep -Milliseconds 200; if(-not [Win.U]::IsZoomed($hwnd)){$pass++;Write-Host '[통과] Restore' -ForegroundColor Green}else{$fail++;Write-Host '[실패] Restore' -ForegroundColor Red}
$before=Parse-Rect (Invoke-Tessdeck @('--info','--hwnd',$handleArg)) 'window='; Invoke-Tessdeck @('--apply','Smaller','--hwnd',$handleArg)|Out-Null; Start-Sleep -Milliseconds 150; $smaller=Parse-Rect (Invoke-Tessdeck @('--info','--hwnd',$handleArg)) 'window='; if($smaller.Width -lt $before.Width -and $smaller.Height -lt $before.Height){$pass++}else{$fail++}
Invoke-Tessdeck @('--apply','Larger','--hwnd',$handleArg)|Out-Null; Start-Sleep -Milliseconds 150; $larger=Parse-Rect (Invoke-Tessdeck @('--info','--hwnd',$handleArg)) 'window='; if($larger.Width -gt $smaller.Width -and $larger.Height -gt $smaller.Height){$pass++}else{$fail++}
Invoke-Tessdeck @('--apply','Center','--hwnd',$handleArg)|Out-Null; Start-Sleep -Milliseconds 150; $centered=Parse-Rect (Invoke-Tessdeck @('--info','--hwnd',$handleArg)) 'window='; $expectX=$work.X+[int](($work.Width-$centered.Width)/2);$expectY=$work.Y+[int](($work.Height-$centered.Height)/2);if([Math]::Abs($centered.X-$expectX)-le $tolerance -and [Math]::Abs($centered.Y-$expectY)-le $tolerance){$pass++}else{$fail++}
Write-Host ""; Write-Host ("결과: 통과 {0} / 실패 {1}" -f $pass,$fail)
$proc|Stop-Process -Force -ErrorAction SilentlyContinue; Remove-Item $handleFile -ErrorAction SilentlyContinue
if($fail -gt 0){exit 1}else{exit 0}
