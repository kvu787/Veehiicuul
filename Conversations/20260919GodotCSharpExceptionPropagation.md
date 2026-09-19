# Conversation: Godot CSharp Exception Propagation

## User

in godot 4.7.2 .net, explain what happens when a c# exception bubbles up to the outermost layer without being caught

## Assistant

I’ll check Godot’s C# exception handling to explain what gets logged and whether the application keeps running.

## Assistant

Godot 4.7.2 catches ordinary exceptions that escape callbacks such as `_Process` and `_Ready`, logs them, and lets the game continue. An exception that escapes a separate .NET thread can instead terminate the process.

## Assistant

**For ordinary C# exceptions escaping `_Process`, `_PhysicsProcess`, `_Ready`, or similar Godot callbacks, Godot 4.7.2 normally logs the exception and keeps the game running.** Your code has no catch, but Godot has one at the boundary where the engine calls C#.

I verified this against your local Godot source checkout tagged `4.7.2-stable`: [CSharpInstanceBridge.cs](/C:/Users/k/Repository/External/Godot_4-7-2/modules/mono/glue/GodotSharp/GodotSharp/Core/Bridge/CSharpInstanceBridge.cs:39).

What happens, step by step:

1. The exception unwinds the C# call stack. Remaining ordinary statements in the affected calls are skipped; applicable `finally` blocks and `using` cleanup execute.
2. Godot’s C# bridge catches the exception.
3. It reports the exception and stack trace to Godot’s debugger when active, otherwise through `GD.PushError`. See [ExceptionUtils.cs](/C:/Users/k/Repository/External/Godot_4-7-2/modules/mono/glue/GodotSharp/GodotSharp/Core/NativeInterop/ExceptionUtils.cs:111).
4. Control returns to the engine. **That callback invocation has ended, but the game loop continues.**

For example:

```csharp
public override void _Process(double delta)
{
    GD.Print("Before");
    throw new InvalidOperationException("Something failed");
    // Any code here is skipped.
}
```

Assuming processing remains enabled, Godot calls `_Process` again next frame. This example therefore prints and reports an exception every frame. An exception in `_Ready` aborts that invocation; Godot does not automatically retry initialization.

**Nothing rolls back the changes made before the exception.** The node can remain partially initialized or in an inconsistent state even though the application is still running. An attached C# debugger may also pause execution, depending on its exception settings.

The result differs outside those callbacks:

- **A truly unhandled exception on a separate .NET thread:** normally terminates the whole application process, not just that thread. Godot’s callback catch does not protect arbitrary threads. [Microsoft documentation](https://learn.microsoft.com/en-us/dotnet/standard/threading/exceptions-in-managed-threads)
- **An exception inside a `Task`, including `Task.Run`:** normally faults the task and is stored in it. Awaiting the task rethrows the exception at the `await`; what happens next depends on who catches it. Merely faulting a task does not normally terminate the application. [Microsoft documentation](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task.exception?view=net-10.0)

So, **escaping all of your own catches does not necessarily mean crashing: the crucial question is whether the exception reaches Godot’s catch or actually escapes all exception handling.**

