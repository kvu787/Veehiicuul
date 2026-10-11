[CmdletBinding()]
param(
    [switch]$Verify,
    [switch]$WaitForKey
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$resultCode = 0
$transcriptStarted = $false
try {
    if (-not $IsWindows -or [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture -ne [System.Runtime.InteropServices.Architecture]::X64) {
        throw 'ShaderUnderstandingApp supports Windows 11 x64 only.'
    }
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw 'Install the .NET 10 SDK for Windows x64, then run Build.cmd again.'
    }
    $logDirectory = & (Join-Path $PSScriptRoot 'CreateSessionDirectory.ps1')
    Start-Transcript -LiteralPath (Join-Path $logDirectory 'Build.log') | Out-Null
    $transcriptStarted = $true
    $project = Join-Path $PSScriptRoot 'ShaderUnderstandingApp.csproj'
    $buildRoot = Join-Path $PSScriptRoot 'BuildOutput'
    $buildName = 'Build' + [Guid]::NewGuid().ToString('N')
    $relativeDestination = 'Releases/' + $buildName
    $destination = Join-Path $buildRoot $relativeDestination
    & dotnet publish $project --configuration Release --runtime win-x64 --self-contained false --output $destination --verbosity minimal
    if ($LASTEXITCODE -ne 0) { throw "Build failed with exit code $LASTEXITCODE." }

    if ($Verify) {
        & dotnet run --project (Join-Path $PSScriptRoot 'Tests/ShaderMathematicsTests/ShaderMathematicsTests.csproj') --configuration Release
        if ($LASTEXITCODE -ne 0) { throw "Mathematical verification failed with exit code $LASTEXITCODE." }
        & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'Run.ps1') -VerifyInterface -BuildDirectory $destination
        if ($LASTEXITCODE -ne 0) { throw "Interface verification failed with exit code $LASTEXITCODE." }
    }
    # Publish only after compilation and requested verification succeed. The
    # same-volume rename changes a single small pointer atomically; published
    # binaries are never overwritten, even while another launcher copies them.
    $pendingReference = Join-Path $buildRoot ($buildName + '.txt')
    [System.IO.File]::WriteAllText($pendingReference, $relativeDestination, [System.Text.UTF8Encoding]::new($false))
    [System.IO.File]::Move($pendingReference, (Join-Path $buildRoot 'CurrentBuild.txt'), $true)
    Write-Host "Built $destination\ShaderUnderstandingApp.exe"
    Write-Host 'Double-click Run.cmd to open the application.'
} catch {
    [Console]::Error.WriteLine($_.Exception.Message)
    $resultCode = 1
} finally {
    if ($transcriptStarted) { Stop-Transcript | Out-Null }
}
if ($WaitForKey) { Read-Host 'Press Enter to close' | Out-Null }
exit $resultCode
