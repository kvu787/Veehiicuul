# Microsoft GameInput

Unmodified files from Microsoft's [Microsoft.GameInput 3.5.283 NuGet package](https://www.nuget.org/packages/Microsoft.GameInput/3.5.283).

Package URL:
<https://api.nuget.org/v3-flatcontainer/microsoft.gameinput/3.5.283/microsoft.gameinput.3.5.283.nupkg>

Package SHA-256:
`B5988CB8FF9D7009B6DDF6AD4E3FF87E91B00CC17FFCD5D628DABC21C208F100`

- `Include/GameInput.h`: the v3 public header from `native/include/GameInput.h`.
- `Library/GameInput.lib`: the x64 static loader from `native/lib/x64/GameInput.lib`, preserved for SDK completeness. The application explicitly loads its local runtime instead of linking this version-selecting loader.
- `Runtime/GameInputRedist.dll`: the full x64 runtime, version 3.5.283.0, extracted from the MSI's embedded cabinet. The small system compatibility shim is excluded.
- `Runtime/GameInputRawInputProxy.exe` and `Runtime/GameInputBridge.dll`: the accompanying x64 runtime components.
- `LICENSE.txt` and `NOTICE.txt`: the original package's terms and notices, unchanged.

The public header and static loader include Microsoft's MIT license. Runtime components are provided under the accompanying Microsoft redistributable license. Runtime components remain unmodified.

`Tools/ImportGameInput.ps1` refreshes these files from an already downloaded and expanded package. It reads the MSI database and expands its embedded cabinet; it does not install the MSI or register services. Ordinary builds use the checked-in files and need no network connection, NuGet client, or SDK installation.
