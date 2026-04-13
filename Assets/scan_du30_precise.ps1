Add-Type -AssemblyName System.Drawing
$src = [System.Drawing.Bitmap]::new('c:\Users\monde\Goon WarfareX\Assets\Du30.jpg')
$H = $src.Height; $W = $src.Width

# Sample column in the middle to find dark pixel content rows
$sampleX = $W / 2  # middle column
$darkRows = @()
for ($y = 0; $y -lt $H; $y++) {
    $c = $src.GetPixel($sampleX, $y)
    $brightness = ($c.R + $c.G + $c.B) / 3
    if ($brightness -lt 220) {  # not white = has content
        $darkRows += $y
    }
}

# Find gaps between content rows - these are the row separators
Write-Host "Total dark rows: $($darkRows.Count)"
$gaps = @()
for ($i = 1; $i -lt $darkRows.Count; $i++) {
    $gap = $darkRows[$i] - $darkRows[$i-1]
    if ($gap -gt 10) {
        $gaps += "Gap at y=$($darkRows[$i-1])-$($darkRows[$i]) (size=$gap)"
    }
}
Write-Host "Content gaps (likely row dividers):"
$gaps | ForEach-Object { Write-Host "  $_" }
Write-Host "First dark row: $($darkRows[0])"
Write-Host "Last dark row: $($darkRows[-1])"
$src.Dispose()
