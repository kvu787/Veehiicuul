# Digital input counts autoload

## User

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
  <current_date>2026-09-29</current_date>
  <timezone>America/Los_Angeles</timezone>
  <filesystem><workspace_roots><root>C:\Users\k\Repository\Veehiicuul</root><root>C:\Users\k\.codex\visualizations\2026\09\29\01a0ec1b-d716-7f72-b88d-9f7a4debea49</root></workspace_roots><permission_profile type="managed"><file_system type="restricted"><entry access="read"><special>:root</special></entry><entry access="write"><path>C:\Users\k\Repository\Veehiicuul</path></entry><entry access="write"><path>C:\Users\k\.codex\visualizations\2026\09\29\01a0ec1b-d716-7f72-b88d-9f7a4debea49</path></entry><entry access="write"><special>:slash_tmp</special></entry><entry access="write"><special>:tmpdir</special></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.git</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\09\29\01a0ec1b-d716-7f72-b88d-9f7a4debea49\.git</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.agents</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\09\29\01a0ec1b-d716-7f72-b88d-9f7a4debea49\.agents</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.codex</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\09\29\01a0ec1b-d716-7f72-b88d-9f7a4debea49\.codex</path></entry></file_system></permission_profile></filesystem>
</environment_context>

## User

C:\Users\k\Repository\Veehiicuul\Veehiicuul_Godot_CSharp\Veehiicuul

write an autoloaded class that collects a count of down events for every digital input (mouse, keyboard, gamepad, etc), makes those counts available via a public api that the game loop can query for a given mouse/keyboard/gamepad input code, and clears the counts at the end of every frame

## Assistant

I’ll check the Godot project and its input handling, then add an autoload with per-input press counts and end-of-frame clearing.

## Assistant

The project keeps frame processing in its main adapter. I’ll preserve that structure, count press events—including multiple presses within one frame—and provide queries for keyboard keys, mouse buttons, individual gamepads, and touch contacts. I’m treating keyboard auto-repeat as a held key, so it won’t add extra presses.

## Assistant

The collector is in place. It listens to the main window before UI handlers can consume events, and the main adapter clears the counts in a `finally` block after the game update. I’m checking compilation and testing event delivery and frame clearing in Godot.

## Assistant

The release build succeeds with three existing unused-field warnings in `InputManager`. The runtime test also caught Godot’s default touch-to-mouse emulation: a touch can produce a mouse press too. I’m separating those event types in the test so each count can be verified precisely.

## Assistant

The Godot runtime checks passed: multiple presses, repeat filtering, gamepad separation, touch contacts, handled input, and clearing after early returns and exceptions. I’m recording the implementation and the required conversation log in separate commits.

## Assistant

Added the [DigitalInputCounts autoload](/C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/EngineIntegration/DigitalInputCounts.cs). Query it inside `Main.Process`:

```csharp
var input = DigitalInputCounts.Instance;
int keys = input.GetPhysicalKeyDownCount(Key.Space);
int clicks = input.GetMouseButtonDownCount(MouseButton.Left);
int buttons = input.GetGamepadButtonDownCount(0, JoyButton.A);
```

Also supports logical keys, mouse wheels, and touch contacts. Reads preserve counts; they clear automatically after `Main.Process`, including early returns and exceptions. Keyboard auto-repeat is ignored.

Verified with 61 synthetic-event checks in Godot and a release build. Three existing `InputManager` warnings remain.

## User

```
c:\Users\k\Repository\Veehiicuul\Veehiicuul_Godot_CSharp\Veehiicuul\project.godot:20:1-20:75
Lines and columns are 1-based. Columns are StartInclusive:EndExclusive. Columns count UTF-16 code units.
```

```
DigitalInputCounts="*res://Source/EngineIntegration/DigitalInputCounts.cs"
```

what does that asterisk mean in the path?

## Assistant

I’ll check how Godot interprets that autoload entry and explain what the `*` changes.

## Assistant

The `*` marks the autoload as a **globally named singleton**. Godot removes it before resolving the actual resource path. [Godot’s parser](/C:/Users/k/Repository/External/Godot_4-7-2/core/config/project_settings.cpp:364)

- **With `*`:** Godot loads the node and exposes `DigitalInputCounts` as a global name in GDScript.
- **Without `*`:** Godot still loads the node at `/root/DigitalInputCounts`, but doesn’t expose that global name.

It corresponds to the **Enable** checkbox in the Autoload settings. That checkbox has no effect on C# access: our `DigitalInputCounts.Instance` property provides that access independently, so it works either way. [Godot documentation](https://docs.godotengine.org/en/stable/tutorials/scripting/singletons_autoload.html)

## User

if that checkbox has no effect on C# access, then why did you add the asterisk? is it recommended practice for things like this?

## Assistant

I’ll check whether Godot recommends that setting for C# autoloads or simply enables it by default.

## Assistant

I included it because **Godot enables that checkbox by default** when adding an autoload. I followed that convention; this implementation doesn’t require it. [Godot documentation](https://docs.godotengine.org/en/stable/tutorials/scripting/singletons_autoload.html)

It isn’t a C#-specific recommended practice. For our service, `*` provides no benefit because access comes from the static `Instance` property. Keeping it matches the editor’s default; removing it is equally valid and leaves the C# behavior unchanged.

## User

is there any difference between the approach that DigitaIInputCounts uses versus this approach which uses `public override void _Input(InputEvent inputEvent)`:
using Godot;
using System.Collections.Generic;

public partial class KeyboardInput : Node
{
    private readonly Dictionary<Key, int> _downCounts = new();

    public override void _Input(InputEvent inputEvent)
    {
        if (inputEvent is not InputEventKey keyEvent
            || !keyEvent.Pressed
            || keyEvent.Echo)
        {
            return;
        }

        Key key = keyEvent.PhysicalKeycode;

        if (key == Key.None)
            return;

        _downCounts.TryGetValue(key, out int count);
        _downCounts[key] = count + 1;
    }

    // Reading does not consume the count.
    public int DownCount(Key key)
    {
        return _downCounts.TryGetValue(key, out int count) ? count : 0;
    }

    public void EndFrame()
    {
        _downCounts.Clear();
    }
}

## Assistant

I’ll compare where the two handlers run in Godot’s input pipeline and when each can miss an event.

## Assistant

Yes. **The counting logic is equivalent for physical keyboard events; the main difference is which events reach the collector.** Both preserve multiple presses per frame, ignore keyboard repeats, and leave counts unchanged when queried.

`DigitalInputCounts` subscribes to the root window’s `WindowInput` signal. Godot emits that signal before passing the event into the viewport’s input handling. [Godot 4.7.2 source](/C:/Users/k/Repository/External/Godot_4-7-2/scene/main/window.cpp:2020)

| Situation                                         | Current `WindowInput` approach  | Your `_Input` approach                |
| ------------------------------------------------- | ------------------------------- | ------------------------------------- |
| Another node handles the event first              | Already recorded it             | Can miss it                           |
| Collector is paused by its process mode           | Signal handler still runs       | Callback stops                        |
| Event injected directly with `Viewport.PushInput` | Does not observe that injection | Can receive it in the target viewport |

The first difference matters for an autoload: `_Input` handlers run in reverse tree order, so scene nodes can run before the autoload. If one calls `SetInputAsHandled()`, remaining handlers do not receive that event. **Regular `_GuiInput` runs after `_Input`**, so ordinary GUI consumption alone does not prevent your example from counting it. [Input propagation documentation](https://docs.godotengine.org/en/stable/tutorials/inputs/inputevent.html)

The pause difference also affects the current implementation: the signal continues collecting while the main adapter is paused, but clearing waits until `Main.Process` runs again. [Pause behavior](https://docs.godotengine.org/en/stable/tutorials/scripting/pausing_games.html). Direct viewport injection follows the viewport’s callbacks rather than the window signal. [Viewport documentation](https://docs.godotengine.org/en/stable/classes/class_viewport.html#class-viewport-method-push-input)

Your `EndFrame()` serves the same purpose as our `ClearFrameCounts()`; either must be called after the game update. Ours already has that call in the adapter’s `finally` block. Supporting mouse, gamepad, and touch inputs is independent of the choice of callback.

I chose `WindowInput` so later scene handlers cannot hide presses from the counter. Your `_Input` version is suitable when that filtering is acceptable or you control all input handlers.

## User

explain what this means: Event injected directly with Viewport.PushInput

## Assistant

I’ll check what `Viewport.PushInput` does and show how it differs from a real keyboard event.

## Assistant

It means **your code manually sends an input event to a particular viewport**. For example, inside a `Node`:

```csharp
using InputEventKey press = new() {
    PhysicalKeycode = Key.Space,
    Pressed = true
};

GetViewport().PushInput(press);
```

This tells the viewport to process a Space-key down event, even though nobody physically pressed Space. It can be useful for automated tests, replaying recorded input, or forwarding input between viewports. [Godot documentation](https://docs.godotengine.org/en/stable/classes/class_viewport.html#class-viewport-method-push-input)

The relevant difference is where the event enters the system:

- **Normal keyboard input:** goes through the window’s `WindowInput` signal, then the viewport’s input handlers.
- **Calling `PushInput` directly:** starts at the viewport’s input handlers, bypassing `WindowInput`. [Godot’s dispatch code](/C:/Users/k/Repository/External/Godot_4-7-2/scene/main/window.cpp:2020)

Consequently, your `_Input` example can count that manually supplied Space press, provided the node receives it. Our current `DigitalInputCounts` would not count it because it listens at the earlier window stage.

## User

that seems like a useful feature. change DigitalInputCounts to use the _Input approach.

## Assistant

I’ll switch `DigitalInputCounts` to `_Input`, keep the existing API and frame clearing, and verify that it counts events sent through `Viewport.PushInput`.

## Assistant

The change passed all 78 Godot checks, including `PushInput`, pause behavior, and frame clearing. The release build also succeeds with the same three existing `InputManager` warnings. I’m committing the change and updated documentation.

## Assistant

Changed [DigitalInputCounts.cs](/C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/EngineIntegration/DigitalInputCounts.cs:61) to use `_Input`, including events injected through the root viewport’s `PushInput`. It now follows normal input consumption and pause behavior.

All 78 runtime checks passed. Release build succeeds with three existing `InputManager` warnings.

Committed as `a360727`.

## User

```
c:\Users\k\Repository\Veehiicuul\Veehiicuul_Godot_CSharp\Veehiicuul\Source\EngineIntegration\DigitalInputCounts.cs:61:44-61:50
Lines and columns are 1-based. Columns are StartInclusive:EndExclusive. Columns count UTF-16 code units.
```
```
@event
```
what does the `@` symbol do here?

## Assistant

I’ll check the declaration and explain that C# syntax.

## Assistant

`@` lets you use a **reserved C# keyword as a name**. Since `event` is a keyword, we write `@event`:

```csharp
public override void _Input(InputEvent @event)
```

This declares a parameter named `event`. The `@` is only an escape prefix in the source code—it is not part of the name and does not change the parameter’s value or behavior.

We use it here to match the parameter name in Godot’s base `_Input` method.

## User

are there any drawbacks or pitfalls to the _Input approach for DigitalInputCounts that i should be aware of

## Assistant

I’ll check the collector and frame-clearing code for pitfalls with `_Input`.

## Assistant

Yes. These are the main ones for the current implementation:

1. **Other input handlers can hide presses.** If an earlier `_Input` handler calls `SetInputAsHandled()`, the collector never receives that event. Autoloads commonly run after scene nodes because input travels in reverse tree order. I found no other custom `_Input` handlers in your current `Source` folder. [Godot input propagation](https://docs.godotengine.org/en/stable/tutorials/inputs/inputevent.html)

2. **UI interactions also count.** `_Input` runs before ordinary GUI handling. Clicking a button or typing into a text field can therefore increment gameplay input counts, even if the GUI subsequently consumes the event. Gameplay should decide whether to act on those counts when a menu or text field is active. [Godot input order](https://docs.godotengine.org/en/stable/tutorials/inputs/inputevent.html)

3. **The clearing boundary is specifically the end of `Main.Process`.** It is not the end of every callback in the entire Godot frame. Read the counts from that game update; another node processing afterward may see zero. Likewise, if you inject an event *after* the game has read its counts but *before* `Main.Process` returns, it can be cleared without gameplay using it. Inject before the relevant reads. [Current clearing code](/C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/Main_GodotAdapter.cs:14)

4. **Pausing needs a deliberate policy.** With the default process mode, the collector stops receiving input while the scene tree is paused. Setting only the collector to `Always` would allow collection, but counts would accumulate while the main adapter—and therefore clearing—remains paused. [Godot pause behavior](https://docs.godotengine.org/en/stable/tutorials/scripting/pausing_games.html)

5. **The autoload belongs to the root viewport.** Input delivered only to another `SubViewport` or window does not automatically reach it. For the injection feature you requested, push events into the root viewport. [Viewport routing](https://docs.godotengine.org/en/stable/tutorials/inputs/inputevent.html)

6. **It does not collect asynchronously while your game loop is blocked.** A long-running update delays input dispatch. Your [current main loop](/C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/Main.cs:66) contains a 100 ms sleep; using `_Input` does not remove that latency.
