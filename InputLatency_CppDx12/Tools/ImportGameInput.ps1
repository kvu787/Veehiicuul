param([Parameter(Mandatory)][string] $PackageDirectory)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Read the MSI database and its cabinet without running or installing the MSI.
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class GameInputPackageReader {
    [DllImport("msi.dll", CharSet = CharSet.Unicode)]
    public static extern uint MsiOpenDatabaseW(string path, IntPtr persist, out uint database);
    [DllImport("msi.dll", CharSet = CharSet.Unicode)]
    public static extern uint MsiDatabaseOpenViewW(uint database, string query, out uint view);
    [DllImport("msi.dll")] public static extern uint MsiViewExecute(uint view, uint record);
    [DllImport("msi.dll")] public static extern uint MsiViewFetch(uint view, out uint record);
    [DllImport("msi.dll")] public static extern uint MsiRecordReadStream(uint record, uint field, byte[] buffer, ref uint size);
    [DllImport("msi.dll")] public static extern uint MsiCloseHandle(uint handle);
}
'@

$applicationDirectory = Split-Path $PSScriptRoot
$destinationDirectory = Join-Path $applicationDirectory 'ThirdParty\GameInput'
$extractionDirectory = Join-Path $applicationDirectory 'BuildOutput\Dependencies\CabinetExtraction'
New-Item -ItemType Directory -Path $extractionDirectory -Force | Out-Null
$installerPath = Join-Path $PackageDirectory 'redist\GameInputRedist.msi'
$installer = New-Object -ComObject WindowsInstaller.Installer
$database = $installer.OpenDatabase($installerPath, 0)
$streamView = $database.OpenView('SELECT `Name` FROM `_Streams`')
$streamView.Execute()
$cabinetName = $null
while ($null -ne ($streamRecord = $streamView.Fetch())) {
    if ($streamRecord.StringData(1).EndsWith('.cab')) { $cabinetName = $streamRecord.StringData(1) }
}
if (-not $cabinetName) { throw 'No embedded cabinet was found.' }

[uint32] $databaseHandle = 0
[uint32] $viewHandle = 0
[uint32] $recordHandle = 0
$cabinetPath = Join-Path $extractionDirectory 'GameInput.cab'
$cabinetStream = [IO.File]::Create($cabinetPath)
try {
    if ([GameInputPackageReader]::MsiOpenDatabaseW($installerPath, [IntPtr]::Zero, [ref] $databaseHandle) -ne 0) { throw 'Cannot open MSI.' }
    $query = 'SELECT `Data` FROM `_Streams` WHERE `Name` = ''' + $cabinetName + ''''
    if ([GameInputPackageReader]::MsiDatabaseOpenViewW($databaseHandle, $query, [ref] $viewHandle) -ne 0) { throw 'Cannot open cabinet view.' }
    if ([GameInputPackageReader]::MsiViewExecute($viewHandle, 0) -ne 0) { throw 'Cannot execute cabinet view.' }
    if ([GameInputPackageReader]::MsiViewFetch($viewHandle, [ref] $recordHandle) -ne 0) { throw 'Cannot fetch cabinet.' }
    $buffer = [byte[]]::new(65536)
    do {
        [uint32] $byteCount = $buffer.Length
        if ([GameInputPackageReader]::MsiRecordReadStream($recordHandle, 1, $buffer, [ref] $byteCount) -ne 0) { throw 'Cannot read cabinet.' }
        $cabinetStream.Write($buffer, 0, [int] $byteCount)
    } while ($byteCount -ne 0)
}
finally {
    $cabinetStream.Dispose()
    foreach ($handle in @($recordHandle, $viewHandle, $databaseHandle)) {
        if ($handle -ne 0) { [GameInputPackageReader]::MsiCloseHandle($handle) | Out-Null }
    }
}
& "$env:SystemRoot\System32\expand.exe" $cabinetPath '-F:*' $extractionDirectory | Out-Host
if ($LASTEXITCODE -ne 0) { throw 'Cabinet extraction failed.' }

foreach ($directory in @('Include', 'Library', 'Runtime')) {
    New-Item -ItemType Directory -Path (Join-Path $destinationDirectory $directory) -Force | Out-Null
}
Copy-Item -LiteralPath (Join-Path $PackageDirectory 'native\include\GameInput.h') -Destination (Join-Path $destinationDirectory 'Include\GameInput.h')
Copy-Item -LiteralPath (Join-Path $PackageDirectory 'native\lib\x64\GameInput.lib') -Destination (Join-Path $destinationDirectory 'Library\GameInput.lib')
foreach ($name in @('LICENSE.txt', 'NOTICE.txt')) {
    Copy-Item -LiteralPath (Join-Path $PackageDirectory $name) -Destination (Join-Path $destinationDirectory $name)
}
$fileView = $database.OpenView('SELECT `File`, `FileName` FROM `File`')
$fileView.Execute()
while ($null -ne ($fileRecord = $fileView.Fetch())) {
    $fileName = $fileRecord.StringData(2).Split('|')[-1]
    if ($fileName -notin @('GameInputRedist.dll', 'GameInputRawInputProxy.exe', 'GameInputBridge.dll')) { continue }
    $filePath = Join-Path $extractionDirectory $fileRecord.StringData(1)
    $bytes = [IO.File]::ReadAllBytes($filePath)
    $peOffset = [BitConverter]::ToInt32($bytes, 60)
    $machine = [BitConverter]::ToUInt16($bytes, $peOffset + 4)
    if ($machine -eq 0x8664) {
        $runtimePath = Join-Path $destinationDirectory "Runtime\$fileName"
        # The MSI also contains a small compatibility shim with the same name.
        if ((Test-Path -LiteralPath $runtimePath) -and (Get-Item -LiteralPath $runtimePath).Length -ge $bytes.Length) { continue }
        Copy-Item -LiteralPath $filePath -Destination $runtimePath
        Write-Host "Staged unmodified x64 runtime: $fileName"
    }
}
