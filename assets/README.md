# Generated render assets

`SceneBackground.png` is a flattened image of the original gray background,
green ground, and red cube. It is baked at a 32:9 aspect ratio so the renderer
can center-crop it without changing the camera's vertical scale. Wider windows
use a centered 32:9 scene viewport with matching side mattes. At 16:9, the
center half of the image maps one-to-one to 2560x1440.

`Settings.ini` exposes independent SimplePaint controls and a base color
for each car material and the sphere. `FacingCutoff` is shared in
`[SimplePaintShader_GlobalParameters]`; `[Sphere]` controls mesh resolution. The normal build copies both
files beside the executable. Edit the source INI and relaunch through `Run.cmd`
to apply changes.

Regenerate the background and `src/generated/CarMesh.generated.h` with Blender
4.5.12 LTS from the repository root:

```powershell
& "$env:USERPROFILE\Program\blender-4.5.12-windows-x64\blender.exe" `
    --background --factory-startup --disable-autoexec `
    ".\Blender\Car.blend" `
    --python-exit-code 1 `
    --python ".\tools\GenerateAssets.py" `
    -- `
    --car-output ".\src\generated\CarMesh.generated.h" `
    --background-output ".\assets\SceneBackground.png"
```

The generated mesh uses Blender's evaluated triangle loops and corner normals,
then converts coordinates from Blender Z-up to renderer Y-up with
`(x, y, z) -> (x, z, -y)`.
