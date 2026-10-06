Set-StrictMode -Version "Latest"
$ErrorActionPreference = "Stop"

$ProcessPath = "$env:UserProfile\Repository\Veehiicuul\Veehiicuul_Godot_CSharp\Veehiicuul\MyBuildOutput\Veehiicuul_Godot_CSharp.exe"
$ProcessName = Split-Path -Path $ProcessPath -Leaf
$PresentMonPath = "$env:UserProfile\Program\PresentMon-2.6.0-x64.exe"
$UsePresentMon = $args -notcontains 'NoPresentMon'

$LogFolderPath = "$env:UserProfile\Repository\Veehiicuul\Veehiicuul_Godot_CSharp\Veehiicuul\MyLogOutput\$(Get-Date -Format "yyyy-MM-dd_HH-mm-ss")"
$GodotLogFilePath = "$logFolderPath\Godot.log"
$PresentMonLogFilePath = "$LogFolderPath\PresentMon.csv"
$PresentMonSessionName = "$([System.IO.Path]::GetFileNameWithoutExtension($ProcessPath))_$([Guid]::NewGuid().ToString('N'))"

New-Item -ItemType "Directory" -Path $LogFolderPath

Start-Transcript -Path "$LogFolderPath\ScriptOutput.log"

try {
    . {
        Write-Host "ProcessPath = $($ProcessPath)"
        Write-Host "PresentMonPath = $($PresentMonPath)"

        if (-not (Test-Path $ProcessPath)) {
            throw "Invalid ProcessPath='$($ProcessPath)'"
        }

        if (@(Get-Process | Select-Object -ExpandProperty Path | Where-Object { $_ -and (Split-Path -Path $_ -Leaf) -eq $ProcessName }).Count -gt 0) {
            throw "Error: Multiple processes have the target process name of '$ProcessName'"
        }

        $presentMonProcess = $null
        if ($UsePresentMon) {
            Write-Host "PresentMon=On"

            if (-not (Test-Path $PresentMonPath)) {
                throw "Invalid PresentMonPath='$($PresentMonPath)'"
            }

            $identity = [System.Security.Principal.WindowsIdentity]::GetCurrent()
            $principal = [System.Security.Principal.WindowsPrincipal]::new($identity)
            $groupSid = [System.Security.Principal.SecurityIdentifier]::new('S-1-5-32-559') # S-1-5-32-559 is the stable identifier for the "Performance Log Users" group
            if (-not $principal.IsInRole($groupSid)) {
                throw "Trying to run PresentMon, but user '$($identity.Name)' must belong to the 'Performance Log Users' group."
            }

            # `--terminate_on_proc_exit` isn't used because I've observed it not stopping PresentMon even after the game exits.
            $presentMonProcess = Start-Process `
                -FilePath $PresentMonPath `
                -ArgumentList "--process_name `"$($ProcessName)`" --session_name `"$($PresentMonSessionName)`" --output_file `"$($PresentMonLogFilePath)`" --set_circular_buffer_size 65536 --no_console_stats --qpc_time --track_etw_status" `
                -WindowStyle "Hidden" `
                -PassThru `
                -RedirectStandardOutput "$LogFolderPath\PresentMonOutput_Standard.log" `
                -RedirectStandardError "$LogFolderPath\PresentMonOutput_Error.log"

            # Work around Windows PowerShell 5.1 returning a null ExitCode for redirected
            # Start-Process output, even after WaitForExit(). Retain the handle while the
            # process is running so the later exit-code check can read the actual result.
            # https://github.com/PowerShell/PowerShell/issues/5421
            $null = $presentMonProcess.Handle
            Write-Host "PresentMon launched with PID='$($presentMonProcess.Id)', session_name='$($PresentMonSessionName)'"
        } else {
            Write-Host "PresentMon=Off"
        }

        $process = Start-Process -FilePath $ProcessPath -ArgumentList "--log-file `"$GodotLogFilePath`"" -PassThru -RedirectStandardOutput 'NUL' -RedirectStandardError '\\.\NUL'
        $process.PriorityClass = [System.Diagnostics.ProcessPriorityClass]::High
        Write-Host "'$($ProcessPath)' launched with PID='$($process.Id)'"

        Wait-Process -Id $process.Id

        if ($null -ne $presentMonProcess) {
            Start-Sleep -Seconds 1
            if (-not $presentMonProcess.HasExited) {
                Write-Host "Stopping PresentMon session $PresentMonSessionName"
                & $PresentMonPath --terminate_existing_session --session_name $PresentMonSessionName
                if ($LASTEXITCODE -ne 0) {
                    throw "PresentMon shutdown failed with exit code $LASTEXITCODE."
                }
            }
            $presentMonProcess.WaitForExit()
            # Convert redirected output after PresentMon has finished writing, including failed runs.
            $utf8WithoutBom = [System.Text.UTF8Encoding]::new($false)
            foreach ($fileName in 'PresentMonOutput_Standard.log', 'PresentMonOutput_Error.log') {
                $filePath = Join-Path $LogFolderPath $fileName
                $content = [System.IO.File]::ReadAllText($filePath)
                [System.IO.File]::WriteAllText($filePath, $content, $utf8WithoutBom)
            }
            if ($presentMonProcess.ExitCode -ne 0) {
                throw "PresentMon exited with code $($presentMonProcess.ExitCode). See PresentMonOutput_Error.log."
            }
            Write-Host 'PresentMon exited; capture output flushed.'
        }
    } | Out-Default
} finally {
    Stop-Transcript
}
