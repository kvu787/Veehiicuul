# Generated render assets

`SceneBackground.png` is a flattened image of the original gray background,
green ground, and red cube. It is baked at a 32:9 aspect ratio so the renderer
can center-crop it without changing the camera's vertical scale. Wider windows
use a centered 32:9 scene viewport with matching side mattes. At 16:9, the
center half of the image maps one-to-one to 2560x1440.

`Settings.json` exposes independent SimplePaint controls and a base color
for each car material and the sphere. See [Usage.md](../Source/SimplePaint/Usage.md) for the validated
parameter ranges; `Sphere` controls mesh resolution. The normal build copies both
files beside the executable. Edit the source JSON and relaunch through `Run.cmd`
to apply changes.

Regenerate the background and `Source/Generated/CarMesh.generated.h` with Blender
4.5.12 LTS from the `3dTestScene_CppDx12` directory. The shared Blender source remains one level
above it, in the repository's `Blender` directory:

```powershell
& "$env:USERPROFILE\Program\blender-4.5.12-windows-x64\blender.exe" `
    --background --factory-startup --disable-autoexec `
    "..\Blender\Car.blend" `
    --python-exit-code 1 `
    --python ".\Tools\GenerateAssets.py" `
    -- `
    --car-output ".\Source\Generated\CarMesh.generated.h" `
    --background-output ".\Assets\SceneBackground.png"
```

The generated mesh uses Blender's evaluated triangle loops and corner normals,
then converts coordinates from Blender Z-up to renderer Y-up with
`(x, y, z) -> (x, z, -y)`.
