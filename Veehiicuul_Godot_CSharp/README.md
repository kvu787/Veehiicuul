# MyRun capture

`MyRun.cmd` launches the existing `MyBuildOutput` executable and captures .NET GC
events from before application launch until capture shutdown after the application
exits. Windows asks for elevation for the hidden GC and PresentMon capture
helpers; the game keeps the launcher's usual permissions. Both helpers also stop
capture if the launcher exits unexpectedly.

Each timestamped folder in `Veehiicuul/MyLogOutput` contains:

- `GarbageCollection.etl`: the `Microsoft-Windows-DotNETRuntime` GC provider,
  keyword `0x1`, informational level `4`, with QPC timestamps.
- `GarbageCollectionCapture.log`: ETW start/stop diagnostics and the unique trace
  session name.
- `CaptureMetadata.json`: the game PID, QPC frequency, and GC capture configuration.
- `MyRun.log`: all launcher console output, including startup and shutdown errors.
- `Godot.log` and `Console.log`: the game's Godot log and combined console stdout
  and stderr.
- `PresentMon.csv`, `PresentMon.log`, and `PresentMonCapture.log`, when enabled:
  frame data, combined PresentMon stdout and stderr, and helper diagnostics.
- `GarbageCollectionReady.signal`, `GarbageCollectionStop.signal`, and
  `GarbageCollectionStopped.signal`: helper lifecycle markers. The stopped marker
  confirms that logman successfully stopped and flushed the trace.
- `PresentMonReady.signal`, `PresentMonStop.signal`, and `PresentMonStopped.signal`,
  when enabled: PresentMon helper lifecycle markers. The stopped marker confirms
  successful process exit and console output flush.

PresentMon uses `--qpc_time` and `--track_etw_status`. Five seconds after the game
exits, the launcher requests a real Ctrl+C event in PresentMon's private console
and waits for it to stop and flush. `MyRun_NoPresentMon.cmd` disables only
PresentMon; GC capture stays enabled. To disable GC capture for a comparison run:

```powershell
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File .\Veehiicuul\MyRun.ps1 NoGarbageCollection
```

Filter GC events to the game PID in `Godot.log` or `CaptureMetadata.json` because
the provider also records other .NET processes. Align raw GC event QPC values
with PresentMon using:

```text
Seconds = (GC event QPC - first PresentMon CPUStartQPC) / QpcFrequency
```

Measure GC-related runtime suspension/restart intervals, not the entire lifetime
of a background collection. Preserve the ETL for checking event loss before
concluding that an absent GC pause rules it out as the cause of a stutter.

# WIP Recommended system configuration

## Hardware

Lenovo Legion 9i
Asus ROG Strix G18

## Nvidia driver version

I have validated these Nvidia driver versions:
- 596.49
- 596.11
  - This driver version is specific to the Lenovo Legion 9i and isn't available through https://www.nvidia.com/en-us/drivers/

I have tried the later driver series of 610.x, 616.x, and 617.x.
They cause issues with my system such as:
- Night light causing a washed out cursor
- Inability to change laptop's built-in display brightness
- Laptop display not turning off when closing lid
However, I don't recall any issues with any games when using those driver series.

## G-SYNC Off

In Nvidia Control Panel, set these:
- Low Low Latency mode = Ultra

## G-SYNC On

todo

# Nvidia settings

Verify nvidia profile settings are as expected using Nvidia Profile Inspector, not the Nvidia App or Nvidia Control Panel.

It is possible for Nvidia App or NVCP to incorrectly show the state of the "Ultra Low Latency" mode setting.

Specifically, this UI setting is separate from the underlying actual setting:

```xml
<ProfileSetting>
  <SettingNameInfo> </SettingNameInfo>
  <SettingID>390467</SettingID>
  <SettingValue>2</SettingValue>
  <ValueType>Dword</ValueType>
</ProfileSetting>
```

That is **"Ultra Low Latency — CPL State"**, the NVIDIA Control Panel's bookkeeping setting:

- `390467` = hexadecimal **`0x0005F543`**
- Value **`2` = Ultra** (`0` = Off, `1` = On)
- `Dword` means a 32-bit integer.
- The blank `SettingNameInfo` means the export supplied no readable name.

It records the Control Panel selection; the separate **`0x10835000`** flag controls Ultra Low Latency enablement. [Profile Inspector definitions](https://github.com/Orbmu2k/nvidiaProfileInspector/blob/master/nvidiaProfileInspector/CustomSettingNames.xml)

This is an example of a well-formed profile:

```xml
<?xml version="1.0" encoding="utf-16"?>
<ArrayOfProfile>
  <Profile>
    <ProfileName>c:\users\k\repository\veehiicuul\veehiicuul_godot_csharp\veehiicuul\mybuildoutput\veehiicuul_godot_csharp.exe</ProfileName>
    <Executeables>
      <string>c:/users/k/repository/veehiicuul/veehiicuul_godot_csharp/veehiicuul/mybuildoutput/veehiicuul_godot_csharp.exe</string>
    </Executeables>
    <Settings>
      <ProfileSetting>
        <SettingNameInfo> </SettingNameInfo>
        <SettingID>390467</SettingID>
        <SettingValue>2</SettingValue>
        <ValueType>Dword</ValueType>
      </ProfileSetting>
      <ProfileSetting>
        <SettingNameInfo>Maximum pre-rendered frames</SettingNameInfo>
        <SettingID>8102046</SettingID>
        <SettingValue>1</SettingValue>
        <ValueType>Dword</ValueType>
      </ProfileSetting>
      <ProfileSetting>
        <SettingNameInfo>FRL Low Latency</SettingNameInfo>
        <SettingID>277041152</SettingID>
        <SettingValue>1</SettingValue>
        <ValueType>Dword</ValueType>
      </ProfileSetting>
    </Settings>
    <ExecutableFindFiles />
  </Profile>
</ArrayOfProfile>
```

This is an example of a misleading profile because the ULLM UI setting is 0 even though the underlying ULLM settings are enabled:

```xml
<?xml version="1.0" encoding="utf-16"?>
<ArrayOfProfile>
  <Profile>
    <ProfileName>VsyncStutterTest.exe</ProfileName>
    <Executeables>
      <string>c:/users/k/repository/vsyncstuttertest/mybuildoutput/vsyncstuttertest.exe</string>
    </Executeables>
    <Settings>
      <ProfileSetting>
        <SettingNameInfo> </SettingNameInfo>
        <SettingID>390467</SettingID>
        <SettingValue>0</SettingValue>
        <ValueType>Dword</ValueType>
      </ProfileSetting>
      <ProfileSetting>
        <SettingNameInfo>Maximum pre-rendered frames</SettingNameInfo>
        <SettingID>8102046</SettingID>
        <SettingValue>1</SettingValue>
        <ValueType>Dword</ValueType>
      </ProfileSetting>
      <ProfileSetting>
        <SettingNameInfo>FRL Low Latency</SettingNameInfo>
        <SettingID>277041152</SettingID>
        <SettingValue>1</SettingValue>
        <ValueType>Dword</ValueType>
      </ProfileSetting>
    </Settings>
    <ExecutableFindFiles />
  </Profile>
</ArrayOfProfile>
```

# Generate DigitalInputMap.cs

```powershell
$ScriptPath = "C:\Users\k\Repository\Veehiicuul\Veehiicuul_Godot_CSharp\Tools\GenerateDigitalInputMap.py"
$OutputPath = "C:\Users\k\Repository\Veehiicuul\Veehiicuul_Godot_CSharp\Veehiicuul\Source\GameDataAndLogic\Input\DigitalInputMap.cs"

$source = py -3.14 $ScriptPath `
  "JoyButton.A" `
  "JoyButton.B" `
  "JoyButton.X" `
  "JoyButton.Y" `
  "JoyButton.DpadUp" `
  "JoyButton.DpadDown" `
  "JoyButton.DpadLeft" `
  "JoyButton.DpadRight" `
  "JoyButton.LeftShoulder" `
  "JoyButton.RightShoulder" `
  "JoyButton.LeftStick" `
  "JoyButton.RightStick" `
  "JoyButton.Back" `
  "JoyButton.Start" `
  "Key.Escape" `
  "MouseButton.Middle"
if ($LASTEXITCODE -ne 0) {
    throw "Generator failed."
}
[System.IO.File]::WriteAllText(
  $OutputPath,
  ($source -join "`n") + "`n",
  [System.Text.UTF8Encoding]::new($false)
)
```
