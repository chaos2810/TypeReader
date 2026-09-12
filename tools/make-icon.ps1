Add-Type -AssemblyName System.Drawing
$sizes = @(16, 32, 48, 256)
$pngs = @()

foreach ($s in $sizes) {
    $scale = $s / 256.0
    $bmp = New-Object System.Drawing.Bitmap($s, $s)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias

    # rounded keycap
    $r = 52 * $scale
    $rect = New-Object System.Drawing.RectangleF((16*$scale), (16*$scale), (224*$scale), (224*$scale))
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = 2 * $r
    $path.AddArc($rect.X, $rect.Y, $d, $d, 180, 90)
    $path.AddArc($rect.Right - $d, $rect.Y, $d, $d, 270, 90)
    $path.AddArc($rect.Right - $d, $rect.Bottom - $d, $d, $d, 0, 90)
    $path.AddArc($rect.X, $rect.Bottom - $d, $d, $d, 90, 90)
    $path.CloseFigure()

    $dark = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 32, 32, 32))
    $g.FillPath($dark, $path)
    $blue = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 76, 194, 255), [single](10*$scale))
    $blue.Alignment = [System.Drawing.Drawing2D.PenAlignment]::Inset
    $g.DrawPath($blue, $path)

    # letter A
    $white = New-Object System.Drawing.Pen([System.Drawing.Color]::White, [single](18*$scale))
    $white.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $white.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $white.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    $g.DrawLines($white, @(
        [System.Drawing.PointF]::new(128*$scale,  76*$scale),
        [System.Drawing.PointF]::new( 88*$scale, 168*$scale)))
    $g.DrawLines($white, @(
        [System.Drawing.PointF]::new(128*$scale,  76*$scale),
        [System.Drawing.PointF]::new(168*$scale, 168*$scale)))
    $g.DrawLines($white, @(
        [System.Drawing.PointF]::new(105*$scale, 138*$scale),
        [System.Drawing.PointF]::new(151*$scale, 138*$scale)))

    $g.Dispose()

    $ms2 = New-Object System.IO.MemoryStream
    $bmp.Save($ms2, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngs += ,($ms2.ToArray())
    $bmp.Dispose()
}

$outPath = Join-Path $PSScriptRoot "..\..\src\TypeReader.App\trayicon.ico"
$ms = New-Object System.IO.MemoryStream
$w = New-Object System.IO.BinaryWriter($ms)
$w.Write([uint16]0)
$w.Write([uint16]1)
$w.Write([uint16]$sizes.Count)
$dataOffset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $bw = if ($sizes[$i] -ge 256) { 0 } else { $sizes[$i] }
    $w.Write([byte]$bw)
    $w.Write([byte]$bw)
    $w.Write([byte]0)
    $w.Write([byte]0)
    $w.Write([uint16]1)
    $w.Write([uint16]32)
    $w.Write([uint32]$pngs[$i].Length)
    $w.Write([uint32]$dataOffset)
    $dataOffset += $pngs[$i].Length
}
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $w.Write($pngs[$i])
}
$w.Flush()
[System.IO.File]::WriteAllBytes((Resolve-Path $PSScriptRoot\..).Path + "\src\TypeReader.App\trayicon.ico", $ms.ToArray())
$w.Dispose()

$bytes = [System.IO.File]::ReadAllBytes((Resolve-Path $PSScriptRoot\..).Path + "\src\TypeReader.App\trayicon.ico")
"count=$([System.BitConverter]::ToUInt16($bytes, 4))"
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $off = 6 + $i * 16
    "img $i w=$($bytes[$off]) size=$([System.BitConverter]::ToUInt32($bytes, $off+8)) offset=$([System.BitConverter]::ToUInt32($bytes, $off+12))"
}