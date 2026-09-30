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

That is **“Ultra Low Latency — CPL State”**, the NVIDIA Control Panel’s bookkeeping setting:

- `390467` = hexadecimal **`0x0005F543`**
- Value **`2` = Ultra** (`0` = Off, `1` = On)
- `Dword` means a 32-bit integer.
- The blank `SettingNameInfo` means the export supplied no readable name.

It records the Control Panel selection; the separate **`0x10835000`** flag controls Ultra Low Latency enablement. [Profile Inspector definitions](https://github.com/Orbmu2k/nvidiaProfileInspector/blob/master/nvidiaProfileInspector/CustomSettingNames.xml)
