# SimplePaint Shader Understanding

A native C# / WPF application for Windows 11 x64. Compare the original SimplePaint shader with the proposed linear-warp squared-facing model using one shared material. Both spheres, both plots, and the numerical inspector update together.

The layout is sized for comfortable use on a 27-inch 2560x1440 monitor: 18-pixel main text, 16-pixel supporting labels, and controls at least 44 pixels tall at 100% Windows scaling. WPF also honors Windows display scaling. The app starts maximized with the standard Windows title bar. Its restored window size is 2200x1320. Parameter and explanation panels scroll when needed in a smaller window.

The graphs have more height by default. Drag the grip between the graphs and the bottom details pane to adjust their share of the lower area. Drag it all the way down to fully hide the details pane, including its tabs. The grip remains at the bottom; drag it up to restore the pane at your preferred height with the same selected tab. After focusing the divider, Up and Down also resize, hide, and restore the pane. The sphere previews keep their height during these adjustments, and material parameters and the inspected normal stay unchanged.

## Build and run

Requires PowerShell 7 (`pwsh`) and the .NET 10 SDK for Windows x64. The application uses WPF and the .NET libraries; it has no third-party package dependencies.

1. Double-click `Build.cmd`. This publishes an optimized Release build to a new directory under `BuildOutput/Releases`, then atomically updates `BuildOutput/CurrentBuild.txt` to select it. A failed build leaves the previous selection intact.
2. Double-click `Run.cmd`. It copies the selected completed build into that session's `LogOutput/yyyy-MM-dd_HH-mm-ss/Application` directory and runs the copy. If no build is selected or its executable is missing, it exits with an error and asks you to build first.

You can keep using an open app while development and rebuilding continue. Every launch has its own binaries, and builds never overwrite published binaries. A launch during a build uses the last completed build; after publication, new launches use the new build. Existing windows keep their version and parameters until you close them.

The build is framework dependent and needs the .NET 10 Windows Desktop Runtime, which is included with the SDK. This application is intended to be built and run inside this repository.

Each application run creates `LogOutput/yyyy-MM-dd_HH-mm-ss`. The folder contains the private application copy, launch information, application errors, and the final material parameters. Build transcripts, verification images, and verification results also go under `LogOutput`. Build products and logs are ignored by Git. Keep an open session's folder until that window closes; afterwards its files can be removed together with its logs.

## Explore the comparison

- Change a slider, or enter an exact value and press Enter. Tab also commits a value; Escape restores it. Invalid values leave the current material unchanged and mark the entry. Hover a parameter label for its meaning.
- Both sphere previews always show the front hemisphere. Click or drag either sphere to inspect the same camera-space normal in both models. The white ring marks a front-facing probe. Arrow keys move it after focusing a sphere; Home selects the center. Inspecting a back normal through the facing graph updates the graphs and readout while the sphere previews continue to show the front.
- Switch **Display** between **Painted color**, **Facing f**, and **Remapped tone t**. Paint receives sRGB encoding; facing and tone map directly to grayscale display intensity.
- The facing plot follows the inspected normal: it sweeps tilt while keeping the probe's offset perpendicular to the material's Rotation plane. Both colored markers stay on their curves as you move the probe on either sphere. Click or drag the plot to inspect another normal in the same slice. Shaded regions represent the back hemisphere. A probe outside the visible angle range retains its actual facing values at the nearest edge, with an arrow and an "outside view" caption.
- Use the facing graph's **Back range** slider to choose how much of the back hemisphere is visible on each side. **0°** shows only **-90° to 90°**, removing both shaded regions; **90°** restores **-180° to 180°**. Intermediate settings zoom the graph symmetrically, including its curves and click positions. This is a graph setting and does not change the material.
- Use **Probe peak** to inspect the exact maximum direction, including peaks narrower than one preview pixel.
- The **Shared color response** plot defaults to **Facing f to RGB**: its horizontal input is facing, and it includes `t = DarkPoint*(1-f) + LightPoint*f`. Changing either endpoint changes the curves. The left and right endpoints show the actual colors at facing 0 and 1, including reversed or constant tone ranges.
- Switch that plot to **Tone t to RGB** to inspect the underlying curve `F(t)` over the full tone interval. This curve depends on Base Color and Brightness; the shaded interval shows the tones selected by Dark Point and Light Point. These endpoints change the selected interval, rather than the underlying curve's shape.
- Hover the RGB curve to read its input, tone, and linear output. Its gray dashed line marks the base-color anchor when reachable; teal and amber dotted lines mark the inspected normal's facing or tone according to the selected axis. If the base-color tone lies outside the selected range, the facing view reports "base absent". Equal endpoints at the base-color tone instead give that color everywhere.
- Read **Shader equations** and **Smoothness and cost** for the mathematical steps and shader implementation implications. **Probe values** lists the intermediate and final values at the inspected normal. The panels scroll at smaller window sizes.
- **Save parameters** and **Load parameters** round-trip the complete material as JSON. **Export PNG** captures the current comparison after the latest preview finishes.

**Parameter presets** includes **Zero Shift: z versus z squared**, **Silhouette transition**, **Reversed tone endpoints**, and **Constant tone**. **Scene sphere material** and **Scene body material** reproduce the materials in the C++ scene's `Assets/Settings.json` at the source revision below.

## Shared controls

Startup, **Reset**, and **Default parameters** use **Dark Point = 0.3** and **Light Point = 0.9**. The scene material presets use the C++ scene's endpoints, 0 and 1.

The ranges follow the existing module's numerical contract. Let `m = 1/1024 = 0.0009765625` and `M = 1-m = 0.9990234375`.

| Control           | Range       | Meaning                                                                     |
| ----------------- | ----------- | --------------------------------------------------------------------------- |
| Base color R/G/B  | `[m, M]`    | An sRGB color decoded to linear RGB before constructing the channel curves. |
| Brightness        | `[m, M]`    | Places the base-color anchor at tone `1-Brightness`.                        |
| Shift             | `[0, M]`    | Sideways component of the front maximum's unit normal.                      |
| Rotation          | `[0, 360)`  | Counterclockwise screen angle: 0 right, 90 up, 180 left, 270 down.          |
| Dark Point        | `[0, M]`    | Input tone at facing zero.                                                  |
| Light Point       | `[m, 1]`    | Input tone at facing one.                                                   |

Equal Dark/Light points give a constant color. Reversed points invert the tone progression. Brightness is an anchor control, not an RGB multiplier. Rotation has no shading effect at zero Shift.

## The two models

Coordinates are camera relative: +X right, +Y up, +Z toward the camera. Rotate the input normal's XY components by negative Rotation first. The sphere preview supplies analytic unit normals in an orthographic view; the evaluator also accepts unnormalized normals.

For a rotated **unit** normal `(x,y,z)` and `s = Shift`, the original real-number expression is:

```text
h = sqrt((1-s)*(1+s))
r = sqrt(x*x + z*z)
f = r*z*h / (r-s*x)    when z > 0
f = 0                 when z <= 0
```

The app evaluates the stable, nonnegative light/dark weights `P,Q` translated from `SimplePaintCore.hlsli`, rather than computing `Q = 1-f`. It also follows `Material.cpp` when composing and scaling the color coefficients. This preserves the construction used by the original shader near its highlight and slice poles.

For the proposal, the rotated normal does **not** need normalization:

```hlsl
// CPU: precompute h = sqrt((1-s)*(1+s)).
// Vertex shader: apply after the existing XY rotation.
float3 w = float3(h*n.x - s*n.z, h*n.y, n.z);

// Pixel shader: use the interpolated w.
float P = w.z*w.z;
float Q = dot(w.xy, w.xy);
```

Both models conceptually produce `f = P/(P+Q)`. The entire proposed transform is linear, so it commutes with interpolation when the triangle has a constant material. Its inverse exists for `s < 1`; therefore `P+Q` is positive for every nonzero normal.

Both front maxima occur at `(s,0,h)` in the rotated frame, and both have `f = 0` on the silhouette. They differ elsewhere: at zero Shift, the original front facing is `z`, while the proposal gives `z*z`. The proposal has an antipodal maximum on the back instead of a constant back hemisphere.

## From facing to color

Facing first chooses a tone. Each linear base-color channel then supplies its own anchored rational curve:

```text
t = DarkPoint*(1-f) + LightPoint*f

c = decoded linear base-color channel
b = Brightness
A = c*b
B = (1-c)*(1-b)
F(t) = A*t / (A*t + B*(1-t))

F(0) = 0
F(1) = 1
F(1-b) = c
```

For the actual shader, combine the tone remap and curve on the CPU:

```text
ND = A*DarkPoint          NL = A*LightPoint
CD = B*(1-DarkPoint)      CL = B*(1-LightPoint)

numerator  = ND*Q + NL*P
complement = CD*Q + CL*P
linear RGB = numerator / (numerator + complement)
```

The division that would construct `f` cancels. Consequently the proposed pixel-facing calculation needs only squares and an addition; it does not need a square root, normalization, division, or branch. The final RGB rational curve still has channel divisions. The original facing code uses one square root for zero Shift and two for positive Shift, in addition to its other arithmetic. The inspector displays `f` and `t` to explain the computation even though a production shader need not materialize them.

The absolute magnitudes of `P,Q` differ between models and are not brightness values. Their common scale cancels from the output. Compare facing, tone, and final colors instead.

## Smoothness and limits

For fixed valid parameters, the proposed facing and linear RGB are infinitely differentiable functions of every nonzero input normal. Their denominators are positive, and the warp is invertible. The original is smooth inside the front hemisphere, but generally has a derivative discontinuity where it meets its constant back hemisphere. A constant tone remap removes that shading variation altogether.

The smoothness claim concerns the real-number functions. It excludes the piecewise sRGB display encoding, finite precision, pixel coverage, and discontinuous surface normals. This app evaluates CPU double-precision mathematics and maps it to 8-bit previews. It is not a GPU benchmark or a bit-for-bit binary32 emulator. Preview images are 448 by 448 samples, so extreme settings can produce a peak too small to hit a pixel. Probe the exact peak to see its mathematical value.

No GPU speedup is inferred from this application's responsiveness. Production use still needs the C++ module's numerical tests, compiled shader inspection, and profiling of the intended workload.

## Verification and source

Run both test suites and produce offscreen PNG captures:

```powershell
pwsh -NoProfile -File .\Build.ps1 -Verify
```

Verify an existing build without rebuilding:

```powershell
pwsh -NoProfile -File .\Run.ps1 -VerifyInterface
```

Verify launching and rebuilding concurrently, including Windows file locks and missing or failed builds:

```powershell
pwsh -NoProfile -File .\Tests\LaunchIsolationTests.ps1
```

The launch isolation checks exercise `Run.cmd`, hold session and published assemblies open against replacement, rebuild while launching another offscreen instance, and confirm that the next launch selects the new completed build. They also verify that missing builds cannot launch and that a failed build preserves the previous selection. This suite requires an existing completed build and publishes another build during its concurrency check.

Verification uses WPF software rendering into images and never shows a window. It constructs the window before starting the dispatcher, matching the initial-render timing of a normal launch. Mathematical tests compare the stable original against an independent rational expression over 30,000 seeded samples, and check shared peaks, scale invariance, interpolation, silhouette and back behavior, endpoint remapping, and invalid inputs. Interface tests exercise real control handlers, exact entry, cancellation of obsolete renders, pixel correspondence, guided explorations, parameter serialization, minimum-window layout, startup status completion, and tooltip contrast and wrapping. Completed preview durations are recorded in each session's `Application.log`.

Source inspected at repository commit `2a7ae01`:

- [Original specification](../3dTestScene_CppDx12/Source/SimplePaint/Specification.md)
- [Original HLSL](../3dTestScene_CppDx12/Source/SimplePaint/SimplePaintCore.hlsli)
- [Original material compilation](../3dTestScene_CppDx12/Source/SimplePaint/Material.cpp)
- [Scene material settings](../3dTestScene_CppDx12/Assets/Settings.json)
- [Mathematical evaluator](Source/ShaderMathematics/PaintMathematics.cs)
- [Independent mathematical checks](Tests/ShaderMathematicsTests/Program.cs)
- [Offscreen interface verification](Source/InterfaceVerification.cs)
- [Launch isolation verification](Tests/LaunchIsolationTests.ps1)

Original facing-lobe and anchored color-curve mathematical construction: Kevin Vu. The linear-warp model implements the proposal from the associated SimplePaint analysis conversation. The original C++ shader is unchanged by this app.
