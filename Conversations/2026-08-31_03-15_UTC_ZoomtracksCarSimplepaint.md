# ZoomTracks-style car and SimplePaint rendering

Date: 2026-08-30

Repository: `Simple_DirectX12_3D_Game`

## User

> let's try and get this sample a little closer to the ZoomTracks
> reimplementation i mentioned in another convo.
>
> first, change the moving sphere into a moving and rotating car. the model for
> the car is in this .blend file:
> `C:\Users\k\Repository\Simple_DirectX12_3D_Game\Blender\Car.blend`
>
> second, use the SimplePaintShader as the shader for the car. Refer to K12
> shader in
> https://github.com/kvu787/SimplePaintShaders/tree/main/Godot/ShaderTest/Shaders
> fully optimize the shader and do any fixes to it as you see fit expose the
> shader params in a way that make it easy for me to adjust
>
> third, change the rendering strategy so that the car is the only 3d rendered
> thing, and everything else (which i think is just the background and the
> static cube) is a 2d rendered image.

## Assistant

Replaced the procedural moving sphere with the Blender car, preserving its
corner normals and five material regions in a generated indexed mesh. The car
keeps the original back-and-forth movement and now continuously rotates.

Ported the K12 SimplePaint shader to HLSL. Camera- and material-invariant work
is precomputed on the CPU, the original slice/Schlick/remap path is reduced to
an equivalent one-square-root expression, and numerical endpoint protections
were added. All user-facing controls and per-material sRGB colors are exposed
in `assets/CarPaint.ini`.

Baked the gray clear color, green ground, and red cube into a 5120x1440 static
image. Runtime rendering is now one depth-disabled full-screen background draw
and one indexed 3D car draw. The background crop and car viewport stay aligned
across narrow, normal, and wider-than-32:9 windows.

Validated deterministic Blender regeneration, the generated mesh fingerprint,
Release and Debug builds, the DirectX 12 debug-layer launch, normal and
ultra-wide captures, and the double-click `Run.cmd` path. `Run.cmd` launched and
exited with code 0.

No images were attached to this conversation.
