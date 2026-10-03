# System.Management package usage

## User

```
c:\Users\k\Repository\Veehiicuul\Veehiicuul_Godot_CSharp\Veehiicuul\Veehiicuul_Godot_CSharp.csproj:36:9-36:75
Lines and columns are 1-based. Columns are StartInclusive:EndExclusive. Columns count UTF-16 code units.
```
```
<PackageReference Include="System.Management" Version="10.0.12" />
```

is this used for anything

## Assistant

I’ll check the project for uses of `System.Management` and record this conversation as requested.

## Assistant

Yes—[DebugInfo.cs:273](/C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/DebugInfo.cs:273) uses it for WMI queries that collect CPU, motherboard, BIOS, memory, graphics, and storage information for the diagnostic report.

However, I found **no calls to `PrintSystemInfo_WindowsX64()`** in the project, so that report currently appears unused at runtime.

Removing the package alone would break compilation; you would also need to remove or replace the dependent code in `DebugInfo.cs`.
