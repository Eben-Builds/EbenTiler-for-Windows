param(
    [switch]$NoPreview
)

# 랜딩페이지 favicon.svg와 같은 EbenTiler 아이콘을 멀티 사이즈 .ico로 만든다.
# 결과물: assets\app.ico
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$toolsDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Split-Path -Parent $toolsDir
$assetsDir = Join-Path $root 'assets'
$previewDir = Join-Path $root 'build\icons'
if (-not (Test-Path $assetsDir)) { New-Item -ItemType Directory -Path $assetsDir -Force | Out-Null }
if (-not $NoPreview -and -not (Test-Path $previewDir)) { New-Item -ItemType Directory -Path $previewDir -Force | Out-Null }

function New-RoundedPath {
    param([single]$X, [single]$Y, [single]$W, [single]$H, [single]$Radius)

    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $diameter = [Math]::Min($Radius * 2.0, [Math]::Min($W, $H))
    if ($diameter -le 0) {
        $path.AddRectangle((New-Object System.Drawing.RectangleF $X, $Y, $W, $H))
        return $path
    }

    $arc = New-Object System.Drawing.RectangleF $X, $Y, $diameter, $diameter
    $path.AddArc($arc, 180, 90)
    $arc.X = $X + $W - $diameter
    $path.AddArc($arc, 270, 90)
    $arc.Y = $Y + $H - $diameter
    $path.AddArc($arc, 0, 90)
    $arc.X = $X
    $path.AddArc($arc, 90, 90)
    $path.CloseFigure()
    return $path
}

function Render-Icon {
    param([int]$Size)

    $bmp = New-Object System.Drawing.Bitmap $Size, $Size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    try {
        $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $g.Clear([System.Drawing.Color]::Transparent)

        $s = $Size / 64.0
        $main = New-Object System.Drawing.RectangleF ([single](5*$s)), ([single](8*$s)), ([single](34*$s)), ([single](48*$s))
        $top = New-Object System.Drawing.RectangleF ([single](42*$s)), ([single](14*$s)), ([single](17*$s)), ([single](19*$s))
        $bottom = New-Object System.Drawing.RectangleF ([single](42*$s)), ([single](37*$s)), ([single](17*$s)), ([single](19*$s))

        $mainPath = New-RoundedPath $main.X $main.Y $main.Width $main.Height ([single](9*$s))
        $gradient = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
            $main,
            ([System.Drawing.Color]::FromArgb(255, 10, 133, 255)),
            ([System.Drawing.Color]::FromArgb(255, 0, 90, 216)),
            [System.Drawing.Drawing2D.LinearGradientMode]::ForwardDiagonal)
        $g.FillPath($gradient, $mainPath)
        $gradient.Dispose()
        $mainPath.Dispose()

        foreach ($part in @(
            @{ Rect=$top; Color=[System.Drawing.Color]::FromArgb(255,102,181,255) },
            @{ Rect=$bottom; Color=[System.Drawing.Color]::FromArgb(255,32,139,244) }
        )) {
            $r = $part.Rect
            $path = New-RoundedPath $r.X $r.Y $r.Width $r.Height ([single](6*$s))
            $brush = New-Object System.Drawing.SolidBrush $part.Color
            $g.FillPath($brush, $path)
            $brush.Dispose()
            $path.Dispose()
        }

        $stroke = [single][Math]::Max(1.8, 5*$s)
        $pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::White), $stroke
        $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
        $pen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
        $points = New-Object 'System.Drawing.PointF[]' 3
        $points[0] = New-Object System.Drawing.PointF ([single](25*$s)), ([single](24*$s))
        $points[1] = New-Object System.Drawing.PointF ([single](34*$s)), ([single](32*$s))
        $points[2] = New-Object System.Drawing.PointF ([single](25*$s)), ([single](40*$s))
        $g.DrawLines($pen, $points)
        $pen.Dispose()
    }
    finally {
        $g.Dispose()
    }
    return $bmp
}

function New-IcoFile {
    param([int[]]$Sizes, [string]$Path)

    $blobs = @()
    foreach ($size in $Sizes) {
        $bmp = Render-Icon $size
        try {
            $ms = New-Object System.IO.MemoryStream
            try {
                $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
                $blobs += , @{ Size = $size; Bytes = $ms.ToArray() }
            }
            finally {
                $ms.Dispose()
            }
        }
        finally {
            $bmp.Dispose()
        }
    }

    $fs = [System.IO.File]::Create($Path)
    $bw = New-Object System.IO.BinaryWriter $fs
    try {
        $bw.Write([UInt16]0)
        $bw.Write([UInt16]1)
        $bw.Write([UInt16]$blobs.Count)

        $offset = 6 + 16 * $blobs.Count
        foreach ($blob in $blobs) {
            $dim = $blob.Size
            if ($dim -ge 256) { $dim = 0 }
            $bw.Write([Byte]$dim)
            $bw.Write([Byte]$dim)
            $bw.Write([Byte]0)
            $bw.Write([Byte]0)
            $bw.Write([UInt16]1)
            $bw.Write([UInt16]32)
            $bw.Write([UInt32]$blob.Bytes.Length)
            $bw.Write([UInt32]$offset)
            $offset += $blob.Bytes.Length
        }

        foreach ($blob in $blobs) { $bw.Write($blob.Bytes) }
    }
    finally {
        $bw.Flush()
        $bw.Dispose()
        $fs.Dispose()
    }
}

$icoPath = Join-Path $assetsDir 'app.ico'
New-IcoFile -Sizes @(16, 20, 24, 32, 40, 48, 64, 96, 128, 256) -Path $icoPath
Write-Host ("EbenTiler icon generated: {0} ({1:N0} bytes)" -f $icoPath, (Get-Item $icoPath).Length)

if (-not $NoPreview) {
    $preview = Render-Icon 256
    try {
        $previewPath = Join-Path $previewDir 'appicon-preview.png'
        $preview.Save($previewPath, [System.Drawing.Imaging.ImageFormat]::Png)
        Write-Host "Preview: $previewPath"
    }
    finally {
        $preview.Dispose()
    }
}
