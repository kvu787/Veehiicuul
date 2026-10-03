# Godot And Unity Orthonormalization

## User (Repository Instructions and Environment)

````text
# AGENTS.md instructions for C:\Users\k\Repository\Veehiicuul

<INSTRUCTIONS>
# Base template

## Development platform compatibility

Support Windows 11 x64 as the only development platform.

## Folder and file naming

This only applies to things that we have the freedom to name as wanted.
Use CamelCase.
Use complete proper words. Don't use typical shortenings. Good: Source, Documentation. Bad: src, docs.

## External tools

You may use the tools in `%UserProfile%\Program`.
You may refer to local copies of source repos in `%UserProfile%\Repository\External`.

## Git

When implementing stuff, avoid difficult-to-review "mega-commits".
Split large work into multiple commits to make it easier to review.
Separate commits that record conversations from other commits.

## Markdown tables

Tables in Markdown must be padded and aligned in a way to make them easy to read in a plaintext editor, not only in a Markdown viewer.

## Mathematical notation in Markdown

Any mathematical notation in Markdown files (LaTeX, KaTeX, MathJax, etc) must display properly in VSCode's Markdown previewer, GitHub.com's Markdown displayer, and the markdown viewer in the Windows 11 ChatGPT app.

## PowerShell

All PowerShell scripts must use:

- Set-StrictMode -Version Latest
- $ErrorActionPreference = 'Stop'

## Godot

When creating a Godot application:

- Use Godot 4.7.2 .NET
- Use C#
- Don't use GDScript
- Halt if you don't find a portable/self-contained install of Godot 4.7.2 .NET at `%UserProfile%\Program\Godot_v4.7.2-stable_mono_win64`
- Halt if that install of Godot doesn't have export templates installed
- Build.cmd must do all building/exporting using release configuration with optimizations fully enabled and use Godot's export via the command-line to create an EXE
- Use DirectX 12
- Keep vsync off
- Keep max fps limiter off
- Set rendering_device/vsync/swapchain_image_count=2
- Set rendering_device/fallback_to_vulkan=false
- Set rendering_device/fallback_to_opengl3=false
- Use Forward+ renderer

The Godot csproj must include this:

```xml
<PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>

    <!-- Enables nullable reference annotations and warnings to catch potential null errors. -->
    <Nullable>enable</Nullable>

    <!-- Enforces the repository's configured code-style rules during builds. -->
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>

    <!--
        Required for IDE0005 to work.
        Enables the build-time IDE0005 check for unused using directives by generating XML documentation.
    -->
    <GenerateDocumentationFile>true</GenerateDocumentationFile>

    <!--
        Required in Godot .NET/C# projects.
        Prepares the C# library and its dependencies for dynamic loading by Godot.
    -->
    <EnableDynamicLoading>true</EnableDynamicLoading>

    <!--
        Required to properly export a Godot .NET/C# project using the command-line (instead of the Godot Editor export GUI).
        Prevents an idle compiler server from keeping Godot's Windows console wrapper waiting after export.
        See https://github.com/godotengine/godot/issues/110101 for more information.
    -->
    <UseSharedCompilation>false</UseSharedCompilation>
<PropertyGroup>
```

## Applications

### Running

If you create a runnable application, create files called `Build.cmd` and `Run.cmd` that respectively build and run the application when double-clicked from File Explorer.
These must be located at the root of the application's folder in the Git repo.
These must be simple wrappers for PowerShell scripts named `Build.ps1` and `Run.ps1` which contain the actual logic to minimize the amount of batch code written.
Run.cmd must exit if it doesn't discover a build of the application at the place that Build.cmd outputs to.
If the application doesn't need to be "built" for it to be run (such as a PowerShell script), then omit Build.cmd and Build.ps1.

### Logging

When creating an application, create a folder called `MyLogOutput` at the root of the application's folder in the git repo.
For each run of the application, a folder must be created in MyLogOutput and named with the current timestamp. This PowerShell code shows what the name of the folder should be:

```powershell
$logFolderPath = "$env:UserProfile\Repository\Godot\VsyncStutterTest\MyLogOutput\$(Get-Date -Format "yyyy-MM-dd_HH-mm-ss")"
New-Item -ItemType "Directory" -Path $logFolderPath
```

Any logs for that application session must be put in that log folder.
`MyLogOutput/` must be gitignored.

# Base template additions

## Conversations

Record verbatim and commit all conversations in a folder named `Conversations` located at the root of this Git repo.
Use one file per conversation.
Prefix these commits with `[cnv]`.
If I attach images to prompts, save and record these in the conversation logs.
If the conversation begins with `dnr`, then do not record the conversation.

## Application compatibility

Do not attempt to maintain any sort of application compatibility between different commits of the repo. This creates unwanted complexity.

## Target platform compatibility

Support Windows 11 x64 as the only target platform.
</INSTRUCTIONS><environment_context>
  <cwd>C:\Users\k\Repository\Veehiicuul</cwd>
  <shell>powershell</shell>
  <current_date>2026-09-28</current_date>
  <timezone>America/Los_Angeles</timezone>
  <filesystem><workspace_roots><root>C:\Users\k\Repository\Veehiicuul</root><root>C:\Users\k\.codex\visualizations\2026\09\28\01a0e734-8cc1-7d52-91e6-11f325a49d22</root></workspace_roots><permission_profile type="managed"><file_system type="restricted"><entry access="read"><special>:root</special></entry><entry access="write"><path>C:\Users\k\Repository\Veehiicuul</path></entry><entry access="write"><path>C:\Users\k\.codex\visualizations\2026\09\28\01a0e734-8cc1-7d52-91e6-11f325a49d22</path></entry><entry access="write"><special>:slash_tmp</special></entry><entry access="write"><special>:tmpdir</special></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.git</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\09\28\01a0e734-8cc1-7d52-91e6-11f325a49d22\.git</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.agents</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\09\28\01a0e734-8cc1-7d52-91e6-11f325a49d22\.agents</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.codex</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\09\28\01a0e734-8cc1-7d52-91e6-11f325a49d22\.codex</path></entry></file_system></permission_profile></filesystem>
</environment_context>
````

## User

in godot engine compared to unity engine, explain when i'm supposed to orthonormalize things. is orthonormalization required in unity? irc, unity has a different transform representation that doesn't require this. explain all of this.

## Assistant Commentary

I’ll check the Godot and Unity documentation and source to explain where orthonormalization is needed and how their transform representations differ.

## Assistant Commentary

Your recollection is broadly right: Unity stores rotation as a quaternion, while Godot stores its basis as a matrix. Godot’s rotation helpers can accumulate matrix drift. The key distinction is whether the basis is supposed to represent only rotation—orthonormalizing it also removes any intended scale and shear.

## Assistant Commentary

Godot 4.7.2’s source confirms that its `Quaternion` property converts to and from the stored basis; it does not switch the node to quaternion storage. You can keep a quaternion in your own C# field and rebuild the node’s rotation from it. Unity avoids the matrix maintenance step, but quaternions you repeatedly multiply yourself still need normalization.

## Assistant

**Your recollection is broadly correct: ordinary Unity `Transform` usage does not require you to orthonormalize a matrix. Godot can require that when you repeatedly modify a basis intended to represent rotation.** However, quaternions also need numerical maintenance when you accumulate rotations yourself.

An object’s 3D basis consists of three vectors describing its X, Y, and Z axes. **Orthonormal** means:

- Each axis has length 1.
- Every pair of axes is perpendicular.

A pure rotation preserves both properties. Scale changes the lengths; shear changes the angles between axes. Consequently, a basis that is not orthonormal can be completely valid—it may intentionally contain scale or shear. [Godot’s Basis documentation](https://docs.godotengine.org/en/4.7/classes/class_basis.html)

Orthonormalization adjusts those vectors to restore unit lengths and perpendicularity. Normalizing each vector individually only fixes the lengths; it does not fix their angles. Godot uses Gram–Schmidt, which keeps the first axis’s direction and adjusts subsequent axes relative to it. See your local [Godot implementation](C:/Users/k/Repository/External/Godot_4-7-2/core/math/basis.cpp:56).

The engines represent transforms differently:

| Aspect                  | Godot `Node3D`                                        | Unity `Transform`                                  |
| ----------------------- | ----------------------------------------------------- | -------------------------------------------------- |
| Local transform         | Position plus a 3×3 basis, with cached rotation/scale | Separate position, quaternion rotation, and scale  |
| Rotation representation | Embedded in the basis; quaternion access converts it  | Stored as a quaternion                             |
| Scale and shear         | Can be embedded directly in the basis                 | Scale is separate; local shear is not exposed      |
| Matrix usage            | The basis is directly accessible and editable         | Matrices are derived for transformations           |

Godot explicitly documents this storage arrangement in its [Node3D source](C:/Users/k/Repository/External/Godot_4-7-2/scene/3d/node_3d.h:69). Unity documents its [quaternion rotation storage](https://docs.unity3d.com/6000.0/Documentation/Manual/QuaternionAndEulerRotationsInUnity.html), and its [Transform API source](https://github.com/Unity-Technologies/UnityCsReference/blob/master/Runtime/Transform/ScriptBindings/Transform.bindings.cs) exposes separate properties and read-only transformation matrices.

**In Godot, the main reason to orthonormalize is accumulated matrix error.** Suppose you repeatedly multiply the previous frame’s basis by a small rotation. Mathematically, pure rotations stay pure rotations. Floating-point arithmetic introduces small errors, however, so the axes can gradually change length and cease to be perpendicular. The error is carried into the next frame.

This includes Godot’s `Rotate()`, `RotateY()`, and `RotateObjectLocal()` helpers: they modify the existing basis and do not automatically orthonormalize it. I checked this in your [Godot 4.7.2 source](C:/Users/k/Repository/External/Godot_4-7-2/scene/3d/node_3d.cpp:1153).

For an unscaled node whose basis should contain only rotation, this is a reasonable pattern:

```csharp
// Inside a Godot Node3D.
// This node is intended to have unit scale and no shear.
RotateY(AngularSpeed * (float)delta);
Orthonormalize();
```

Calling it once after the frame’s rotation updates is straightforward. Periodic correction is also possible; there is no universal interval at which drift becomes significant. Godot’s documentation explicitly suggests periodic or per-frame correction. **A stationary node does not accumulate this error merely because frames pass.** [Godot’s precision guidance](https://docs.godotengine.org/en/4.7/tutorials/3d/using_transforms.html#precision-errors)

These are the practical distinctions:

| What your code does                                    | Appropriate action                                                    |
| ------------------------------------------------------ | --------------------------------------------------------------------- |
| Repeatedly rotates an existing, unscaled basis         | Orthonormalize periodically or after each update                      |
| Rebuilds rotation from maintained angles each frame    | Usually no extra orthonormalization; old matrix errors do not persist |
| Rebuilds rotation from a maintained unit quaternion    | Maintain quaternion normalization; no extra basis correction needed   |
| Changes only position                                  | No orthonormalization needed                                          |
| Uses deliberate scale or shear                         | Preserve it; do not blindly orthonormalize the whole transform        |
| Constructs a coordinate frame from custom axis vectors | Ensure the resulting axes satisfy the requirements of the algorithm   |

**Scale is the major trap.** Orthonormalizing a basis with scale `(2, 3, 4)` reduces its axis lengths to 1. It also removes shear. It cannot distinguish intended deformation from floating-point drift.

For gameplay objects, a convenient arrangement is an unscaled orientation/controller node with a scaled visual child. Alternatively, keep the intended scale separately and reconstruct the basis from rotation and scale. Repeatedly extracting scale from a drifting matrix can preserve the very scale error you wanted to eliminate. Godot also recommends scaling visual children when manipulating the parent’s rotation. [Godot’s transform guidance](https://docs.godotengine.org/en/4.7/tutorials/3d/using_transforms.html#precision-errors)

**You can use a quaternion as your authoritative rotation state in Godot**, much like Unity does internally:

```csharp
private Godot.Quaternion Orientation = Godot.Quaternion.Identity;

public override void _Ready()
{
    Orientation = Quaternion.Normalized();
}

public override void _Process(double delta)
{
    var rotationStep = new Godot.Quaternion(
        Vector3.Up,
        AngularSpeed * (float)delta);

    // Post-multiplication applies this step in the object's local space.
    Orientation = (Orientation * rotationStep).Normalized();
    Quaternion = Orientation;
}
```

This assumes your script owns the node’s rotation and `AngularSpeed` is in radians per second. Each assignment reconstructs the basis’s rotation while preserving the node’s separate scale value. You do not need an additional `Orthonormalize()` call. The quaternion getter and setter still convert through Godot’s basis internally; keeping `Orientation` yourself avoids using that matrix conversion as your persistent rotation state. [Godot’s quaternion setter](C:/Users/k/Repository/External/Godot_4-7-2/scene/3d/node_3d.cpp:321)

**Unity replaces this matrix-maintenance concern with the simpler requirement that rotation quaternions remain normalized.** A unit quaternion represents a rotation, so the rotation matrix derived from it has perpendicular unit axes, within floating-point precision. Scale is applied separately.

For ordinary `Transform` usage, you do not need to periodically orthonormalize its axes. But if you maintain and repeatedly multiply your own quaternion variable, normalize that variable:

```csharp
// Unity: rotationStep is a valid rotation quaternion.
orientation = (orientation * rotationStep).normalized;
transform.localRotation = orientation;
```

Unity explicitly expects normalized quaternions. Its quaternion multiplication operator does **not** normalize the result itself, so “Unity uses quaternions” does not mean arbitrary quaternion arithmetic is immune to drift. [Unity’s quaternion contract](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Quaternion.html), [multiplication implementation](https://github.com/Unity-Technologies/UnityCsReference/blob/master/Runtime/Export/Math/Quaternion.cs)

If you implement your own matrix accumulation or construct custom coordinate frames in Unity, the same orthonormalization considerations apply. Unity even provides [`Vector3.OrthoNormalize`](https://docs.unity3d.com/ScriptReference/Vector3.OrthoNormalize.html).

A few related details matter when deciding whether to “fix” a transform:

- **World transforms can contain shear in either engine.** A rotated child under a nonuniformly scaled parent can acquire a sheared world matrix. That is a consequence of the hierarchy, even with perfectly valid rotations. Orthonormalizing it changes the intended transformation. Unity’s [`lossyScale` documentation](https://docs.unity3d.com/ScriptReference/Transform-lossyScale.html) describes this limitation.
- **Choose operations that support the transform you have.** Godot’s `Transform3D.Inverse()` assumes an orthonormal basis. With intended scale or shear, use `AffineInverse()` for an invertible transform; orthonormalizing first would change what you are inverting. [Transform3D documentation](https://docs.godotengine.org/en/4.7/classes/class_transform3d.html#class-transform3d-method-inverse)
- **Extracting rotation need not modify the node.** Godot’s `Basis.GetRotationQuaternion()` orthonormalizes a temporary copy internally. You do not need to erase the node’s scale first. For an arbitrarily sheared matrix, this produces a particular extracted rotation, not necessarily an original rotation you intended. [C# implementation](C:/Users/k/Repository/External/Godot_4-7-2/modules/mono/glue/GodotSharp/GodotSharp/Core/Basis.cs:552)
- **Correction has limits.** Orthonormalization cannot recover missing axes from a degenerate basis, undo accumulated orientation error, or fix gimbal lock. A mirrored orthonormal basis can also retain a reflection; a quaternion represents proper rotation only.

For your own Godot controllers, I would keep yaw/pitch values for constrained camera motion, or a normalized quaternion for unrestricted orientation, and write the resulting rotation to the node. Use basis orthonormalization when you deliberately choose to accumulate rotation directly in the basis.


## User

C:\Users\k\Repository\Veehiicuul\Veehiicuul_Godot_CSharp

in this game, the rotations are essentially 2d. for the rotations that are adjusted at game runtime, i only need to set Y rotation to non-zero value. rotation.XZ is always zero. given this, how should i set rotation

## Assistant Commentary

I’ll check how the game currently updates rotations and identify the simplest way to set yaw from your game state.

## Assistant Commentary

Your camera yaw already uses the right pattern: it stores an angle and assigns `(0, yaw, 0)`. The car currently converts its angle to a quaternion and then back to Euler angles. You can set the angle directly; with the current untransformed Ribeye track root, local yaw matches world yaw.

## Assistant

**Keep yaw as a single `float` in your game state, then assign `Rotation` or `RotationDegrees` directly with X and Z set to zero. You do not need a quaternion or orthonormalization for this.**

Your game already stores heading in **clockwise degrees**, which has the opposite sign to Godot’s positive Y rotation. Therefore:

```csharp
node.RotationDegrees = new Vector3(0f, -headingDegrees, 0f);
```

The equivalent using radians is:

```csharp
node.Rotation = new Vector3(
    0f,
    -Mathf.DegToRad(headingDegrees),
    0f);
```

Use the minus sign for your game’s heading convention; an angle already expressed in Godot’s convention needs no negation. `Rotation` uses radians, while `RotationDegrees` handles the conversion for you. [Godot’s rotation properties](https://docs.godotengine.org/en/4.7/classes/class_node3d.html#class-node3d-property-rotation)

For your current car, I would simplify [`ApplyStateToGameObject()`](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/GameDataAndLogic/Car/CarState.cs:90) to:

```csharp
public void ApplyStateToGameObject() {
    Node3D car = this.CarSwitcher.CurrentCarTransform;
    car.GlobalPosition = this.Position;
    car.RotationDegrees = new Vector3(0f, -this.Rotation, 0f);
}
```

That local rotation assignment fits your current hierarchy: player cars are direct children of the untransformed Ribeye track root, so local and world yaw coincide. If you instead need a world heading under a rotated parent, use `GlobalRotationDegrees`. These properties differ in coordinate space, not angle units.

Your current car code constructs a fresh yaw quaternion, and [`SetPositionAndRotation()`](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/Extensions/TransformExtensions.cs:16) immediately converts it back to Euler angles. Those conversions are unnecessary for your single-angle state.

**Why this avoids the orthonormalization issue:** setting local `Rotation` replaces the stored Euler angles. Godot then rebuilds the basis from those angles and the stored scale. You are not multiplying another rotation into the previous frame’s matrix, so its rotation-matrix errors do not accumulate. The local rotation setter also preserves your car’s scale. [Godot’s setter](C:/Users/k/Repository/External/Godot_4-7-2/scene/3d/node_3d.cpp:796), [basis reconstruction](C:/Users/k/Repository/External/Godot_4-7-2/scene/3d/node_3d.cpp:92)

Your [camera yaw update](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/GameDataAndLogic/Camera/CameraController.cs:50) already follows this approach. It can remain as written, or use the equivalent:

```csharp
this.CameraYawOffset.RotationDegrees =
    new Vector3(0f, -this.RotationOffset, 0f);
```

For continuous turning, accumulate changes in your own angle variable and assign the resulting absolute angle to the node. If that angle can grow indefinitely, wrap it periodically to a bounded range to retain floating-point precision. With only Y rotation, Euler rotation order and gimbal lock are not concerns.
