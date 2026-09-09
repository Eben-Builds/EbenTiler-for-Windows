# 최종 앱 아이콘(시안 F - 겹친 두 창)을 그려서 멀티 사이즈 .ico 로 만든다.
#
#   powershell -ExecutionPolicy Bypass -File tools\make-appicon.ps1
#
# 크기마다 새로 그린다. 큰 그림 하나를 줄이면 16픽셀에서 선이 뭉개지기 때문이다.
# 특히 16/24픽셀은 요소를 덜어낸 단순화 버전으로 그린다.
#
# 결과물: assets\app.ico  (빌드 때 실행 파일에 박힌다)
#         build\icons\appicon-preview.png  (눈으로 확인용)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$toolsDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$root      = Split-Path -Parent $toolsDir
$assetsDir = Join-Path $root 'assets'
$previewDir = Join-Path $root 'build\icons'
foreach ($d in @($assetsDir, $previewDir)) {
    if (-not (Test-Path $d)) { New-Item -ItemType Directory -Path $d -Force | Out-Null }
}

$BLUE  = [System.Drawing.Color]::FromArgb(255,  37,  99, 235)
$INK   = [System.Drawing.Color]::FromArgb(255,  22,  27,  34)
$WHITE = [System.Drawing.Color]::FromArgb(255, 255, 255, 255)
# 앞 창 테두리 색. 어두운 작업표시줄에서 검정 테두리는 배경에 묻히므로 파랑을 쓴다.
$OUTLINE = $BLUE

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
    $g.DrawRectangle($p, [int][Math]::Round($x), [int][Math]::Round($y), [int][Math]::Round($w) - 1, [int][Math]::Round($h) - 1)
    $p.Dispose()
}

# 뒤 창과 앞 창 사이를 투명하게 파내서 두 장이 겹친 것으로 보이게 한다.
function EraseR {
    param($g, [double]$x, [double]$y, [double]$w, [double]$h)
    $old = $g.CompositingMode
    $g.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
    $b = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::Transparent)
    $g.FillRectangle($b, [int][Math]::Round($x), [int][Math]::Round($y), [int][Math]::Round($w), [int][Math]::Round($h))
    $b.Dispose()
    $g.CompositingMode = $old
}

function Draw-Icon {
    param($g, [int]$s)

    if ($s -le 24) {
        # 작은 크기: 요소를 최대한 키우고 테두리를 얇게 해서 형태만 남긴다.
        $side = [Math]::Round($s * 0.68)
        $off  = $s - $side
        $half = [Math]::Floor($side / 2)
        $bp   = [Math]::Max(1, [Math]::Round($s * 0.10))

        DrawR $g $BLUE 0 0 $side $side $bp
        EraseR $g ($off - 1) ($off - 1) ($side + 1) ($side + 1)

        FillR $g $BLUE  $off $off $half $side
        FillR $g $WHITE ($off + $half) $off ($side - $half) $side
        DrawR $g $OUTLINE $off $off $side $side 1
        return
    }

    # 보통 크기: 뒤 창은 선으로, 앞 창은 채워서 그린다.
    $pen   = [Math]::Max(2, [Math]::Round($s * 0.07))
    $edge  = [Math]::Round($s * 0.03)
    $back  = $s * 0.58
    $front = $s * 0.64
    $fx = $s - $edge - $front
    $fy = $s - $edge - $front

    DrawR $g $BLUE $edge $edge $back $back $pen

    $gapv = [Math]::Max(1, [Math]::Round($s * 0.045))
    EraseR $g ($fx - $gapv) ($fy - $gapv) ($front + $gapv * 2) ($front + $gapv * 2)

    $half = [Math]::Round($front / 2)
    FillR $g $BLUE  $fx $fy $half $front
    FillR $g $WHITE ($fx + $half) $fy ($front - $half) $front
    DrawR $g $OUTLINE $fx $fy $front $front $pen
}

function Render {
    param([int]$Size)
    $bmp = New-Object System.Drawing.Bitmap $Size, $Size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::None
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
    $g.Clear([System.Drawing.Color]::Transparent)
    Draw-Icon $g $Size
    $g.Dispose()
    return $bmp
}

# ── 멀티 사이즈 .ico 만들기 ─────────────────────────────────────────
# .ico 는 여러 크기의 그림을 한 파일에 담는다. 각 그림을 PNG 로 넣는다.
function New-IcoFile {
    param([int[]]$Sizes, [string]$Path)

    $blobs = @()
    foreach ($s in $Sizes) {
        $bmp = Render $s
        $ms = New-Object System.IO.MemoryStream
        $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
        $blobs += , @{ Size = $s; Bytes = $ms.ToArray() }
        $ms.Dispose()
        $bmp.Dispose()
    }

    $fs = [System.IO.File]::Create($Path)
    $bw = New-Object System.IO.BinaryWriter $fs
    try {
        $bw.Write([UInt16]0)                 # 예약
        $bw.Write([UInt16]1)                 # 종류: 아이콘
        $bw.Write([UInt16]$blobs.Count)      # 그림 개수

        $offset = 6 + 16 * $blobs.Count
        foreach ($b in $blobs) {
            $dim = $b.Size
            if ($dim -ge 256) { $dim = 0 }   # 256 은 0 으로 적는 규칙
            $bw.Write([Byte]$dim)            # 가로
            $bw.Write([Byte]$dim)            # 세로
            $bw.Write([Byte]0)               # 색 수 (32비트라 0)
            $bw.Write([Byte]0)               # 예약
            $bw.Write([UInt16]1)             # 플레인
            $bw.Write([UInt16]32)            # 비트 수
            $bw.Write([UInt32]$b.Bytes.Length)
            $bw.Write([UInt32]$offset)
            $offset += $b.Bytes.Length
        }
        foreach ($b in $blobs) { $bw.Write($b.Bytes) }
    }
    finally {
        $bw.Flush(); $bw.Dispose(); $fs.Dispose()
    }
}

$icoPath = Join-Path $assetsDir 'app.ico'
New-IcoFile -Sizes @(16, 24, 32, 48, 64, 128, 256) -Path $icoPath
Write-Host ("아이콘 생성: {0}  ({1:N0} 바이트)" -f $icoPath, (Get-Item $icoPath).Length)

# ── 확인용 미리보기 ─────────────────────────────────────────────────
$sizes = @(256, 64, 48, 32, 24, 16)
$cell = 150
$w = 60 + $cell * $sizes.Count
$h = 360
$sheet = New-Object System.Drawing.Bitmap $w, $h
$sg = [System.Drawing.Graphics]::FromImage($sheet)
$sg.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$sg.Clear([System.Drawing.Color]::FromArgb(255, 250, 250, 250))

$fTitle = New-Object System.Drawing.Font 'Malgun Gothic', 12, ([System.Drawing.FontStyle]::Bold)
$fSmall = New-Object System.Drawing.Font 'Malgun Gothic', 9
$brInk  = New-Object System.Drawing.SolidBrush $INK
$brDim  = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 100, 110, 125))
$sg.DrawString('시안 F - 겹친 두 창 (크기별 실제 렌더)', $fTitle, $brInk, 24, 16)
$sg.DrawString('위: 밝은 배경   아래: 어두운 배경', $fSmall, $brDim, 24, 40)

$x = 40
foreach ($s in $sizes) {
    $box = 120
    FillR $sg ([System.Drawing.Color]::FromArgb(255, 243, 243, 243)) $x 70 $box $box
    FillR $sg ([System.Drawing.Color]::FromArgb(255,  32,  32,  32)) $x 200 $box $box

    $img = Render $s
    if ($s -gt $box) {
        $sg.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $sg.DrawImage($img, [int]($x + 12), 82, 96, 96)
        $sg.DrawImage($img, [int]($x + 12), 212, 96, 96)
    } else {
        $ox = $x + ($box - $s) / 2
        $sg.DrawImage($img, [int]$ox, [int](70 + ($box - $s) / 2))
        $sg.DrawImage($img, [int]$ox, [int](200 + ($box - $s) / 2))
    }
    $img.Dispose()
    $sg.DrawString(("{0}px" -f $s), $fSmall, $brDim, ($x + 40), 326)
    $x += $cell
}

# 작은 크기는 픽셀이 어디에 찍혔는지 봐야 하므로 확대해서 한 번 더 보여준다.
$zoomSizes = @(16, 24, 32)
$zx = 40
$zoomSheetW = 60 + 260 * $zoomSizes.Count
$zoom = New-Object System.Drawing.Bitmap $zoomSheetW, 320
$zg = [System.Drawing.Graphics]::FromImage($zoom)
$zg.Clear([System.Drawing.Color]::FromArgb(255, 250, 250, 250))
$zg.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$zg.DrawString('작은 크기 픽셀 확대 (왼쪽 밝은 배경 / 오른쪽 어두운 배경)', $fTitle, $brInk, 24, 16)
$zg.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$zg.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
foreach ($s in $zoomSizes) {
    $img = Render $s
    $scale = [int](192 / $s)
    $side = $s * $scale
    FillR $zg ([System.Drawing.Color]::FromArgb(255, 243, 243, 243)) $zx 60 $side $side
    $zg.DrawImage($img, [int]$zx, 60, $side, $side)
    FillR $zg ([System.Drawing.Color]::FromArgb(255, 32, 32, 32)) ($zx + $side + 10) 60 $side $side
    $zg.DrawImage($img, [int]($zx + $side + 10), 60, $side, $side)
    $img.Dispose()
    $zg.DrawString(("{0}px (x{1})" -f $s, $scale), $fSmall, $brDim, $zx, (70 + $side))
    $zx += 260
}
$zg.Dispose()
$zoomPath = Join-Path $previewDir 'appicon-zoom.png'
$zoom.Save($zoomPath, [System.Drawing.Imaging.ImageFormat]::Png)
$zoom.Dispose()
Write-Host "확대 미리보기: $zoomPath"
$sg.Dispose()
$previewPath = Join-Path $previewDir 'appicon-preview.png'
$sheet.Save($previewPath, [System.Drawing.Imaging.ImageFormat]::Png)
$sheet.Dispose()
Write-Host "미리보기: $previewPath"
