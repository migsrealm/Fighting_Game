Add-Type -AssemblyName System.Drawing
$src = [System.Drawing.Image]::FromFile('c:\Users\monde\Goon WarfareX\Assets\Du30.jpg')
$W = $src.Width; $H = $src.Height

# Top white band is 108px, content goes to ~y=944
# Each tile: fH = (1024 - 108) / 4 = 229px
$topOff = 108
$cols = 4; $rows = 4
$tw = [int]($W / $cols)
$th = [int](($H - $topOff) / $rows)
Write-Host "TileW=$tw TileH=$th"

$outDir = 'c:\Users\monde\Goon WarfareX\Assets\_du30_correct'
if (!(Test-Path $outDir)) { New-Item -ItemType Directory $outDir | Out-Null }
$n = 1
for($r = 0; $r -lt $rows; $r++) {
    for($c = 0; $c -lt $cols; $c++) {
        $tile = New-Object System.Drawing.Bitmap($tw, $th)
        $g = [System.Drawing.Graphics]::FromImage($tile)
        $sx = $c * $tw; $sy = $topOff + $r * $th
        $g.DrawImage($src, (New-Object System.Drawing.Rectangle(0,0,$tw,$th)),
                          (New-Object System.Drawing.Rectangle($sx,$sy,$tw,$th)),
                          [System.Drawing.GraphicsUnit]::Pixel)
        $g.Dispose()
        $tile.Save("$outDir\tile_${n}_c${c}r${r}.png", [System.Drawing.Imaging.ImageFormat]::Png)
        $tile.Dispose()
        $n++
    }
}
$src.Dispose()
Write-Host 'Done'
