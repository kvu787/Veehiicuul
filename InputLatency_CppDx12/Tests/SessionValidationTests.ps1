Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$applicationDirectory = Split-Path $PSScriptRoot
$validator = Join-Path $applicationDirectory 'Tools\ValidateSession.ps1'
$fixtureDirectory = Join-Path $applicationDirectory ('LogOutput\' + (Get-Date -Format 'yyyy-MM-dd_HH-mm-ss') + '\SessionValidation')
New-Item -ItemType Directory -Path $fixtureDirectory -Force | Out-Null
$identifier = '0' * 64
$files = @{
    'Application.log' = "MeasuredInputKind=Gamepad`nLoggingFailed=0`nDevicesEnumerated=1`nFramesSubmitted=1`n"
    'MeasurementDiagnostics.txt' = "DroppedCallbackRecords=0`nDroppedPresentationRecords=0`nInvalidTimestamps=0`nDeviceLimitOrMetadataErrors=0`nUnexpectedPollingErrors=0`n"
    'DisplayDiagnostics.txt' = "StartOrProcessingError=0`nEtwEventsLost=0`nEtwBuffersLost=0`nDroppedSubmissions=0`nDroppedCompletions=0`nDroppedMeasurements=0`nInvalidClocks=0`nDecoderWarnings=0`nDecoderOverflows=0`nSubmittedFrames=1`nDisplayedFrames=1`nDiscardedFrames=0`nUnresolvedFrames=0`nMaximumClockUncertaintyUs=1`n"
    'Devices.csv' = "Device,TimestampUs,Connected,Kind,VendorId,ProductId,DeviceId,Name`n0,90,1,Gamepad,1,2,$identifier,Fixture`n"
    'Readings.csv' = "Device,ReadingTimestampUs,CallbackTimestampUs,CallbackDelayUs,ChangeIntervalUs,Foreground,ValidTimestamp`n0,100,130,30,,1,1`n"
    'Presentations.csv' = "Device,Frame,ReadingTimestampUs,SampleTimestampUs,PresentBeginTimestampUs,PresentEndTimestampUs,SampleDelayUs,PresentBeginDelayUs,PresentCallDurationUs,PresentAccepted,Visualized,ValidTimestamp`n0,0,100,110,120,140,10,20,20,1,1,1`n"
    'DisplayFrames.csv' = "Frame,PresentStartQpc,DisplayQpc,DisplayTimestampUs,ClockUncertaintyUs,PresentMode,Status`n0,1200,1500,150,1,0,Displayed`n"
    'DisplayReadings.csv' = "Device,Frame,ReadingSerial,ReadingTimestampUs,SampleTimestampUs,DisplayQpc,DisplayTimestampUs,ReadingToDisplayUs,ClockUncertaintyUs,FirstDisplayForReading,Valid`n0,0,1,100,110,1500,150,50,1,1,1`n"
    'Summary.csv' = @'
Device,Kind,Name,Metric,Count,PercentileWindowCount,LastUs,MinimumUs,MeanUs,MaximumUs,P50Us,P95Us,P99Us
0,Gamepad,Fixture,ReadingToCallback,1,1,30,30,30,30,30,30,30
0,Gamepad,Fixture,StateChangeInterval,0,0,0,0,0,0,0,0,0
0,Gamepad,Fixture,ReadingToLateSample,1,1,10,10,10,10,10,10,10
0,Gamepad,Fixture,ReadingToPresentBegin,1,1,20,20,20,20,20,20,20
0,Gamepad,Fixture,PresentCallDuration,1,1,20,20,20,20,20,20,20
0,Gamepad,Fixture,ReadingToDisplay,1,1,50,50,50,50,50,50,50
'@
}
foreach ($entry in $files.GetEnumerator()) {
    Set-Content -LiteralPath (Join-Path $fixtureDirectory $entry.Key) -Value $entry.Value -Encoding utf8
}
$valid = & $validator -LogDirectory $fixtureDirectory -RequireGamepad -RequireDisplayMeasurements
if (-not $valid.Passed -or $valid.SummaryMetricsChecked -ne 6) { throw 'The known-valid fixture was rejected.' }

$cases = @(
    @{ File = 'Readings.csv'; Old = '100,130,30'; New = '100,130,31'; Error = 'Callback duration' },
    @{ File = 'Presentations.csv'; Old = '140,10,20,20'; New = '140,11,20,20'; Error = 'Sample duration' },
    @{ File = 'DisplayReadings.csv'; Old = '1500,150,50'; New = '1501,150,50'; Error = 'matched frame event' },
    @{ File = 'DisplayReadings.csv'; Old = '1500,150,50'; New = '1500,150,51'; Error = 'Reading-to-display duration' },
    @{ File = 'DisplayReadings.csv'; Old = '50,1,1,1'; New = '50,1,0,1'; Error = 'deduplication' },
    @{ File = 'DisplayFrames.csv'; Old = 'Displayed'; New = 'Discarded'; Error = 'discarded/unresolved frame' },
    @{ File = 'Summary.csv'; Old = '30,30,30,30,30,30,30'; New = '30,30,31,30,30,30,30'; Error = 'Summary aggregate' },
    @{ File = 'Summary.csv'; Old = '30,30,30,30,30,30,30'; New = '30,30,30,30,30,31,30'; Error = 'Summary percentile' },
    @{ File = 'Devices.csv'; Old = 'Gamepad'; New = 'Keyboard'; Error = 'non-gamepad device' },
    @{ File = 'MeasurementDiagnostics.txt'; Old = 'DroppedCallbackRecords=0'; New = 'DroppedCallbackRecords=1'; Error = 'DroppedCallbackRecords' }
)
foreach ($case in $cases) {
    $fixturePath = Join-Path $fixtureDirectory $case.File
    Set-Content -LiteralPath $fixturePath -Value $files[$case.File].Replace($case.Old, $case.New) -Encoding utf8
    $caught = $false
    try { & $validator -LogDirectory $fixtureDirectory -RequireGamepad -RequireDisplayMeasurements | Out-Null }
    catch {
        if ($_.Exception.Message -notlike ('*' + $case.Error + '*')) { throw }
        $caught = $true
    }
    finally { Set-Content -LiteralPath $fixturePath -Value $files[$case.File] -Encoding utf8 }
    if (-not $caught) { throw "Validator accepted corrupt data in $($case.File)." }
}
Write-Host "Session validation accepted a known-good fixture and rejected all $($cases.Count) corruption cases."

# Permit an explicitly recorded clock rejection only when it has no accepted
# timestamp or display-reading row and all remaining observations validate.
Set-Content -LiteralPath (Join-Path $fixtureDirectory 'DisplayDiagnostics.txt') -Value $files['DisplayDiagnostics.txt'].Replace('InvalidClocks=0', 'InvalidClocks=1').Replace('SubmittedFrames=1', 'SubmittedFrames=2').Replace('DisplayedFrames=1', 'DisplayedFrames=2') -Encoding utf8
Set-Content -LiteralPath (Join-Path $fixtureDirectory 'DisplayFrames.csv') -Value ($files['DisplayFrames.csv'] + "1,1600,1800,,,,InvalidClockOrTraceLoss`n") -Encoding utf8
$rejected = & $validator -LogDirectory $fixtureDirectory -RequireGamepad -RequireDisplayMeasurements -AllowRejectedClockFrames
if (-not $rejected.Passed -or $rejected.RejectedClockFrames -ne 1 -or $rejected.FirstDisplayedReadings -ne 1) { throw 'Safe clock rejection was not preserved.' }
Write-Host 'An explicitly rejected clock frame was excluded while the remaining measurements passed.'
