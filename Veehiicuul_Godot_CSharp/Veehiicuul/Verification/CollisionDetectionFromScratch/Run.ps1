param(
    [switch] $ValidationOnly,
    [switch] $SkipNative,
    [switch] $SkipWindowedFrames,
    [ValidateRange(0.01, 100.0)] [double] $Scale = 1.0,
    [ValidateRange(1, 10)] [int] $Repetitions = 3,
    [ValidateRange(1, 60)] [int] $ColdStartProcesses = 15,
    # Any of: Validation, Simulation, Measure, NativeMeasure, Variants, ColdStart.
    [string[]] $Stages = @('Validation', 'Simulation', 'Measure', 'NativeMeasure', 'Variants', 'ColdStart'),
    [string[]] $Suites = @('queries', 'paths', 'indexfootprint', 'extent', 'spacing', 'cellsize', 'breakdown', 'construction', 'single'),
    [string[]] $Tracks = @(),
    [string[]] $ColdStartTracks = @('Circuit', 'CircuitFine', 'CircuitWide', 'Crowded'),
    # performance, efficiency, none, or logical processor numbers such as 19,20,21.
    # The defaults give the cleanest timing on an idle machine. While the machine is
    # in use for something else, name processors that are idle and use -Priority normal.
    [string[]] $Affinity = @('performance'),
    [ValidateSet('high', 'normal', 'belownormal')] [string] $Priority = 'high',
    # Validation threads. Zero leaves the choice to the harness, or uses one thread
    # for each processor that -Affinity lists.
    [ValidateRange(0, 256)] [int] $Threads = 0
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# powershell.exe -File passes a comma-separated list as one string.
$Stages = @($Stages | ForEach-Object { $_ -split ',' } | Where-Object { $_ -ne '' })
$Suites = @($Suites | ForEach-Object { $_ -split ',' } | Where-Object { $_ -ne '' })
$Tracks = @($Tracks | ForEach-Object { $_ -split ',' } | Where-Object { $_ -ne '' })
$ColdStartTracks = @($ColdStartTracks | ForEach-Object { $_ -split ',' } | Where-Object { $_ -ne '' })
$affinityItems = @($Affinity | ForEach-Object { $_ -split '[,\s]+' } | Where-Object { $_ -ne '' })
$affinityText = $affinityItems -join ','
if ($ValidationOnly) { $Stages = @('Validation') }

$executable = Join-Path $PSScriptRoot 'bin\Release\net10.0\CollisionDetectionFromScratch.exe'
$nativeDirectory = Join-Path $PSScriptRoot 'NativeProject\Build'
$nativeExecutable = Join-Path $nativeDirectory 'CollisionDetectionNative.exe'
if (-not (Test-Path -LiteralPath $executable -PathType Leaf)) {
    Write-Host 'The harness has not been built. Run Build.cmd first.' -ForegroundColor Red
    exit 1
}
if (-not $SkipNative -and -not (Test-Path -LiteralPath $nativeExecutable -PathType Leaf)) {
    Write-Host 'The engine-side harness has not been built. Run Build.cmd first.' -ForegroundColor Red
    exit 1
}

# A timing process of the console program places itself. The launcher places the
# other processes: the engine-side harness on the named processors, and every
# process on them when they are listed by number.
function Get-Placement {
    param([string] $Request)
    $line = @(& $executable placement "--affinity=$Request" | Where-Object { $_ -like 'PLACEMENT *' })
    if ($LASTEXITCODE -ne 0 -or $line.Count -ne 1) { return $null }
    return $line[0].Substring('PLACEMENT '.Length) | ConvertFrom-Json
}
$placement = Get-Placement $affinityText
if ($null -eq $placement) {
    Write-Host "The affinity '$affinityText' could not be resolved." -ForegroundColor Red
    exit 1
}
$namedMask = [long] $placement.Mask
$listedMask = if ($affinityText -match '^\d') { $namedMask } else { [long] 0 }
if ($Threads -eq 0 -and $listedMask -ne 0) { $Threads = $affinityItems.Count }
$priorityClass = switch ($Priority) {
    'high' { [System.Diagnostics.ProcessPriorityClass]::High }
    'normal' { [System.Diagnostics.ProcessPriorityClass]::Normal }
    default { [System.Diagnostics.ProcessPriorityClass]::BelowNormal }
}
$timingPlacement = @("--affinity=$affinityText", "--priority=$Priority")

$logFolderPath = Join-Path $PSScriptRoot ('MyLogOutput\' + (Get-Date -Format 'yyyy-MM-dd_HH-mm-ss'))
New-Item -ItemType Directory -Path $logFolderPath -Force | Out-Null
$utf8 = [System.Text.UTF8Encoding]::new($false)
$script:failed = $false
$runtimeVariables = @('DOTNET_TieredCompilation', 'DOTNET_TieredPGO', 'DOTNET_ReadyToRun')

# Runs one process and stores everything it prints. Windows PowerShell transcripts
# omit the output of console programs, so it is captured here.
function Invoke-Logged {
    param(
        [string] $Name,
        [string] $Program,
        [string[]] $Arguments,
        [hashtable] $Environment = @{},
        [string] $WorkingDirectory = $PSScriptRoot,
        [int] $TimeoutSeconds = 3600,
        # For programs that do not place themselves.
        [switch] $Place,
        [long] $AffinityMask = 0
    )
    $process = New-Object System.Diagnostics.Process
    $process.StartInfo.FileName = $Program
    $process.StartInfo.Arguments = ($Arguments | ForEach-Object { if ($_ -match '[\s"]') { '"' + $_ + '"' } else { $_ } }) -join ' '
    $process.StartInfo.WorkingDirectory = $WorkingDirectory
    $process.StartInfo.UseShellExecute = $false
    $process.StartInfo.CreateNoWindow = $true
    $process.StartInfo.RedirectStandardOutput = $true
    $process.StartInfo.RedirectStandardError = $true
    foreach ($variable in $runtimeVariables) { [void] $process.StartInfo.EnvironmentVariables.Remove($variable) }
    foreach ($key in $Environment.Keys) { $process.StartInfo.EnvironmentVariables[$key] = [string] $Environment[$key] }
    try {
        [void] $process.Start()
        if ($Place) {
            $process.PriorityClass = $priorityClass
            if ($AffinityMask -ne 0) { $process.ProcessorAffinity = [IntPtr] $AffinityMask }
        }
        $standardOutput = $process.StandardOutput.ReadToEndAsync()
        $standardError = $process.StandardError.ReadToEndAsync()
        $finished = $process.WaitForExit($TimeoutSeconds * 1000)
        if (-not $finished) {
            $process.Kill()
            $process.WaitForExit()
        }
        $text = $standardOutput.Result + $standardError.Result
        [System.IO.File]::WriteAllText((Join-Path $logFolderPath ($Name + '.txt')), $text, $utf8)
        if (-not $finished) {
            Write-Host "$Name exceeded $TimeoutSeconds seconds." -ForegroundColor Red
            $script:failed = $true
        } else {
            Write-Host "$Name finished with exit code $($process.ExitCode)."
            if ($process.ExitCode -ne 0) { $script:failed = $true }
        }
    } finally {
        $process.Dispose()
    }
}

# Records how busy each logical processor is, so that a run states its conditions.
# Utility counts work done, so that a core above its nominal frequency exceeds 100.
function Write-ProcessorUse {
    param([string] $Name)
    $record = [ordered] @{ Time = (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'); Seconds = 3 }
    try {
        foreach ($counter in @('% Processor Utility', '% Processor Time')) {
            $samples = @(Get-Counter -Counter "\Processor Information(*)\$counter" -SampleInterval 1 -MaxSamples 3)
            $means = @{}
            foreach ($sample in $samples) {
                foreach ($value in $sample.CounterSamples) {
                    if ($value.InstanceName -match '^0,(\d+)$') {
                        $processor = [int] $Matches[1]
                        if (-not $means.ContainsKey($processor)) { $means[$processor] = 0.0 }
                        $means[$processor] += $value.CookedValue / $samples.Count
                    }
                }
            }
            $record[$counter] = @($means.Keys | Sort-Object | ForEach-Object { [math]::Round($means[$_], 1) })
        }
    } catch {
        $record['Error'] = $_.Exception.Message
    }
    [System.IO.File]::WriteAllText((Join-Path $logFolderPath ($Name + '.json')), ($record | ConvertTo-Json -Compress), $utf8)
}

function Invoke-Native {
    param([string] $Name, [string] $Mode, [bool] $Headless, [long] $AffinityMask, [string[]] $Extra = @())
    $arguments = @()
    if ($Headless) { $arguments += '--headless' } else { $arguments += @('--windowed', '--resolution', '1280x720') }
    $arguments += @('--log-file', (Join-Path $logFolderPath ($Name + '.log')), '--', "--mode=$Mode", ('--output=' + (Join-Path $logFolderPath ($Name + '.json'))))
    $arguments += $Extra
    Invoke-Logged -Name $Name -Program $nativeExecutable -Arguments $arguments -WorkingDirectory $nativeDirectory -TimeoutSeconds 900 -Place -AffinityMask $AffinityMask
}

Write-Host "Results: $logFolderPath"
Write-Host "Placement: $($placement.Description); priority=$Priority"
$placementRecord = [ordered] @{
    Affinity = $affinityText
    Priority = $Priority
    TimingProcessor = $placement.TimingProcessor
    Mask = $placement.Mask
    Description = $placement.Description
    PerformanceMask = (Get-Placement 'performance').Mask
    EfficiencyMask = (Get-Placement 'efficiency').Mask
}
[System.IO.File]::WriteAllText((Join-Path $logFolderPath 'Placement.json'), ($placementRecord | ConvertTo-Json), $utf8)
Write-ProcessorUse -Name 'ProcessorUseBefore'
if ($Stages -contains 'Validation') {
    $arguments = @('validate', "--scale=$Scale", ('--output=' + (Join-Path $logFolderPath 'Validation.json')))
    if ($Threads -gt 0) { $arguments += "--threads=$Threads" }
    Invoke-Logged -Name 'Validation' -Program $executable -Arguments $arguments -Place -AffinityMask $listedMask
    if (-not $SkipNative) {
        Invoke-Native -Name 'NativeChecks' -Mode 'checks' -Headless $true -AffinityMask $listedMask
    }
}

if ($Stages -contains 'Simulation') {
    Invoke-Logged -Name 'Simulation' -Program $executable -Place -AffinityMask $listedMask -Arguments @(
        'simulate', ('--output=' + (Join-Path $logFolderPath 'Simulation.json')))
}

if ($Stages -contains 'Measure') {
    for ($repetition = 1; $repetition -le $Repetitions; ++$repetition) {
        foreach ($suite in $Suites) {
            $name = "Measure_${suite}_$repetition"
            $arguments = @('measure', "--suite=$suite", ('--output=' + (Join-Path $logFolderPath ($name + '.json')))) + $timingPlacement
            if ($Tracks.Count -gt 0) { $arguments += '--tracks=' + ($Tracks -join ',') }
            Invoke-Logged -Name $name -Program $executable -Arguments $arguments
        }
    }
}

if ($Stages -contains 'NativeMeasure' -and -not $SkipNative) {
    for ($repetition = 1; $repetition -le $Repetitions; ++$repetition) {
        Invoke-Native -Name "NativeMeasure_$repetition" -Mode 'measure' -Headless $true -AffinityMask $namedMask
        Invoke-Native -Name "NativeFramesHeadless_$repetition" -Mode 'frames' -Headless $true -AffinityMask $namedMask -Extra @('--frames=6000')
        if (-not $SkipWindowedFrames) {
            Invoke-Native -Name "NativeFramesWindowed_$repetition" -Mode 'frames' -Headless $false -AffinityMask $namedMask -Extra @('--frames=6000')
        }
    }
}

if ($Stages -contains 'Variants') {
    # The same query suite on two tracks under other runtime settings and other cores.
    $variants = @(
        @{ Name = 'NoDynamicProfile'; Environment = @{ DOTNET_TieredPGO = '0' }; Affinity = $affinityText },
        @{ Name = 'NoTiering'; Environment = @{ DOTNET_TieredCompilation = '0' }; Affinity = $affinityText },
        @{ Name = 'EfficiencyCore'; Environment = @{}; Affinity = 'efficiency' },
        @{ Name = 'Unpinned'; Environment = @{}; Affinity = 'none' }
    )
    foreach ($variant in $variants) {
        for ($repetition = 1; $repetition -le $Repetitions; ++$repetition) {
            $name = "Variant_$($variant.Name)_$repetition"
            Invoke-Logged -Name $name -Program $executable -Environment $variant.Environment -Arguments @(
                'measure', '--suite=queries', '--tracks=Circuit,CircuitWide', "--affinity=$($variant.Affinity)",
                "--priority=$Priority", ('--output=' + (Join-Path $logFolderPath ($name + '.json'))))
        }
    }
}

if ($Stages -contains 'ColdStart') {
    foreach ($track in $ColdStartTracks) {
        for ($process = 1; $process -le $ColdStartProcesses; ++$process) {
            Invoke-Logged -Name "ColdStart_${track}_$process" -Program $executable -Arguments (@(
                'coldstart', "--track=$track", ('--output=' + (Join-Path $logFolderPath "ColdStart_$track.jsonl"))) + $timingPlacement)
        }
    }
}

Write-ProcessorUse -Name 'ProcessorUseAfter'
if ($script:failed) {
    Write-Host 'At least one step failed. See the files in the results folder.' -ForegroundColor Red
    exit 1
}
Write-Host 'Every step passed.'
exit 0
