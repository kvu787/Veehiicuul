Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$applicationRoot = Split-Path -Parent $PSScriptRoot
$logDirectory = & (Join-Path $applicationRoot 'CreateSessionDirectory.ps1')
$checks = 0
$heldFiles = [System.Collections.Generic.List[System.IO.FileStream]]::new()
Start-Transcript -LiteralPath (Join-Path $logDirectory 'LaunchIsolationTests.log') | Out-Null

function Require([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw "Launch isolation verification failed: $Message" }
    $script:checks++
}

function WaitForProcess([System.Diagnostics.Process]$Process) {
    if (-not $Process.WaitForExit(60000)) {
        $Process.Kill($true)
        throw 'A verification child process exceeded 60 seconds.'
    }
}

function StartVerification([string]$Name) {
    $wrapper = Join-Path $applicationRoot 'Run.cmd'
    # Exercise the real double-click wrapper. The verification argument keeps
    # the WPF app and this helper off all physical displays.
    return Start-Process -FilePath $env:ComSpec -ArgumentList ('/d /s /c ""{0}" -VerifyInterface"' -f $wrapper) -WindowStyle Hidden -PassThru `
        -RedirectStandardOutput (Join-Path $logDirectory ($Name + '.log')) -RedirectStandardError (Join-Path $logDirectory ($Name + '.Error.log'))
}

function ReadLaunch([string]$Name) {
    $output = Get-Content -LiteralPath (Join-Path $logDirectory ($Name + '.log')) -Raw
    Require ($output.Contains('Offscreen interface verification passed. No window was shown.')) "$Name exercises the interactive controls and previews"
    $match = [regex]::Match($output, '(?m)^Session logs: (.+)\r?$')
    Require $match.Success "$Name reports its session"
    $session = $match.Groups[1].Value.Trim()
    $launcher = Get-Content -LiteralPath (Join-Path $session 'Launcher.log')
    $build = ($launcher | Where-Object { $_.StartsWith('Build: ') }).Substring(7)
    $executable = ($launcher | Where-Object { $_.StartsWith('Application: ') }).Substring(13)
    Require ($executable -eq (Join-Path $session 'Application/ShaderUnderstandingApp.exe')) "$Name runs the session's private copy"
    Require ((Get-FileHash -LiteralPath (Join-Path $build 'ShaderUnderstandingApp.dll')).Hash -eq (Get-FileHash -LiteralPath (Join-Path $session 'Application/ShaderUnderstandingApp.dll')).Hash) "$Name copies the complete selected build"
    return [pscustomobject]@{ Session = $session; Build = $build; Executable = $executable }
}

try {
    $currentBuildPath = Join-Path $applicationRoot 'BuildOutput/CurrentBuild.txt'
    Require (Test-Path -LiteralPath $currentBuildPath) 'a completed build exists before testing'
    $previousReference = [System.IO.File]::ReadAllText($currentBuildPath)
    $firstProcess = StartVerification 'FirstLaunch'
    WaitForProcess $firstProcess
    Require ($firstProcess.ExitCode -eq 0) 'the first Run.cmd invocation succeeds'
    $first = ReadLaunch 'FirstLaunch'

    $privateAssembly = Join-Path $first.Session 'Application/ShaderUnderstandingApp.dll'
    $publishedAssembly = Join-Path $first.Build 'ShaderUnderstandingApp.dll'
    $privateHash = (Get-FileHash -LiteralPath $privateAssembly).Hash
    # Deny writes and renames for the entire rebuild, modeling a long-lived
    # Windows app. Also lock the selected publication while another launch
    # copies it, proving the build never overwrites that publication.
    foreach ($path in @($privateAssembly, $publishedAssembly)) {
        $heldFiles.Add([System.IO.File]::Open($path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::Read))
    }
    $secondProcess = StartVerification 'DuringBuildLaunch'
    $buildScript = Join-Path $applicationRoot 'Build.ps1'
    $buildProcess = Start-Process -FilePath 'pwsh' -ArgumentList @('-NoProfile', '-File', ('"{0}"' -f $buildScript)) -WindowStyle Hidden -PassThru `
        -RedirectStandardOutput (Join-Path $logDirectory 'ConcurrentBuild.log') -RedirectStandardError (Join-Path $logDirectory 'ConcurrentBuild.Error.log')
    WaitForProcess $secondProcess
    WaitForProcess $buildProcess
    Require ($secondProcess.ExitCode -eq 0) 'Run.cmd remains usable during a rebuild'
    Require ($buildProcess.ExitCode -eq 0) 'rebuilding succeeds while session and publication assemblies are locked'
    $second = ReadLaunch 'DuringBuildLaunch'
    Require ($second.Session -ne $first.Session) 'launches have distinct session directories'
    Require ((Get-FileHash -LiteralPath $privateAssembly).Hash -eq $privateHash) 'rebuilding preserves the running version'
    Require ([System.IO.File]::ReadAllText($currentBuildPath) -ne $previousReference) 'the successful rebuild publishes a new build'

    $thirdProcess = StartVerification 'AfterBuildLaunch'
    WaitForProcess $thirdProcess
    Require ($thirdProcess.ExitCode -eq 0) 'Run.cmd succeeds after publication'
    $third = ReadLaunch 'AfterBuildLaunch'
    Require ($third.Build -ne $first.Build) 'the next launch selects the newly published build'

    # A separate launcher fixture checks both missing-build paths without
    # modifying the real selection or any active user's files.
    $fixture = Join-Path $logDirectory 'MissingBuildApplication'
    New-Item -ItemType Directory -Path $fixture | Out-Null
    foreach ($name in @('Run.ps1', 'CreateSessionDirectory.ps1')) {
        Copy-Item -LiteralPath (Join-Path $applicationRoot $name) -Destination (Join-Path $fixture $name)
    }
    foreach ($case in @('MissingReference', 'MissingExecutable')) {
        if ($case -eq 'MissingExecutable') {
            $fixtureBuild = Join-Path $fixture 'BuildOutput/Releases/EmptyBuild'
            New-Item -ItemType Directory -Path $fixtureBuild -Force | Out-Null
            [System.IO.File]::WriteAllText((Join-Path $fixture 'BuildOutput/CurrentBuild.txt'), 'Releases/EmptyBuild')
        }
        $missing = Start-Process -FilePath 'pwsh' -ArgumentList @('-NoProfile', '-File', ('"{0}"' -f (Join-Path $fixture 'Run.ps1')), '-VerifyInterface') -WindowStyle Hidden -PassThru `
            -RedirectStandardOutput (Join-Path $logDirectory ($case + '.log')) -RedirectStandardError (Join-Path $logDirectory ($case + '.Error.log'))
        WaitForProcess $missing
        Require ($missing.ExitCode -eq 1) "$case refuses to launch"
        Require ((Get-Content -LiteralPath (Join-Path $logDirectory ($case + '.Error.log')) -Raw).Contains('No built application was found.')) "$case explains how to build"
        Require (-not (Test-Path -LiteralPath (Join-Path $fixture 'LogOutput'))) "$case creates no application session"
    }
    Copy-Item -LiteralPath (Join-Path $applicationRoot 'Build.ps1') -Destination (Join-Path $fixture 'Build.ps1')
    $fixtureReferencePath = Join-Path $fixture 'BuildOutput/CurrentBuild.txt'
    $fixtureReference = [System.IO.File]::ReadAllText($fixtureReferencePath)
    $realReference = [System.IO.File]::ReadAllText($currentBuildPath)
    # The fixture deliberately has no project, so publishing must fail before
    # replacing its previous selection. No real source files are modified.
    $failedBuild = Start-Process -FilePath 'pwsh' -ArgumentList @('-NoProfile', '-File', ('"{0}"' -f (Join-Path $fixture 'Build.ps1'))) -WindowStyle Hidden -PassThru `
        -RedirectStandardOutput (Join-Path $logDirectory 'FailedBuild.log') -RedirectStandardError (Join-Path $logDirectory 'FailedBuild.Error.log')
    WaitForProcess $failedBuild
    Require ($failedBuild.ExitCode -eq 1) 'a failed publish reports failure'
    Require ([System.IO.File]::ReadAllText($fixtureReferencePath) -eq $fixtureReference) 'a failed publish preserves the previous selection'
    Require ([System.IO.File]::ReadAllText($currentBuildPath) -eq $realReference) 'failure testing leaves the real build selection intact'
    Write-Host "Passed $checks launch isolation checks. Logs: $logDirectory"
} finally {
    foreach ($stream in $heldFiles) { $stream.Dispose() }
    Stop-Transcript | Out-Null
}
