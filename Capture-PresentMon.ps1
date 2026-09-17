Set-StrictMode -Version "Latest"
$ErrorActionPreference = "Stop"

$EXE_NAME = "Veehiicuul.exe"

$logFolderPath = "$env:UserProfile\Downloads\$(Get-Date -Format "yyyy-MM-dd_HH-mm-ss") $EXE_NAME logs"
New-Item -ItemType "Directory" -Path $logFolderPath

$presentMonPath = "$env:UserProfile\Program\PresentMon-2.5.1-x64.exe"
$presentMonLogFilePath  = "$($logFolderPath)\PresentMon.csv"
& $presentMonPath --process_name $EXE_NAME --output_file $presentMonLogFilePath
