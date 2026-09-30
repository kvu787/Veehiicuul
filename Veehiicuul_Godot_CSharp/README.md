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
