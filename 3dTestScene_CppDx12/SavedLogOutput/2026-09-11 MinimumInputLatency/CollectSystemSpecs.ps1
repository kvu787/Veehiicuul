# Collect system and build context without changing machine configuration.
# Run explicitly to refresh; AnalyzePresentMon.py never refreshes this snapshot.
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$analysisDirectory = $PSScriptRoot
$repositoryDirectory = (Resolve-Path (Join-Path $analysisDirectory '..\..')).Path
function Read-CimData {
    param([string]$ClassName, [string[]]$Properties, [string]$Namespace = 'root/cimv2')
    try { @(Get-CimInstance -Namespace $Namespace -ClassName $ClassName | Select-Object -Property $Properties) }
    catch { @{ Unavailable = $_.Exception.Message } }
}
function Read-RegistryData {
    param([string]$Path, [string[]]$Properties)
    try { Get-ItemProperty -LiteralPath $Path | Select-Object -Property $Properties }
    catch { @{ Unavailable = $_.Exception.Message } }
}
$inventory = [ordered]@{
    CollectedAt = (Get-Date).ToString('o')
    TimeZone = (Get-TimeZone).Id
    Provenance = 'Post-capture inventory of the local computer. This is not capture-time telemetry.'
    Computer = Read-CimData Win32_ComputerSystem @('Manufacturer','Model','SystemType','TotalPhysicalMemory','NumberOfProcessors','NumberOfLogicalProcessors','HypervisorPresent')
    Processor = Read-CimData Win32_Processor @('Name','Manufacturer','NumberOfCores','NumberOfLogicalProcessors','MaxClockSpeed','CurrentClockSpeed','L2CacheSize','L3CacheSize','SocketDesignation','AddressWidth')
    BaseBoard = Read-CimData Win32_BaseBoard @('Manufacturer','Product','Version')
    Bios = Read-CimData Win32_BIOS @('Manufacturer','SMBIOSBIOSVersion','ReleaseDate','SMBIOSMajorVersion','SMBIOSMinorVersion')
    Memory = Read-CimData Win32_PhysicalMemory @('Manufacturer','PartNumber','Capacity','Speed','ConfiguredClockSpeed','DeviceLocator','BankLabel','SMBIOSMemoryType','ConfiguredVoltage')
    OperatingSystem = Read-CimData Win32_OperatingSystem @('Caption','Version','BuildNumber','OSArchitecture','LastBootUpTime','TotalVisibleMemorySize')
    WindowsVersion = Read-RegistryData 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion' @('ProductName','DisplayVersion','CurrentBuild','UBR','BuildLabEx','EditionID')
    Graphics = Read-CimData Win32_VideoController @('Name','VideoProcessor','DriverVersion','DriverDate','AdapterRAM','CurrentHorizontalResolution','CurrentVerticalResolution','CurrentRefreshRate','CurrentBitsPerPixel','PNPDeviceID')
    Storage = Read-CimData Win32_DiskDrive @('Model','Size','InterfaceType','MediaType','FirmwareRevision')
    PhysicalDisks = Read-CimData MSFT_PhysicalDisk @('FriendlyName','MediaType','BusType','Size','FirmwareVersion') 'root/Microsoft/Windows/Storage'
    MonitorIdentity = Read-CimData WmiMonitorID @('Active','ManufacturerName','ProductCodeID','UserFriendlyName','WeekOfManufacture','YearOfManufacture') 'root/wmi'
    GraphicsScheduling = Read-RegistryData 'HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers' @('HwSchMode')
    GameBar = Read-RegistryData 'HKCU:\Software\Microsoft\GameBar' @('AutoGameModeEnabled','AllowAutoGameMode')
    GameDvr = Read-RegistryData 'HKCU:\System\GameConfigStore' @('GameDVR_Enabled','GameDVR_FSEBehaviorMode')
    DeviceGuard = Read-CimData Win32_DeviceGuard @('VirtualizationBasedSecurityStatus','SecurityServicesConfigured','SecurityServicesRunning') 'root/Microsoft/Windows/DeviceGuard'
    ActivePowerScheme = @(& powercfg.exe /getactivescheme)
    ProcessorPowerSettings = @(& powercfg.exe /query SCHEME_CURRENT SUB_PROCESSOR)
    GameProcess = @(Get-Process -Name Veehiicuul -ErrorAction SilentlyContinue | Select-Object Id,StartTime,Path,MainWindowTitle,PriorityClass,ProcessorAffinity)
    RepositoryCommit = (& git -C $repositoryDirectory rev-parse HEAD)
    RepositoryStatus = @(& git -C $repositoryDirectory status --short)
}
$inventory | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $analysisDirectory 'SystemSpecs.json') -Encoding utf8
$evidence = Join-Path $analysisDirectory 'Context'
New-Item -ItemType Directory -Path $evidence -Force | Out-Null
$sourceFiles = @(
    'MyLogOutput\2026-09-11_13-59-05\Application.log',
    'MyLogOutput\2026-09-11_13-59-05\Launcher.log',
    'MyBuildOutput\Release\Assets\Settings.json',
    'MyBuildOutput\Release\CMakeCache.txt',
    'MyBuildOutput\Release\build.ninja'
)
$manifest = @()
foreach ($relativePath in $sourceFiles) {
    $sourcePath = Join-Path $repositoryDirectory $relativePath
    if (Test-Path -LiteralPath $sourcePath) {
        $item = Get-Item -LiteralPath $sourcePath
        Copy-Item -LiteralPath $sourcePath -Destination (Join-Path $evidence $item.Name)
        $manifest += [ordered]@{ Source=$relativePath; Snapshot=('Context/' + $item.Name); Length=$item.Length; LastWriteTime=$item.LastWriteTime.ToString('o'); Sha256=(Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash }
    }
}
foreach ($sourcePath in @((Join-Path $repositoryDirectory 'MyBuildOutput\Release\Veehiicuul.exe'), (Join-Path $env:USERPROFILE 'Program\PresentMon-2.5.1-x64.exe'))) {
    if (Test-Path -LiteralPath $sourcePath) {
        $item = Get-Item -LiteralPath $sourcePath
        $manifest += [ordered]@{ Source=$sourcePath; Length=$item.Length; LastWriteTime=$item.LastWriteTime.ToString('o'); Sha256=(Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash; FileVersion=$item.VersionInfo.FileVersion; ProductVersion=$item.VersionInfo.ProductVersion }
    }
}
$manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $analysisDirectory 'ContextManifest.json') -Encoding utf8
$diagnosticPath = Join-Path $analysisDirectory 'DirectXDiagnostics.txt'
$diagnosticProcess = Start-Process -FilePath "$env:WINDIR\System32\dxdiag.exe" -ArgumentList @('/whql:off','/t',('"' + $diagnosticPath + '"')) -WindowStyle Hidden -PassThru
$diagnosticProcess.WaitForExit()
Write-Output "Saved SystemSpecs.json, DirectXDiagnostics.txt, ContextManifest.json, and context snapshots."
