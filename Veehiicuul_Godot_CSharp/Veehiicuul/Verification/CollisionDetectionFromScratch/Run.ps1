param(
    [switch] $ValidationOnly,
    [switch] $SkipNative,
    [switch] $SkipWindowedFrames,
    [ValidateRange(0.01, 100.0)] [double] $Scale = 1.0,
    [ValidateRange(1, 10)] [int] $Repetitions = 3,
    [ValidateRange(1, 60)] [int] $ColdStartProcesses = 15,
    # Any of: Validation, Simulation, Measure, Variants, ColdStart.
    [string[]] $Stages = @('Validation', 'Simulation', 'Measure', 'Variants', 'ColdStart'),
    [string[]] $Suites = @('queries', 'paths', 'extent', 'spacing', 'cellsize', 'breakdown', 'construction', 'single')
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# powershell.exe -File passes a comma-separated list as one string.
$Stages = @($Stages | ForEach-Object { $_ -split ',' } | Where-Object { $_ -ne '' })
$Suites = @($Suites | ForEach-Object { $_ -split ',' } | Where-Object { $_ -ne '' })
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
        [int] $TimeoutSeconds = 3600
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

function Invoke-Native {
    param([string] $Name, [string] $Mode, [bool] $Headless, [string[]] $Extra = @())
    $arguments = @()
    if ($Headless) { $arguments += '--headless' } else { $arguments += @('--windowed', '--resolution', '1280x720') }
    $arguments += @('--log-file', (Join-Path $logFolderPath ($Name + '.log')), '--', "--mode=$Mode", ('--output=' + (Join-Path $logFolderPath ($Name + '.json'))))
    $arguments += $Extra
    Invoke-Logged -Name $Name -Program $nativeExecutable -Arguments $arguments -WorkingDirectory $nativeDirectory -TimeoutSeconds 900
}

Write-Host "Results: $logFolderPath"
if ($Stages -contains 'Validation') {
    Invoke-Logged -Name 'Validation' -Program $executable -Arguments @('validate', "--scale=$Scale", ('--output=' + (Join-Path $logFolderPath 'Validation.json')))
    if (-not $SkipNative) {
        Invoke-Native -Name 'NativeChecks' -Mode 'checks' -Headless $true
    }
}

if ($Stages -contains 'Simulation') {
    Invoke-Logged -Name 'Simulation' -Program $executable -Arguments @('simulate', ('--output=' + (Join-Path $logFolderPath 'Simulation.json')))
}

if ($Stages -contains 'Measure') {
    for ($repetition = 1; $repetition -le $Repetitions; ++$repetition) {
        foreach ($suite in $Suites) {
            $name = "Measure_${suite}_$repetition"
            Invoke-Logged -Name $name -Program $executable -Arguments @('measure', "--suite=$suite", ('--output=' + (Join-Path $logFolderPath ($name + '.json'))))
        }
    }

    if (-not $SkipNative) {
        for ($repetition = 1; $repetition -le $Repetitions; ++$repetition) {
            Invoke-Native -Name "NativeMeasure_$repetition" -Mode 'measure' -Headless $true
            Invoke-Native -Name "NativeFramesHeadless_$repetition" -Mode 'frames' -Headless $true -Extra @('--frames=6000')
            if (-not $SkipWindowedFrames) {
                Invoke-Native -Name "NativeFramesWindowed_$repetition" -Mode 'frames' -Headless $false -Extra @('--frames=6000')
            }
        }
    }
}

if ($Stages -contains 'Variants') {
    # The same query suite on two tracks under other runtime settings and other cores.
    $variants = @(
        @{ Name = 'NoDynamicProfile'; Environment = @{ DOTNET_TieredPGO = '0' }; Affinity = 'performance' },
        @{ Name = 'NoTiering'; Environment = @{ DOTNET_TieredCompilation = '0' }; Affinity = 'performance' },
        @{ Name = 'EfficiencyCore'; Environment = @{}; Affinity = 'efficiency' },
        @{ Name = 'Unpinned'; Environment = @{}; Affinity = 'none' }
    )
    foreach ($variant in $variants) {
        for ($repetition = 1; $repetition -le $Repetitions; ++$repetition) {
            $name = "Variant_$($variant.Name)_$repetition"
            Invoke-Logged -Name $name -Program $executable -Environment $variant.Environment -Arguments @(
                'measure', '--suite=queries', '--tracks=Circuit,CircuitWide', "--affinity=$($variant.Affinity)",
                ('--output=' + (Join-Path $logFolderPath ($name + '.json'))))
        }
    }
}

if ($Stages -contains 'ColdStart') {
    foreach ($track in @('Circuit', 'CircuitFine', 'CircuitWide', 'Crowded')) {
        for ($process = 1; $process -le $ColdStartProcesses; ++$process) {
            Invoke-Logged -Name "ColdStart_${track}_$process" -Program $executable -Arguments @(
                'coldstart', "--track=$track", ('--output=' + (Join-Path $logFolderPath "ColdStart_$track.jsonl")))
        }
    }
}

if ($script:failed) {
    Write-Host 'At least one step failed. See the files in the results folder.' -ForegroundColor Red
    exit 1
}
Write-Host 'Every step passed.'
exit 0
