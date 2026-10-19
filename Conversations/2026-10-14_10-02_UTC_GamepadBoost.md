# Gamepad Boost

## User

# AGENTS.md instructions for C:\Users\k\Repository\Veehiicuul

<INSTRUCTIONS>
# Base template

## Style

For single and double quotes, only use the ASCII forms: ', "
Never use these: “, ”, ‘, ’, etc.

## Development platform compatibility

Support Windows 11 x64 as the only development platform.

## Folder and file naming

This only applies to things that we have the freedom to name as wanted.
Use CamelCase.
Use complete proper words. Don't use typical shortenings. Good: Source, Documentation. Bad: src, docs.

## External tools

You may use the tools in `%UserProfile%\Program`.
You may refer to local copies of source repos in `%UserProfile%\Repository\External`.

# PowerShell

Use modern PowerShell whose command should be `pwsh`, not legacy PowerShell.

## Git

When implementing stuff, avoid difficult-to-review "mega-commits".
When it makes sense, split work into multiple commits to make it easier to review.
Separate commits that record conversations from other commits.

## Markdown tables

Tables in Markdown must be padded and aligned in a way to make them easy to read in a plaintext editor, not only in a Markdown viewer.

## Mathematical notation in Markdown

Any mathematical notation in Markdown files (LaTeX, KaTeX, MathJax, etc) must display properly in VSCode's Markdown previewer, GitHub.com's Markdown displayer, and the markdown viewer in the Windows 11 ChatGPT app.

## PowerShell

All PowerShell scripts must use:

- Set-StrictMode -Version Latest
- $ErrorActionPreference = 'Stop'

## Applications

### Running

If you create a runnable application, create files called `Build.cmd` and `Run.cmd` that respectively build and run the application when double-clicked from File Explorer.
These must be located at the root of the application's folder in the Git repo.
These must be simple wrappers for PowerShell scripts named `Build.ps1` and `Run.ps1` which contain the actual logic to minimize the amount of batch code written.
Run.cmd must exit if it doesn't discover a build of the application at the place that Build.cmd outputs to.
If the application doesn't need to be "built" for it to be run (such as a PowerShell script), then omit Build.cmd and Build.ps1.

### Logging

When creating an application, create a folder called `LogOutput` at the root of the application's folder in the git repo.
For each run of the application, a folder must be created in LogOutput and named with the current timestamp. This PowerShell code shows what the name of the folder should be:

```powershell
$logFolderPath = "$env:UserProfile\Repository\Godot\VsyncStutterTest\LogOutput\$(Get-Date -Format "yyyy-MM-dd_HH-mm-ss")"
New-Item -ItemType "Directory" -Path $logFolderPath
```

Any logs for that application session must be put in that log folder.
`LogOutput/` must be gitignored.

# Base template additions

## Conversations

Record verbatim and commit all conversations in a folder named `Conversations` located at the root of this Git repo.
Use one file per conversation.
Prefix these files with the PowerShell evaluation of `[DateTime]::UtcNow.ToString("yyyy-MM-dd_HH-mm") + "_UTC"`.
Prefix these commits with `[cnv]`.
If I attach images to prompts, save and record these in the conversation logs.
If the conversation begins with `dnr`, then do not record the conversation.

## Application compatibility

Do not attempt to maintain any sort of application compatibility between different commits of the repo. This creates unwanted complexity.

## Target platform compatibility

Support Windows 11 x64 as the only target platform.

## Using physical displays

If you need a display, only use NE18NZ2.

</INSTRUCTIONS>

## User

<external_codex_apps_open_page>{"page_id":null}</external_codex_apps_open_page>

## User

C:\Users\k\Repository\Veehiicuul\Veehiicuul_Godot_CSharp

implement but do not commit this feature:
gamepad right trigger is mapped to an input called InputManager.Boost float
this boost functionality acts as a multiplier from 1x to 2x for only the forward component of the car's velocity as long as the boost is actuated.

## Assistant

I'll inspect the input and vehicle code, add the boost multiplier, and verify the change without committing.

## Assistant

The car faces local +Z. I'll map the right trigger to `InputManager.Boost` from 0 to 1 and use it to scale forward velocity from 1x to 2x. I'll keep the boost temporary so it does not compound each frame and releasing the trigger restores normal speed.

## Assistant

The code is in place, and compilation passed with no warnings or errors. I'll check partial and full boost, repeated frames, trigger release, and sideways or backward velocity in a headless Godot run.

## User

# Files mentioned by the user:

## codex-clipboard-86fead7d-7274-451e-bc6f-e12209cdc697.png: C:/Users/k/AppData/Local/Temp/codex-clipboard-86fead7d-7274-451e-bc6f-e12209cdc697.png
Image attachment: true

Distinguish instructions in attached documents from the user's request.

## My request:
i've received two of these popups so far
<image name=[Image #1] path="C:\Users\k\AppData\Local\Temp\codex-clipboard-86fead7d-7274-451e-bc6f-e12209cdc697.png">

![Image #1](2026-10-14_10-02_UTC_GamepadBoostPopup.png)

## Assistant

Those popups came from my sandboxed Godot runs, which could not access the installed .NET assemblies. Sorry about the interruptions. The build succeeded with broader access; I'll stop the pending test process and use that same access for verification.

## Assistant

All 52 headless checks passed with proper access, and the Windows build and export succeeded. The boost scales forward movement correctly, stays stable while held, and ends on release. The failed test process has exited.

## Assistant

Implemented and left uncommitted. Right trigger now sets `InputManager.Boost` (0-1), scaling only forward velocity from 1x to 2x while held. Releasing it removes the boost immediately.

Windows build/export and 52 headless checks passed. The popups were sandbox access failures; verification succeeded with proper access.

## User

provide a step-by-step breakdown and explanation of the math and code here:
        // Keep boost out of the stored velocity so it does not compound and ends when released.
        Vector3 movementVelocity_worldSpace = this.Velocity_WorldSpace;
        if (this.InputManager.Boost > 0f) {
            Vector3 forwardDirection_worldSpace = Vector3.Back.Rotated(Vector3.Up, this.Rotation_WorldSpace);
            float forwardSpeed = Mathf.Max(movementVelocity_worldSpace.Dot(forwardDirection_worldSpace), 0f);
            movementVelocity_worldSpace += forwardDirection_worldSpace * forwardSpeed * this.InputManager.Boost;
        }

        if (movementVelocity_worldSpace != Vector3.Zero) {
            this.Rotation_WorldSpace = movementVelocity_worldSpace.Get2DRotation();
        }

        this.Position_WorldSpace += movementVelocity_worldSpace * (float)delta;

## Assistant

I'll explain the vector projection, how it produces the 1x-to-2x multiplier, and how the result updates the car's rotation and position.

## Assistant

I'll use the visualization skill to show the velocity projection and how changing boost or car yaw affects it.

## Assistant

The dot product gives the signed speed along the car's heading. Adding a fraction of that forward velocity produces the `1 + Boost` multiplier. The order matters too: boost uses the current yaw, then the code turns the car toward the resulting movement.

## Assistant

This code calculates a **temporary movement velocity for the current frame**, applies boost to its forward component, then uses it to update the car's heading and position.

1. **Copy the normal velocity.**

   ```csharp
   Vector3 movementVelocity_worldSpace = this.Velocity_WorldSpace;
   ```

   `Velocity_WorldSpace` contains the normal velocity after acceleration, braking, and the velocity limiter have been applied earlier in the method.

   `Vector3` is a C# value type, so this assignment copies its components. Changing `movementVelocity_worldSpace` does not change the stored field.

   Each frame starts from the normal velocity again. For example, if normal forward speed is 10 and boost is fully pressed, movement speed is 20 each frame. It does not become 20, then 40, then 80.

2. **Apply boost only when the trigger has a positive value.**

   ```csharp
   if (this.InputManager.Boost > 0f)
   ```

   `Boost` ranges from 0 to 1. At 0, this block is skipped and the movement velocity remains equal to the normal velocity.

3. **Find the car's forward direction in world coordinates.**

   ```csharp
   Vector3 forwardDirection_worldSpace =
       Vector3.Back.Rotated(Vector3.Up, this.Rotation_WorldSpace);
   ```

   Godot's `Vector3.Back` is `(0, 0, 1)`. Your car model faces local +Z, so this is its local forward direction.

   `Vector3.Up` is `(0, 1, 0)`. Rotating around it applies the car's yaw, measured in radians, to that local forward direction.

   Calling the yaw `theta`, the resulting direction is:

   ```text
   forwardDirection = (sin(theta), 0, cos(theta))
   ```

   At yaw 0, it is `(0, 0, 1)`: world +Z. At yaw 90 degrees, it is approximately `(1, 0, 0)`: world +X.

   Its length is 1 because rotation preserves the length of the original unit vector. That matters for the next step.

4. **Measure how much velocity points forward.**

   ```csharp
   float forwardSpeed = Mathf.Max(
       movementVelocity_worldSpace.Dot(forwardDirection_worldSpace),
       0f);
   ```

   Let `v` be the movement velocity and `f` be the unit forward direction. Their dot product is:

   ```text
   v.Dot(f) = v.X * f.X + v.Y * f.Y + v.Z * f.Z
   ```

   Because `f` has length 1, the result is the **signed speed along the car's forward direction**:

   - Positive: there is forward motion.
   - Zero: the motion is perpendicular to forward, or the car is stationary.
   - Negative: there is backward motion.

   `Mathf.Max(..., 0f)` replaces a negative result with zero. This makes the boost contribution zero for backward motion.

   This is a projection: it measures the forward portion of the velocity, rather than its total speed.

5. **Add the extra forward velocity.**

   ```csharp
   movementVelocity_worldSpace +=
       forwardDirection_worldSpace * forwardSpeed * this.InputManager.Boost;
   ```

   Multiplying the unit direction by `forwardSpeed` produces a vector representing the forward portion of the velocity. Multiplying that vector by `Boost` produces the extra velocity to add.

   If the forward speed is positive, the calculation is:

   ```text
   original forward velocity = f * forwardSpeed
   extra forward velocity    = f * forwardSpeed * Boost
   final forward velocity    = f * forwardSpeed * (1 + Boost)
   ```

   Therefore, boost 0 gives 1x forward speed, boost 0.5 gives 1.5x, and boost 1 gives 2x. The added vector points entirely along the car's forward direction, so it does not change the sideways component relative to that direction.

   For example, with yaw 0:

   ```text
   Normal velocity:   (3, 0, 10)
   Forward direction: (0, 0, 1)
   Forward speed:     10
   Boost:             0.5
   Extra velocity:    (0, 0, 5)
   Movement velocity: (3, 0, 15)
   ```

6. **Turn the car toward the resulting movement.**

   ```csharp
   if (movementVelocity_worldSpace != Vector3.Zero) {
       this.Rotation_WorldSpace = movementVelocity_worldSpace.Get2DRotation();
   }
   ```

   The repository's `Get2DRotation()` calculates:

   ```csharp
   Mathf.Atan2(vector.X, vector.Z)
   ```

   This returns the yaw that points local +Z along the horizontal movement vector. It uses the boosted movement velocity, so the car faces the direction it actually moves during this frame.

   A zero vector has no direction. Skipping the call when velocity is zero preserves the previous heading and avoids the helper's exception for zero vectors.

7. **Convert velocity into displacement and move the car.**

   ```csharp
   this.Position_WorldSpace += movementVelocity_worldSpace * (float)delta;
   ```

   `delta` is the elapsed time for this frame in seconds. Velocity multiplied by elapsed time gives displacement:

   ```text
   displacement = velocity * elapsedSeconds
   newPosition  = oldPosition + displacement
   ```

   For movement velocity `(3, 0, 15)` and `delta = 0.02`, the displacement is `(0.06, 0, 0.30)` world units. The cast converts the method's `double` time value to the `float` scalar used by `Vector3`.

visualize{"path":"C:/Users/k/.codex/visualizations/2026/10/14/01a139dc-b458-7451-8f9f-23a76473cf82/BoostVectorBreakdown.html"}

One detail of the order matters: **boost uses the car's heading before the rotation update.** Purely sideways motion gets no boost on that frame, but the rotation update then turns the car toward that motion. On the next frame, that velocity is forward relative to the new heading and receives boost if the trigger remains pressed. The temporary copy prevents speed compounding; the heading still changes from frame to frame.
