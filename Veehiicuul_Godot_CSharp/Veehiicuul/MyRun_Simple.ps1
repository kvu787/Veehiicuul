Set-StrictMode -Version "Latest"
$ErrorActionPreference = "Stop"

$ProcessPath = "$env:UserProfile\Repository\Veehiicuul\Veehiicuul_Godot_CSharp\Veehiicuul\MyBuildOutput\Veehiicuul_Godot_CSharp.exe"
$LogFolderPath = "$env:UserProfile\Repository\Veehiicuul\Veehiicuul_Godot_CSharp\Veehiicuul\MyLogOutput\$(Get-Date -Format "yyyy-MM-dd_HH-mm-ss")"
$GodotLogFilePath = "$logFolderPath\Godot.log"

New-Item -ItemType "Directory" -Path $LogFolderPath

$process = Start-Process -FilePath $ProcessPath -ArgumentList "--log-file `"$GodotLogFilePath`"" -PassThru
$process.PriorityClass = [System.Diagnostics.ProcessPriorityClass]::High
Write-Host "Launched with PID=$($process.Id)"

Wait-Process -Id $process.Id
Read-Host "Press ENTER to exit"
