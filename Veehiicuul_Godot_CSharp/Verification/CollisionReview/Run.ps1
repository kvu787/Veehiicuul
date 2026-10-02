param(
    [switch] $SkipBenchmarks,
    [ValidateSet('All', 'Engine')] [string] $Stage = 'All',
    [switch] $UseDefaultTiering
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$transcriptStarted = $false
$previousOutput = [Environment]::GetEnvironmentVariable('COLLISION_REVIEW_OUTPUT')
$previousTiering = [Environment]::GetEnvironmentVariable('DOTNET_TieredCompilation')
$previousSkip = [Environment]::GetEnvironmentVariable('COLLISION_REVIEW_SKIP_BENCHMARKS')
try {
    $engineExecutable = Join-Path $PSScriptRoot 'MyBuildOutput\EngineProject\Build\Veehiicuul_Godot_CSharp.exe'
    $managedExecutable = Join-Path $PSScriptRoot 'MyBuildOutput\Managed\net10.0\CollisionReview.exe'
    if (-not (Test-Path -LiteralPath $engineExecutable -PathType Leaf) -or -not (Test-Path -LiteralPath $managedExecutable -PathType Leaf)) {
        throw 'Build the verification applications with Build.cmd before running Run.cmd.'
    }
    $logDirectory = Join-Path $PSScriptRoot ('MyLogOutput\' + (Get-Date -Format 'yyyy-MM-dd_HH-mm-ss'))
    New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
    Start-Transcript -LiteralPath (Join-Path $logDirectory 'Run.log') | Out-Null
    $transcriptStarted = $true
    $env:COLLISION_REVIEW_OUTPUT = $logDirectory
    $env:COLLISION_REVIEW_SKIP_BENCHMARKS = if ($SkipBenchmarks) { '1' } else { '0' }
    if ($UseDefaultTiering) { [Environment]::SetEnvironmentVariable('DOTNET_TieredCompilation', $null) }
    else { $env:DOTNET_TieredCompilation = '0' }
    Write-Host "Review output: $logDirectory"
    $stages = if ($Stage -eq 'All') { @('Engine', 'Managed') } else { @($Stage) }
    foreach ($currentStage in $stages) {
        $start = [Diagnostics.ProcessStartInfo]::new()
        $start.UseShellExecute = $false
        $start.CreateNoWindow = $true
        $start.RedirectStandardOutput = $true
        $start.RedirectStandardError = $true
        if ($currentStage -eq 'Engine') {
            $start.FileName = $engineExecutable
            $start.Arguments = '--headless --log-file "' + (Join-Path $logDirectory 'Godot.log') + '"'
        } else {
            $start.FileName = $managedExecutable
            $start.Arguments = '"' + $logDirectory + '" "' + ([IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\Veehiicuul\Tracks\Ribeye\Ribeye_ColliderData.json'))) + '"'
            if ($SkipBenchmarks) { $start.Arguments += ' --skip-benchmarks' }
        }
        $process = [Diagnostics.Process]::new()
        try {
            $process.StartInfo = $start
            if (-not $process.Start()) { throw "Unable to launch $currentStage." }
            $process.PriorityClass = [Diagnostics.ProcessPriorityClass]::BelowNormal
            $standardOutput = $process.StandardOutput.ReadToEndAsync()
            $standardError = $process.StandardError.ReadToEndAsync()
            $process.WaitForExit()
            $output = $standardOutput.GetAwaiter().GetResult()
            $errors = $standardError.GetAwaiter().GetResult()
            [IO.File]::WriteAllText((Join-Path $logDirectory ($currentStage + '.log')), $output + $errors, [Text.UTF8Encoding]::new($false))
            Write-Host $output
            if ($process.ExitCode -ne 0) { throw "$currentStage exited with code $($process.ExitCode). $errors" }
        }
        finally { $process.Dispose() }
    }
}
catch {
    Write-Host $_.Exception.ToString() -ForegroundColor Red
    exit 1
}
finally {
    [Environment]::SetEnvironmentVariable('COLLISION_REVIEW_OUTPUT', $previousOutput)
    [Environment]::SetEnvironmentVariable('DOTNET_TieredCompilation', $previousTiering)
    [Environment]::SetEnvironmentVariable('COLLISION_REVIEW_SKIP_BENCHMARKS', $previousSkip)
    if ($transcriptStarted) { Stop-Transcript | Out-Null }
}
