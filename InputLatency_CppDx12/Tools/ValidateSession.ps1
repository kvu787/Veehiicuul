param(
    [Parameter(Mandatory)][string] $LogDirectory,
    [switch] $RequireGamepad,
    [switch] $RequireDisplayMeasurements,
    [switch] $AllowRejectedClockFrames
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Require {
    param([bool] $Condition, [string] $Message)
    if (-not $Condition) { throw $Message }
}

function Read-Values {
    param([string] $Name)
    $values = @{}
    foreach ($line in Get-Content -LiteralPath (Join-Path $LogDirectory $Name)) {
        if ($line -match '^([^=]+)=(.*)$') { $values[$Matches[1]] = $Matches[2] }
    }
    return $values
}

function Near {
    param([double] $Actual, [double] $Expected)
    # Summary.csv uses the C++ stream's six significant digits.
    return [Math]::Abs($Actual - $Expected) -le (0.001 + [Math]::Abs($Expected) * 0.000006)
}

$LogDirectory = (Resolve-Path -LiteralPath $LogDirectory).Path
$application = Read-Values 'Application.log'
$diagnostics = Read-Values 'MeasurementDiagnostics.txt'
$displayDiagnostics = Read-Values 'DisplayDiagnostics.txt'
Require ($application['MeasuredInputKind'] -eq 'Gamepad') 'The session did not declare gamepad-only measurement.'
Require ($application['LoggingFailed'] -eq '0') 'Application logging failed.'
foreach ($name in 'DroppedCallbackRecords', 'DroppedPresentationRecords', 'InvalidTimestamps', 'DeviceLimitOrMetadataErrors', 'UnexpectedPollingErrors') {
    Require ($diagnostics[$name] -eq '0') "Measurement diagnostic $name is nonzero or missing."
}
foreach ($name in 'StartOrProcessingError', 'EtwEventsLost', 'EtwBuffersLost', 'DroppedSubmissions', 'DroppedCompletions', 'DroppedMeasurements', 'DecoderWarnings', 'DecoderOverflows') {
    Require ($displayDiagnostics[$name] -eq '0') "Display diagnostic $name is nonzero or missing."
}
if (-not $AllowRejectedClockFrames) { Require ($displayDiagnostics['InvalidClocks'] -eq '0') 'Display diagnostic InvalidClocks is nonzero or missing.' }

$devices = @(Import-Csv -LiteralPath (Join-Path $LogDirectory 'Devices.csv'))
$readings = @(Import-Csv -LiteralPath (Join-Path $LogDirectory 'Readings.csv'))
$presentations = @(Import-Csv -LiteralPath (Join-Path $LogDirectory 'Presentations.csv'))
$frames = @(Import-Csv -LiteralPath (Join-Path $LogDirectory 'DisplayFrames.csv'))
$displayReadings = @(Import-Csv -LiteralPath (Join-Path $LogDirectory 'DisplayReadings.csv'))
$summary = @(Import-Csv -LiteralPath (Join-Path $LogDirectory 'Summary.csv'))
$identities = @{}
$samples = @{}
foreach ($device in $devices) {
    Require ($device.Kind -eq 'Gamepad') 'A non-gamepad device was logged.'
    Require ($device.DeviceId.Length -eq 64) 'A GameInput device identifier is incomplete.'
    if ($identities.ContainsKey($device.Device)) {
        Require ($identities[$device.Device] -eq $device.DeviceId) 'A device index changed identity.'
    }
    $identities[$device.Device] = $device.DeviceId
    foreach ($metric in 'ReadingToCallback', 'StateChangeInterval', 'ReadingToLateSample', 'ReadingToPresentBegin', 'PresentCallDuration', 'ReadingToDisplay') {
        $key = $device.Device + '/' + $metric
        if (-not $samples.ContainsKey($key)) { $samples[$key] = [Collections.Generic.List[double]]::new() }
    }
}
Require ([long]$application['DevicesEnumerated'] -eq $identities.Count) 'Application device count differs from connection records.'
if ($RequireGamepad) { Require ($identities.Count -gt 0) 'No gamepad was enumerated.' }

foreach ($reading in $readings) {
    Require ($identities.ContainsKey($reading.Device)) 'A callback references an unknown device.'
    Require ($reading.ValidTimestamp -eq '1') 'An invalid callback timestamp was logged.'
    Require ([long]$reading.CallbackTimestampUs - [long]$reading.ReadingTimestampUs -eq [long]$reading.CallbackDelayUs) 'Callback duration subtraction is incorrect.'
    if ($reading.Foreground -eq '1') {
        $samples[$reading.Device + '/ReadingToCallback'].Add([double]$reading.CallbackDelayUs)
        if ($reading.ChangeIntervalUs -ne '') { $samples[$reading.Device + '/StateChangeInterval'].Add([double]$reading.ChangeIntervalUs) }
    }
}
foreach ($presentation in $presentations) {
    Require ($identities.ContainsKey($presentation.Device)) 'A presentation references an unknown device.'
    Require ($presentation.ValidTimestamp -eq '1') 'An invalid presentation timeline was logged.'
    Require ([long]$presentation.ReadingTimestampUs -le [long]$presentation.SampleTimestampUs -and
        [long]$presentation.SampleTimestampUs -le [long]$presentation.PresentBeginTimestampUs -and
        [long]$presentation.PresentBeginTimestampUs -le [long]$presentation.PresentEndTimestampUs) 'Reading/sample/Present timestamps are out of order.'
    Require ([long]$presentation.SampleTimestampUs - [long]$presentation.ReadingTimestampUs -eq [long]$presentation.SampleDelayUs) 'Sample duration subtraction is incorrect.'
    Require ([long]$presentation.PresentBeginTimestampUs - [long]$presentation.ReadingTimestampUs -eq [long]$presentation.PresentBeginDelayUs) 'Present-begin duration subtraction is incorrect.'
    Require ([long]$presentation.PresentEndTimestampUs - [long]$presentation.PresentBeginTimestampUs -eq [long]$presentation.PresentCallDurationUs) 'Present-call duration subtraction is incorrect.'
    if ($presentation.PresentAccepted -eq '1') {
        $samples[$presentation.Device + '/ReadingToLateSample'].Add([double]$presentation.SampleDelayUs)
        if ($presentation.Visualized -eq '1') {
            $samples[$presentation.Device + '/ReadingToPresentBegin'].Add([double]$presentation.PresentBeginDelayUs)
            $samples[$presentation.Device + '/PresentCallDuration'].Add([double]$presentation.PresentCallDurationUs)
        }
    }
}

$frameLookup = @{}
$frameStatuses = @{}
foreach ($frame in $frames) {
    Require (-not $frameLookup.ContainsKey($frame.Frame)) 'A submitted frame has multiple results.'
    $frameLookup[$frame.Frame] = $frame
    if (-not $frameStatuses.ContainsKey($frame.Status)) { $frameStatuses[$frame.Status] = 0 }
    $frameStatuses[$frame.Status]++
}
Require ($frames.Count -eq [long]$displayDiagnostics['SubmittedFrames']) 'Frame results do not account for every submitted frame.'
$rejectedClocks = @($frames | Where-Object Status -EQ 'InvalidClockOrTraceLoss')
Require ([long]$displayDiagnostics['InvalidClocks'] -eq $rejectedClocks.Count) 'Clock diagnostics are not fully explained by rejected frames.'
foreach ($frame in $rejectedClocks) {
    Require ($frame.DisplayTimestampUs -eq '' -and $frame.ClockUncertaintyUs -eq '') 'A rejected clock frame contains an accepted clock conversion.'
}
$lastSerial = @{}
$countedReadings = [Collections.Generic.HashSet[string]]::new()
foreach ($reading in $displayReadings) {
    Require ($identities.ContainsKey($reading.Device)) 'A display reading references an unknown device.'
    Require ($frameLookup.ContainsKey($reading.Frame)) 'A display reading references an unknown frame.'
    $frame = $frameLookup[$reading.Frame]
    Require ($frame.Status -eq 'Displayed') 'A discarded/unresolved frame contributed a display reading.'
    Require ($reading.Valid -eq '1') 'An invalid display reading was logged.'
    Require ($reading.DisplayQpc -eq $frame.DisplayQpc -and $reading.DisplayTimestampUs -eq $frame.DisplayTimestampUs) 'A display reading differs from its matched frame event.'
    Require ([double]$reading.ClockUncertaintyUs -ge 1 -and [double]$reading.ClockUncertaintyUs -le 100) 'Display clock uncertainty is outside the accepted bounds.'
    Require ([long]$reading.ReadingTimestampUs -le [long]$reading.SampleTimestampUs -and
        [long]$reading.SampleTimestampUs -le [long]$reading.DisplayTimestampUs) 'Reading/sample/display timestamps are out of order.'
    Require ([long]$reading.DisplayTimestampUs - [long]$reading.ReadingTimestampUs -eq [long]$reading.ReadingToDisplayUs) 'Reading-to-display duration subtraction is incorrect.'
    $serial = [long]$reading.ReadingSerial
    $isFirst = -not $lastSerial.ContainsKey($reading.Device) -or $serial -gt $lastSerial[$reading.Device]
    Require (($reading.FirstDisplayForReading -eq '1') -eq $isFirst) 'First-display deduplication differs from serial ordering.'
    if ($isFirst) {
        Require ($countedReadings.Add($reading.Device + '/' + $reading.ReadingSerial)) 'A reading was counted more than once.'
        $lastSerial[$reading.Device] = $serial
        $samples[$reading.Device + '/ReadingToDisplay'].Add([double]$reading.ReadingToDisplayUs)
    }
}
if ($RequireDisplayMeasurements) { Require ($countedReadings.Count -gt 0) 'No fresh gamepad readings reached a displayed frame.' }

$metricKeys = [Collections.Generic.HashSet[string]]::new()
foreach ($row in $summary) {
    Require ($row.Kind -eq 'Gamepad') 'Non-gamepad summary statistics were logged.'
    $key = $row.Device + '/' + $row.Metric
    Require ($samples.ContainsKey($key) -and $metricKeys.Add($key)) 'A summary metric is unknown or duplicated.'
    $values = $samples[$key]
    Require ([long]$row.Count -eq $values.Count) "Summary count differs from raw data for $key."
    Require ([long]$row.PercentileWindowCount -eq [Math]::Min(8192, $values.Count)) "Percentile window differs for $key."
    if ($values.Count -eq 0) { continue }
    $statistics = $values | Measure-Object -Minimum -Maximum -Average
    foreach ($comparison in @(@([double]$row.MinimumUs, $statistics.Minimum), @([double]$row.MaximumUs, $statistics.Maximum),
        @([double]$row.MeanUs, $statistics.Average), @([double]$row.LastUs, $values[$values.Count - 1]))) {
        Require (Near $comparison[0] $comparison[1]) "Summary aggregate differs from raw data for $key."
    }
    $sorted = @($values | Select-Object -Last 8192 | Sort-Object)
    foreach ($percentile in @(@('P50Us', 0.5), @('P95Us', 0.95), @('P99Us', 0.99))) {
        $expected = $sorted[[int][Math]::Ceiling($sorted.Count * $percentile[1]) - 1]
        Require (Near ([double]$row.($percentile[0])) $expected) "Summary percentile differs from raw data for $key."
    }
}
Require ($metricKeys.Count -eq $identities.Count * 6) 'A gamepad summary metric is missing.'

$result = [ordered]@{
    Passed = $true
    DeviceCount = $identities.Count
    ConnectionRecords = $devices.Count
    CallbackReadings = $readings.Count
    PresentationReadings = $presentations.Count
    DisplayReadingRows = $displayReadings.Count
    FirstDisplayedReadings = $countedReadings.Count
    RejectedClockFrames = $rejectedClocks.Count
    FrameStatuses = $frameStatuses
    MaximumClockUncertaintyUs = [double]$displayDiagnostics['MaximumClockUncertaintyUs']
    SummaryMetricsChecked = $metricKeys.Count
}
$result | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $LogDirectory 'Validation.json') -Encoding utf8
[pscustomobject]$result
