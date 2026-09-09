# 앱 아이콘 시안을 그려서 비교표(대지)를 만든다.
#
#   powershell -ExecutionPolicy Bypass -File tools\make-icons.ps1
#
# 각 시안을 밝은 배경과 어두운 배경 양쪽에, 그리고 실제 쓰이는 크기
# (48 / 32 / 16 픽셀)로 나란히 그려서 어느 쪽이 작게 줄여도 알아보이는지 본다.
# 트레이 아이콘은 16픽셀에서 읽히지 않으면 소용이 없기 때문이다.

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$toolsDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Split-Path -Parent $toolsDir
$outDir = Join-Path $root 'build\icons'
if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir -Force | Out-Null }

# 색
$BLUE     = [System.Drawing.Color]::FromArgb(255,  37,  99, 235)
$BLUEDARK = [System.Drawing.Color]::FromArgb(255,  29,  78, 216)
$INK      = [System.Drawing.Color]::FromArgb(255,  24,  28,  35)
$SLATE    = [System.Drawing.Color]::FromArgb(255,  71,  85, 105)
$WHITE    = [System.Drawing.Color]::FromArgb(255, 255, 255, 255)
$DIM      = [System.Drawing.Color]::FromArgb(255, 148, 163, 184)

function FillR {
    param($g, $color, [double]$x, [double]$y, [double]$w, [double]$h)
    if ($w -lt 1 -or $h -lt 1) { return }
    $b = New-Object System.Drawing.SolidBrush $color
    $g.FillRectangle($b, [int][Math]::Round($x), [int][Math]::Round($y), [int][Math]::Round($w), [int][Math]::Round($h))
    $b.Dispose()
}

function DrawR {
    param($g, $color, [double]$x, [double]$y, [double]$w, [double]$h, [double]$pen)
    $p = New-Object System.Drawing.Pen ($color, [float][Math]::Max(1, [Math]::Round($pen)))
    $p.Alignment = [System.Drawing.Drawing2D.PenAlignment]::Inset
    $g.DrawRectangle($p, [int][Math]::Round($x), [int][Math]::Round($y), [int][Math]::Round($w), [int][Math]::Round($h))
    $p.Dispose()
}

# ── 시안 정의 ───────────────────────────────────────────────────────
# 각 시안은 (Graphics, 한 변 크기)를 받아 그 크기에 맞게 직접 그린다.
# 큰 그림을 줄이지 않고 크기마다 새로 그려야 16픽셀에서도 선이 또렷하다.

$designs = @(
    @{
        Key = 'A'; Name = '좌우 두 장'
        Note = '가장 흔한 배치인 좌우 절반을 그대로 보여준다. 파란 타일이라 어느 작업표시줄에서도 눈에 띈다.'
        Draw = {
            param($g, $s)
            FillR $g $BLUE 0 0 $s $s
            $m = $s * 0.20
            $gap = [Math]::Max(1, [Math]::Round($s * 0.07))
            $inner = $s - $m * 2
            $w = ($inner - $gap) / 2
            FillR $g $WHITE $m $m $w $inner
            FillR $g $WHITE ($m + $w + $gap) $m $w $inner
        }
    },
    @{
        Key = 'B'; Name = '사분면'
        Note = '네 칸으로 나뉜 화면. 기능 범위를 가장 잘 설명하지만 16픽셀에서는 칸이 뭉갠다.'
        Draw = {
            param($g, $s)
            FillR $g $BLUE 0 0 $s $s
            $m = $s * 0.20
            $gap = [Math]::Max(1, [Math]::Round($s * 0.07))
            $inner = $s - $m * 2
            $c = ($inner - $gap) / 2
            FillR $g $WHITE $m $m $c $c
            FillR $g $WHITE ($m + $c + $gap) $m $c $c
            FillR $g $WHITE $m ($m + $c + $gap) $c $c
            FillR $g $WHITE ($m + $c + $gap) ($m + $c + $gap) $c $c
        }
    },
    @{
        Key = 'C'; Name = '창 테두리 + 절반'
        Note = '배경 타일 없이 선으로만 그린 창. 깔끔하지만 어두운 작업표시줄에서는 잘 안 보인다.'
        Draw = {
            param($g, $s)
            $m = [Math]::Max(1, [Math]::Round($s * 0.13))
            $w = $s - $m * 2
            $pen = [Math]::Max(1, [Math]::Round($s * 0.09))
            FillR $g $INK $m $m ($w / 2) $w
            DrawR $g $INK $m $m $w $w $pen
        }
    },
    @{
        Key = 'D'; Name = '3분할'
        Note = '세로 3분할. Rectangle 다운 특징이지만 16픽셀에서 기둥이 붙어 보인다.'
        Draw = {
            param($g, $s)
            FillR $g $BLUE 0 0 $s $s
            $m = $s * 0.20
            $gap = [Math]::Max(1, [Math]::Round($s * 0.055))
            $inner = $s - $m * 2
            $w = ($inner - $gap * 2) / 3
            FillR $g $WHITE $m $m $w $inner
            FillR $g $WHITE ($m + $w + $gap) $m $w $inner
            FillR $g $WHITE ($m + ($w + $gap) * 2) $m $w $inner
        }
    },
    @{
        Key = 'E'; Name = '절반 강조'
        Note = '어두운 타일에 왼쪽 절반만 파랗게. 방금 배치한 쪽이 어디인지 한눈에 들어온다.'
        Draw = {
            param($g, $s)
            FillR $g $INK 0 0 $s $s
            $m = $s * 0.20
            $gap = [Math]::Max(1, [Math]::Round($s * 0.07))
            $inner = $s - $m * 2
            $w = ($inner - $gap) / 2
            FillR $g $BLUE $m $m $w $inner
            FillR $g $DIM ($m + $w + $gap) $m $w $inner
        }
    },
    @{
        Key = 'F'; Name = '겹친 두 창'
        Note = '창을 정리하기 전 모습. 뜻은 살지만 형태가 복잡해 작아지면 덩어리로 보인다.'
        Draw = {
            param($g, $s)
            $pen = [Math]::Max(1, [Math]::Round($s * 0.08))
            $side = $s * 0.58
            DrawR $g $SLATE ($s * 0.08) ($s * 0.08) $side $side $pen
            FillR $g $WHITE ($s * 0.34) ($s * 0.34) $side $side
            DrawR $g $INK ($s * 0.34) ($s * 0.34) $side $side $pen
            FillR $g $INK ($s * 0.34) ($s * 0.34) ($side / 2) $side
        }
    }
)

function Render-Design {
    param($Design, [int]$Size)
    $bmp = New-Object System.Drawing.Bitmap $Size, $Size
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::None
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
    $g.Clear([System.Drawing.Color]::Transparent)
    & $Design.Draw $g $Size
    $g.Dispose()
    return $bmp
}

# ── 개별 PNG 저장 ───────────────────────────────────────────────────
foreach ($d in $designs) {
    foreach ($sz in @(256, 48, 32, 16)) {
        $bmp = Render-Design $d $sz
        $bmp.Save((Join-Path $outDir ("{0}-{1}.png" -f $d.Key, $sz)), [System.Drawing.Imaging.ImageFormat]::Png)
        $bmp.Dispose()
    }
}
Write-Host "시안 PNG 저장: $outDir"

# ── 비교표(대지) 만들기 ─────────────────────────────────────────────
$rowH   = 150
$padX   = 20
$labelW = 210
$bigW   = 120
$sheetW = $labelW + ($bigW + 24) * 2 + 300 + $padX * 2
$sheetH = 70 + $rowH * $designs.Count

$sheet = New-Object System.Drawing.Bitmap $sheetW, $sheetH
$sg = [System.Drawing.Graphics]::FromImage($sheet)
$sg.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$sg.Clear([System.Drawing.Color]::FromArgb(255, 250, 250, 250))

$fontTitle = New-Object System.Drawing.Font 'Malgun Gothic', 13, ([System.Drawing.FontStyle]::Bold)
$fontName  = New-Object System.Drawing.Font 'Malgun Gothic', 11, ([System.Drawing.FontStyle]::Bold)
$fontNote  = New-Object System.Drawing.Font 'Malgun Gothic', 8.5
$fontHead  = New-Object System.Drawing.Font 'Malgun Gothic', 9
$brInk     = New-Object System.Drawing.SolidBrush $INK
$brDim     = New-Object System.Drawing.SolidBrush $SLATE

$sg.DrawString('Rectangle for Windows - 앱 아이콘 시안', $fontTitle, $brInk, $padX, 16)

$x1 = $padX + $labelW
$x2 = $x1 + $bigW + 24
$x3 = $x2 + $bigW + 40
$sg.DrawString('밝은 배경', $fontHead, $brDim, $x1, 48)
$sg.DrawString('어두운 배경', $fontHead, $brDim, $x2, 48)
$sg.DrawString('실제 크기 (48 / 32 / 16px)', $fontHead, $brDim, $x3, 48)

$y = 70
foreach ($d in $designs) {
    # 구분선
    $pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 224, 224, 224)), 1
    $sg.DrawLine($pen, $padX, $y, $sheetW - $padX, $y)
    $pen.Dispose()

    $sg.DrawString(("{0}. {1}" -f $d.Key, $d.Name), $fontName, $brInk, $padX, ($y + 14))
    $rect = New-Object System.Drawing.RectangleF ($padX, ($y + 38), ($labelW - 16), 100)
    $sg.DrawString($d.Note, $fontNote, $brDim, $rect)

    # 밝은 배경 / 어두운 배경
    FillR $sg ([System.Drawing.Color]::FromArgb(255, 243, 243, 243)) $x1 ($y + 14) $bigW $bigW
    FillR $sg ([System.Drawing.Color]::FromArgb(255,  32,  32,  32)) $x2 ($y + 14) $bigW $bigW

    $big = Render-Design $d 256
    $sg.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $sg.DrawImage($big, [int]($x1 + 12), [int]($y + 26), 96, 96)
    $sg.DrawImage($big, [int]($x2 + 12), [int]($y + 26), 96, 96)
    $big.Dispose()

    # 실제 크기: 밝은 줄 / 어두운 줄 두 벌
    $cx = $x3
    foreach ($sz in @(48, 32, 16)) {
        $small = Render-Design $d $sz
        FillR $sg ([System.Drawing.Color]::FromArgb(255, 243, 243, 243)) $cx ($y + 20) 56 56
        FillR $sg ([System.Drawing.Color]::FromArgb(255,  32,  32,  32)) $cx ($y + 80) 56 56
        $ox = $cx + (56 - $sz) / 2
        $sg.DrawImage($small, [int]$ox, [int]($y + 20 + (56 - $sz) / 2))
        $sg.DrawImage($small, [int]$ox, [int]($y + 80 + (56 - $sz) / 2))
        $small.Dispose()
        $sg.DrawString(("{0}px" -f $sz), $fontNote, $brDim, ($cx + 10), ($y + 138))
        $cx += 66
    }

    $y += $rowH
}

$sg.Dispose()
$sheetPath = Join-Path $outDir 'candidates.png'
$sheet.Save($sheetPath, [System.Drawing.Imaging.ImageFormat]::Png)
$sheet.Dispose()
Write-Host "비교표 저장: $sheetPath"
