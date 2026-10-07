param([Parameter(Mandatory)][string] $Executable)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$applicationDirectory = Split-Path $PSScriptRoot
$logs = Join-Path $applicationDirectory 'LogOutput'
$previousDirectories = @(Get-ChildItem -LiteralPath $logs -Directory | Select-Object -ExpandProperty FullName)
$arguments = @('--hidden', '--software-adapter', '--duration', '2')
$first = Start-Process -FilePath $Executable -ArgumentList $arguments -WindowStyle Hidden -PassThru
$second = Start-Process -FilePath $Executable -ArgumentList $arguments -WindowStyle Hidden -PassThru
try {
    foreach ($application in @($first, $second)) {
        if (-not $application.WaitForExit(20000)) { throw 'Concurrent session test timed out.' }
        if ($application.ExitCode -ne 0) { throw 'Concurrent session launch failed.' }
    }
    $newSessions = @(Get-ChildItem -LiteralPath $logs -Directory | Where-Object {
        $_.FullName -notin $previousDirectories -and (Test-Path -LiteralPath (Join-Path $_.FullName 'Application.log'))
    })
    if ($newSessions.Count -ne 2) { throw 'Concurrent launches did not reserve separate session directories.' }
    foreach ($session in $newSessions) {
        if ($session.Name -notmatch '^\d{4}-\d{2}-\d{2}_\d{2}-\d{2}-\d{2}$') { throw 'Session naming format changed.' }
        $applicationLog = Get-Content -LiteralPath (Join-Path $session.FullName 'Application.log')
        if ($applicationLog -notcontains 'LoggingFailed=0' -or $applicationLog[-1] -ne 'InputLatency shutdown') { throw 'A concurrent session failed to finish its own logs.' }
    }
    $protectedDirectory = $newSessions[0].FullName
    $hashes = @{}
    foreach ($file in Get-ChildItem -LiteralPath $protectedDirectory -File) { $hashes[$file.Name] = (Get-FileHash -LiteralPath $file.FullName).Hash }
    $reuseArguments = @('--hidden', '--duration', '1', '--log-directory', ('"' + $protectedDirectory + '"'))
    $reuse = Start-Process -FilePath $Executable -ArgumentList $reuseArguments -WindowStyle Hidden -Wait -PassThru
    if ($reuse.ExitCode -ne 1) { throw 'Reusing an occupied session directory was not rejected.' }
    foreach ($file in Get-ChildItem -LiteralPath $protectedDirectory -File) {
        if (-not $hashes.ContainsKey($file.Name) -or (Get-FileHash -LiteralPath $file.FullName).Hash -ne $hashes[$file.Name]) { throw 'Rejected session reuse modified existing logs.' }
    }
    Write-Host 'Concurrent launches used separate timestamp directories; occupied-directory reuse failed without modifying logs.'
}
finally {
    foreach ($application in @($first, $second)) {
        $application.Refresh()
        if (-not $application.HasExited) { $application.Kill(); $application.WaitForExit() }
    }
}
