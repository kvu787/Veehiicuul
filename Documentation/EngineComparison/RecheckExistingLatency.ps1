Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Read existing recordings only. This script launches no game and makes no new capture.
function Get-Percentile {
    param([double[]]$Values, [double]$Fraction)
    $index = ($Values.Length - 1) * $Fraction
    $lower = [int][Math]::Floor($index)
    $upper = [int][Math]::Ceiling($index)
    return $Values[$lower] + ($Values[$upper] - $Values[$lower]) * ($index - $lower)
}

$captures = @(
    @{ Id = 'ZO'; Path = 'C:\Users\k\Repository\ZoomTracks\ZoomTracks\MyLogOutput\2026-09-17_01-33-02\PresentMon.csv'; Trim = 0 },
    @{ Id = 'ZU'; Path = 'C:\Users\k\Repository\ZoomTracks\ZoomTracks\MyLogOutput\2026-09-17_01-37-02\PresentMon.csv'; Trim = 0 },
    @{ Id = 'GO'; Path = 'C:\Users\k\Repository\VsyncStutterTest\MyLogOutput\2026-09-17_02-33-46\PresentMon.csv'; Trim = 5 },
    @{ Id = 'GU'; Path = 'C:\Users\k\Repository\VsyncStutterTest\MyLogOutput\2026-09-17_02-38-05\PresentMon.csv'; Trim = 5 }
)
$results = foreach ($capture in $captures) {
    $rows = @(Import-Csv -LiteralPath $capture.Path)
    $start = [double]$rows[0].TimeInMs
    $duration = ([double]$rows[-1].TimeInMs - $start) / 1000
    $window = @($rows | Where-Object {
        $elapsed = ([double]$_.TimeInMs - $start) / 1000
        $capture.Trim -eq 0 -or ($elapsed -ge $capture.Trim -and $elapsed -lt ($duration - $capture.Trim))
    })
    [double[]]$values = @($window | Where-Object { $_.MsAllInputToPhotonLatency -ne 'NA' } | ForEach-Object { [double]$_.MsAllInputToPhotonLatency } | Sort-Object)
    [PSCustomObject]@{
        Id = $capture.Id
        Path = $capture.Path
        Sha256 = (Get-FileHash -LiteralPath $capture.Path -Algorithm SHA256).Hash
        TotalRows = $rows.Length
        DurationSeconds = $duration
        TrimStartAndEndSeconds = $capture.Trim
        WindowRows = $window.Length
        InputSamples = $values.Length
        CoveragePercent = 100 * $values.Length / $window.Length
        MeanMilliseconds = ($values | Measure-Object -Average).Average
        MedianMilliseconds = Get-Percentile $values 0.5
        P95Milliseconds = Get-Percentile $values 0.95
        P99Milliseconds = Get-Percentile $values 0.99
        MaximumMilliseconds = $values[-1]
    }
}
$results | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'RecheckedExistingLatency.json') -Encoding utf8
$results | Select-Object Id, InputSamples, MeanMilliseconds, MedianMilliseconds, P95Milliseconds, P99Milliseconds, MaximumMilliseconds | Format-Table
