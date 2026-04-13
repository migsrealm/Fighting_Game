Add-Type -AssemblyName System.Drawing
$src = [System.Drawing.Image]::FromFile('c:\Users\monde\Goon WarfareX\Assets\Du30.jpg')
$W = $src.Width; $H = $src.Height

# Actual content area starts at y=108 (top padding), ends at ~y=944
$topOffset = 108
$contentH = 944 - $topOffset  # ~836px of actual content
$cols = 4; $rows = 4
$tw = [int]($W / $cols)
$th = [int]($contentH / $rows)
Write-Host "Image: ${W}x${H} | Offset: $topOffset | ContentH: $contentH | TileW: $tw | TileH: $th"

$outDir = 'c:\Users\monde\Goon WarfareX\Assets\_du30_tiles_fixed'
if (!(Test-Path $outDir)) { New-Item -ItemType Directory $outDir | Out-Null }

$n=1
for($r=0; $r -lt $rows; $r++) {
  for($c=0; $c -lt $cols; $c++) {
    $tile = New-Object System.Drawing.Bitmap($tw, $th)
    $g = [System.Drawing.Graphics]::FromImage($tile)
    $sx = $c * $tw; $sy = $topOffset + $r * $th
    $src_rect = New-Object System.Drawing.Rectangle($sx, $sy, $tw, $th)
    $dst_rect = New-Object System.Drawing.Rectangle(0, 0, $tw, $th)
    $g.DrawImage($src, $dst_rect, $src_rect, [System.Drawing.GraphicsUnit]::Pixel)
    $g.Dispose()
    $tile.Save("$outDir\tile_${n}_c${c}r${r}.png", [System.Drawing.Imaging.ImageFormat]::Png)
    $tile.Dispose()
    $n++
  }
}
$src.Dispose()
Write-Host 'Done'
