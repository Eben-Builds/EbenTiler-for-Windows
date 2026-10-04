param(
    [switch]$NoPreview
)

# Tessdeck의 겹쳐진 두 창 심볼을 Windows 멀티 사이즈 .ico로 만든다.
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
        $g.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
        $g.Clear([System.Drawing.Color]::Transparent)

        $s = $Size / 64.0
        $small = $Size -le 40

        # Rounded navy app tile.
        $tile = New-Object System.Drawing.RectangleF ([single](2*$s)), ([single](2*$s)), ([single](60*$s)), ([single](60*$s))
        $tilePath = New-RoundedPath $tile.X $tile.Y $tile.Width $tile.Height ([single](14*$s))
        $tileGradient = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
            $tile,
            ([System.Drawing.Color]::FromArgb(255, 16, 72, 156)),
            ([System.Drawing.Color]::FromArgb(255, 2, 17, 48)),
            [System.Drawing.Drawing2D.LinearGradientMode]::ForwardDiagonal)
        $g.FillPath($tileGradient, $tilePath)
        $tileGradient.Dispose()

        $tilePen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 30, 157, 255)), ([single][Math]::Max(1.0, 1.5*$s))
        $g.DrawPath($tilePen, $tilePath)
        $tilePen.Dispose()
        $tilePath.Dispose()

        # Back window: pale glass panel.
        $back = New-Object System.Drawing.RectangleF ([single](13*$s)), ([single](15*$s)), ([single](32*$s)), ([single](30*$s))
        $backPath = New-RoundedPath $back.X $back.Y $back.Width $back.Height ([single](7*$s))
        if ($small) {
            $backBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 234, 242, 255))
        } else {
            $backBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
                $back,
                ([System.Drawing.Color]::FromArgb(255, 255, 255, 255)),
                ([System.Drawing.Color]::FromArgb(255, 158, 196, 255)),
                [System.Drawing.Drawing2D.LinearGradientMode]::ForwardDiagonal)
        }
        $g.FillPath($backBrush, $backPath)
        $backBrush.Dispose()
        $backPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(235, 255, 255, 255)), ([single][Math]::Max(1.0, 1.2*$s))
        $g.DrawPath($backPen, $backPath)
        $backPen.Dispose()
        $backPath.Dispose()

        # Front window: vivid cyan/blue panel overlapping the back panel.
        $front = New-Object System.Drawing.RectangleF ([single](25*$s)), ([single](27*$s)), ([single](31*$s)), ([single](28*$s))
        $frontPath = New-RoundedPath $front.X $front.Y $front.Width $front.Height ([single](7*$s))
        if ($small) {
            $frontBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 17, 126, 255))
        } else {
            $frontBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
                $front,
                ([System.Drawing.Color]::FromArgb(245, 61, 224, 255)),
                ([System.Drawing.Color]::FromArgb(255, 0, 82, 238)),
                [System.Drawing.Drawing2D.LinearGradientMode]::ForwardDiagonal)
        }
        $g.FillPath($frontBrush, $frontPath)
        $frontBrush.Dispose()

        $frontPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 55, 235, 255)), ([single][Math]::Max(1.0, 1.4*$s))
        $g.DrawPath($frontPen, $frontPath)
        $frontPen.Dispose()

        if (-not $small) {
            # Soft top highlight keeps the glass feel without hurting small-size clarity.
            $highlightPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(180, 220, 255, 255)), ([single][Math]::Max(1.0, 1.1*$s))
            $g.DrawLine($highlightPen, ([single](31*$s)), ([single](29*$s)), ([single](49*$s)), ([single](29*$s))
            $highlightPen.Dispose()
        }
        $frontPath.Dispose()
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
Write-Host ("Tessdeck icon generated: {0} ({1:N0} bytes)" -f $icoPath, (Get-Item $icoPath).Length)

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
