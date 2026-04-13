Add-Type -AssemblyName System.Drawing
$src = [System.Drawing.Bitmap]::new('c:\Users\monde\Goon WarfareX\Assets\Du30.jpg')
$H = $src.Height; $W = $src.Width
Write-Host "Size: ${W}x${H}"

# Scan each row for the average brightness - blank divider rows will be very bright (white bg)
$rowBrightness = @()
for ($y = 0; $y -lt $H; $y++) {
    $total = 0
    $step = [int]($W / 20)  # sample 20 pixels per row
    for ($x = 0; $x -lt $W; $x += $step) {
        $c = $src.GetPixel($x, $y)
        $total += ($c.R + $c.G + $c.B) / 3
    }
    $rowBrightness += $total / ($W / $step)
}

# Find rows that are near-white (brightness > 240) - these are dividers
Write-Host "Looking for bright divider rows..."
$dividers = @()
for ($y = 0; $y -lt $H; $y++) {
    if ($rowBrightness[$y] -gt 245) {
        $dividers += $y
    }
}

# Group consecutive bright rows into bands
$bands = @()
$start = -1
for ($i = 0; $i -lt $dividers.Count; $i++) {
    if ($start -eq -1) { $start = $dividers[$i] }
    if ($i -eq $dividers.Count-1 -or $dividers[$i+1] -ne $dividers[$i]+1) {
        $bands += "$start-$($dividers[$i])"
        $start = -1
    }
}
Write-Host "Bright divider bands: $($bands -join ', ')"
$src.Dispose()
