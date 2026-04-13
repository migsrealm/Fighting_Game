Add-Type -AssemblyName System.Drawing
$src = [System.Drawing.Image]::FromFile('c:\Users\monde\Goon WarfareX\Assets\larry_gadon.jpg')
$W = $src.Width; $H = $src.Height
Write-Host "Image: ${W}x${H}"

# Try 4col x 5row (current assumption) and 4col x 4row
foreach ($rows in @(4, 5)) {
    $cols = 4
    $tw = [int]($W/$cols); $th = [int]($H/$rows)
    Write-Host "  ${cols}c x ${rows}r: tile=${tw}x${th}"
}

# Extract with 4x5 first
$cols=4; $rows=5
$tw = [int]($W/$cols); $th = [int]($H/$rows)
$outDir = 'c:\Users\monde\Goon WarfareX\Assets\_larry_tiles'
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
