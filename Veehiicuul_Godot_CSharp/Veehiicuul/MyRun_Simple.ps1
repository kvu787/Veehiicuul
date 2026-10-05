Set-StrictMode -Version "Latest"
$ErrorActionPreference = "Stop"

$ProcessName = "Veehiicuul_Godot_CSharp.exe"
$ProcessPath = "$env:UserProfile\Repository\Veehiicuul\Veehiicuul_Godot_CSharp\Veehiicuul\MyBuildOutput\Veehiicuul_Godot_CSharp.exe"
$PresentMonPath = "$env:UserProfile\Program\PresentMon-2.6.0-x64.exe"
$UsePresentMon = $args -notcontains 'NoPresentMon'

$LogFolderPath = "$env:UserProfile\Repository\Veehiicuul\Veehiicuul_Godot_CSharp\Veehiicuul\MyLogOutput\$(Get-Date -Format "yyyy-MM-dd_HH-mm-ss")"
$GodotLogFilePath = "$logFolderPath\Godot.log"
$PresentMonLogFilePath = "$LogFolderPath\PresentMon.csv"

New-Item -ItemType "Directory" -Path $LogFolderPath

Start-Transcript -Path "$LogFolderPath\ScriptOutput.log"

try {
    . {
        Write-Host "ProcessName = $($ProcessName)"
        Write-Host "ProcessPath = $($ProcessPath)"
        Write-Host "PresentMonPath = $($PresentMonPath)"

        $presentMonProcess = $null
        if ($UsePresentMon -and (Test-Path $PresentMonPath)) {
            $identity = [System.Security.Principal.WindowsIdentity]::GetCurrent()
            $principal = [System.Security.Principal.WindowsPrincipal]::new($identity)
            $groupSid = [System.Security.Principal.SecurityIdentifier]::new('S-1-5-32-559') # S-1-5-32-559 is the stable identifier for the "Performance Log Users" group
            if (-not $principal.IsInRole($groupSid)) {
                throw "Trying to run PresentMon, but user '$($identity.Name)' must belong to the 'Performance Log Users' group."
            }

            $PresentMonSessionName = 'Veehiicuul' + [Guid]::NewGuid().ToString('N')

            # `--terminate_on_proc_exit` isn't used because I've observed it not stopping PresentMon even after the game exits.
            $presentMonProcess = Start-Process `
                -FilePath $PresentMonPath `
                -ArgumentList "--process_name `"$($ProcessName)`" --session_name $PresentMonSessionName --output_file `"$($PresentMonLogFilePath)`" --set_circular_buffer_size 65536 --no_console_stats --qpc_time --track_etw_status" `
                -WindowStyle "Hidden" `
                -PassThru `
                -RedirectStandardOutput "$LogFolderPath\PresentMonOutput_Standard.log" `
                -RedirectStandardError "$LogFolderPath\PresentMonOutput_Error.log"

            # Work around Windows PowerShell 5.1 returning a null ExitCode for redirected
            # Start-Process output, even after WaitForExit(). Retain the handle while the
            # process is running so the later exit-code check can read the actual result.
            # https://github.com/PowerShell/PowerShell/issues/5421
            $null = $presentMonProcess.Handle
            Write-Host "PresentMon launched with PID=$($presentMonProcess.Id), session=$PresentMonSessionName"
        }

        $process = Start-Process -FilePath $ProcessPath -ArgumentList "--log-file `"$GodotLogFilePath`"" -PassThru -RedirectStandardOutput 'NUL' -RedirectStandardError '\\.\NUL'
        $process.PriorityClass = [System.Diagnostics.ProcessPriorityClass]::High
        Write-Host "Launched with PID=$($process.Id)"

        Wait-Process -Id $process.Id

        if ($null -ne $presentMonProcess) {
            Start-Sleep -Seconds 5
            if (-not $presentMonProcess.HasExited) {
                Write-Host "Stopping PresentMon session $PresentMonSessionName"
                & $PresentMonPath --terminate_existing_session --session_name $PresentMonSessionName
                if ($LASTEXITCODE -ne 0) {
                    throw "PresentMon shutdown failed with exit code $LASTEXITCODE."
                }
            }
            $presentMonProcess.WaitForExit()
            if ($presentMonProcess.ExitCode -ne 0) {
                throw "PresentMon exited with code $($presentMonProcess.ExitCode). See PresentMonOutput_Error.log."
            }
            Write-Host 'PresentMon exited; capture output flushed.'
        }
    } | Out-Default
} finally {
    Stop-Transcript
}
