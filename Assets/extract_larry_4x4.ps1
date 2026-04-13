Add-Type -AssemblyName System.Drawing
$src = [System.Drawing.Image]::FromFile('c:\Users\monde\Goon WarfareX\Assets\larry_gadon.jpg')
$W = $src.Width; $H = $src.Height
$cols=4; $rows=4
$tw = [int]($W/$cols); $th = [int]($H/$rows)
Write-Host "Image: ${W}x${H} | Tile: ${tw}x${th}"
$outDir = 'c:\Users\monde\Goon WarfareX\Assets\_larry_tiles_4x4'
if (!(Test-Path $outDir)) { New-Item -ItemType Directory $outDir | Out-Null }
$n=1
for($r=0; $r -lt $rows; $r++) {
  for($c=0; $c -lt $cols; $c++) {
    $tile = New-Object System.Drawing.Bitmap($tw, $th)
    $g = [System.Drawing.Graphics]::FromImage($tile)
    $src_rect = New-Object System.Drawing.Rectangle(($c*$tw), ($r*$th), $tw, $th)
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
