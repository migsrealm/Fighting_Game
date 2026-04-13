Add-Type -AssemblyName System.Drawing
$src = [System.Drawing.Bitmap]::new('c:\Users\monde\Goon WarfareX\Assets\Du30.jpg')
$H = $src.Height; $W = $src.Width

# For each row, scan all columns, track if ANY pixel is dark
$rowHasContent = @()
for ($y = 0; $y -lt $H; $y++) {
    $hasDark = $false
    for ($x = 0; $x -lt $W; $x += 4) {  # sample every 4px
        $c = $src.GetPixel($x, $y)
        if (($c.R + $c.G + $c.B) / 3 -lt 230) { $hasDark = $true; break }
    }
    $rowHasContent += $hasDark
}

# Find transitions between content and no-content
$transitions = @()
for ($y = 1; $y -lt $H; $y++) {
    if ($rowHasContent[$y] -ne $rowHasContent[$y-1]) {
        $type = $rowHasContent[$y] ? "START" : "END"
        $transitions += "${type} at y=$y"
    }
}
$transitions | ForEach-Object { Write-Host $_ }
$src.Dispose()
