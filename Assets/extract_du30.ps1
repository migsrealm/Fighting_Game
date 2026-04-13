Add-Type -AssemblyName System.Drawing
$src = [System.Drawing.Image]::FromFile('c:\Users\monde\Goon WarfareX\Assets\Du30.jpg')
$W = $src.Width; $H = $src.Height
Write-Host "Image: ${W}x${H}"
foreach ($rows in @(4, 5)) {
    $cols = 4; $tw = [int]($W/$cols); $th = [int]($H/$rows)
    Write-Host "  ${cols}c x ${rows}r: tile=${tw}x${th}"
}
# Extract 4x4
$cols=4; $rows=4; $tw = [int]($W/$cols); $th = [int]($H/$rows)
$outDir = 'c:\Users\monde\Goon WarfareX\Assets\_du30_tiles'
if (!(Test-Path $outDir)) { New-Item -ItemType Directory $outDir | Out-Null }
$n=1
for($r=0; $r -lt $rows; $r++) {
  for($c=0; $c -lt $cols; $c++) {
    $tile = New-Object System.Drawing.Bitmap($tw, $th)
    $g = [System.Drawing.Graphics]::FromImage($tile)
    $src_rect = New-Object System.Drawing.Rectangle(($c*$tw), ($r*$th), $tw, $th)
    $g.DrawImage($src, (New-Object System.Drawing.Rectangle(0,0,$tw,$th)), $src_rect, [System.Drawing.GraphicsUnit]::Pixel)
    $g.Dispose()
    $tile.Save("$outDir\tile_${n}_c${c}r${r}.png", [System.Drawing.Imaging.ImageFormat]::Png)
    $tile.Dispose()
    $n++
  }
}
$src.Dispose()
Write-Host 'Done 4x4'
