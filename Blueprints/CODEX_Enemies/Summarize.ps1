# Run after FlightProbe completes. Thresholds describe intact, powered flight, not battle survivability.
$rows = Import-Csv (Join-Path $PSScriptRoot 'Flight.csv')
$baseline = Import-Csv (Join-Path $PSScriptRoot 'Flight-baseline-expanded.csv')
$results = foreach ($group in ($rows | Group-Object name)) {
    $steady = @($group.Group | Where-Object { [double]$_.seconds -ge 25 -and [double]$_.seconds -lt 75 })
    if ($steady.Count -eq 0) { throw "No settled-flight samples: $($group.Name)" }
    $tilt = ($steady | ForEach-Object { [double]$_.tiltDegrees } | Measure-Object -Maximum).Maximum
    $height = ($steady | ForEach-Object { [math]::Abs([double]$_.heightError) } | Measure-Object -Maximum).Maximum
    $vertical = ($steady | ForEach-Object { [math]::Abs([double]$_.verticalSpeed) } | Measure-Object -Maximum).Maximum
    $unpowered = ($group.Group | ForEach-Object { [int]$_.unpowered } | Measure-Object -Maximum).Maximum
    $lostTargets = @($steady | Where-Object { [double]$_.targetDistance -lt 0 }).Count
    $minBlocks = ($group.Group | ForEach-Object { [int]$_.blocks } | Measure-Object -Minimum).Minimum
    $initialBlocks = [int]$group.Group[0].blocks
    $duration = [double]$group.Group[-1].seconds
    $idle = @($group.Group | Where-Object { ([double]$_.seconds -ge 10 -and [double]$_.seconds -lt 15) -or [double]$_.seconds -ge 85 })
    $idleVertical = ($idle | ForEach-Object { [math]::Abs([double]$_.verticalSpeed) } | Measure-Object -Maximum).Maximum
    $idleHasTarget = @($idle | Where-Object { [double]$_.targetDistance -ge 0 }).Count
    $peakSpeed = ($steady | ForEach-Object { [double]$_.horizontalSpeed } | Measure-Object -Maximum).Maximum
    $baselinePeak = ($baseline | Where-Object { $_.name -eq $group.Name -and [double]$_.seconds -ge 25 -and [double]$_.seconds -lt 75 } | ForEach-Object { [double]$_.horizontalSpeed } | Measure-Object -Maximum).Maximum
    $speedRatio = $peakSpeed / $baselinePeak
    [pscustomobject]@{
        name = $group.Name
        passed = ($duration -ge 89.9 -and $idle.Count -ge 40 -and $idleHasTarget -eq 0 -and $idleVertical -le 0.1 -and $tilt -le 5 -and $height -le 1 -and $vertical -le 1 -and $unpowered -eq 0 -and $lostTargets -eq 0 -and $minBlocks -eq $initialBlocks -and $speedRatio -ge 0.3)
        peakHorizontalSpeed = $peakSpeed
        baselinePeakHorizontalSpeed = $baselinePeak
        peakSpeedRatio = $speedRatio
        seconds = $duration
        settledFromSeconds = 25
        maxIdleVerticalSpeed = $idleVertical
        maxTiltDegrees = $tilt
        maxAbsHeightErrorMeters = $height
        maxAbsVerticalSpeed = $vertical
        maxUnpowered = $unpowered
        lostTargetSamples = $lostTargets
        minBlocks = $minBlocks
    }
}
$results | ConvertTo-Json -Depth 4 | Set-Content -Encoding utf8 (Join-Path $PSScriptRoot 'Flight-summary.json')
$results | Format-Table
if (@($results).Count -ne 3 -or @($results | Where-Object { -not $_.passed }).Count -gt 0) { throw 'Flight acceptance failed; inspect Flight.csv' }
