Set-StrictMode -Version "Latest"
$ErrorActionPreference = "Stop"

$ProcessName = "Veehiicuul_Godot_CSharp.exe"
$ProcessPath = "$env:UserProfile\Repository\Veehiicuul\Veehiicuul_Godot_CSharp\Veehiicuul\MyBuildOutput\Veehiicuul_Godot_CSharp.exe"
$PresentMonPath = "$env:UserProfile\Program\PresentMon-2.6.0-x64.exe"

$LogFolderPath = "$env:UserProfile\Repository\Veehiicuul\Veehiicuul_Godot_CSharp\Veehiicuul\MyLogOutput\$(Get-Date -Format "yyyy-MM-dd_HH-mm-ss")"
$GodotLogFilePath = "$logFolderPath\Godot.log"
$PresentMonLogFilePath = "$LogFolderPath\PresentMon.csv"

New-Item -ItemType "Directory" -Path $LogFolderPath

if (($args -notcontains "NoPresentMon") -and (Test-Path $PresentMonPath)) {
    # This PresentMon process must be manually closed by the user. This is intentional.
    # "--terminate_on_proc_exit" isn't used because I've observed issues with it.
    Start-Process `
        -FilePath $PresentMonPath `
        -ArgumentList "--process_name `"$($ProcessName)`" --output_file `"$($PresentMonLogFilePath)`" --set_circular_buffer_size 65536 --no_console_stats --qpc_time" `
        -Verb "RunAs"
}

$process = Start-Process -FilePath $ProcessPath -ArgumentList "--log-file `"$GodotLogFilePath`"" -PassThru
$process.PriorityClass = [System.Diagnostics.ProcessPriorityClass]::High
Write-Host "Launched with PID=$($process.Id)"

Wait-Process -Id $process.Id
