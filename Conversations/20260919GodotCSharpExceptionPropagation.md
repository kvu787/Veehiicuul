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

## User

explain happens to an uncaught exception in _Init, _Ready, and _Process

## Assistant

I’ll check initialization separately, because C# constructors and Godot callbacks can fail differently.

## Assistant

In **Godot 4.7.2 .NET**, the key difference is that a constructor exception prevents successful script creation, while exceptions in `_Ready` and `_Process` abort only the current callback.

First, **C# uses a constructor instead of GDScript’s `_init()`**. A C# method you name `_Init()` is not automatically an initialization callback. [Godot documentation](https://docs.godotengine.org/en/4.6/tutorials/best_practices/godot_notifications.html#init-vs-initialization-vs-export)

| Where it throws           | Immediate result                                                         | What happens afterward                                                          |
| ------------------------- | ------------------------------------------------------------------------ | ------------------------------------------------------------------------------- |
| Constructor (`_init`)      | Construction stops. When Godot creates the script, it catches and logs it. | Script creation fails; that failed script instance does not proceed to callbacks. |
| `_Ready()`                | The rest of that `_Ready` invocation is skipped; Godot logs the exception. | The node remains. `_Process` can still run, even with incomplete initialization.  |
| `_Process(double delta)`  | The rest of that frame’s invocation is skipped; Godot logs the exception.  | Godot calls it again next frame, assuming the node is still eligible to process.  |

**Constructor:** When Godot constructs a script—for example, while instantiating a scene—its managed bridge catches the exception and reports failure. The native engine then clears the failed script attachment. The underlying native node may remain, but it does not have that successfully constructed C# script. See [constructor handling](/C:/Users/k/Repository/External/Godot_4-7-2/modules/mono/glue/GodotSharp/GodotSharp/Core/Bridge/ScriptManagerBridge.cs:113) and [failure cleanup](/C:/Users/k/Repository/External/Godot_4-7-2/modules/mono/csharp_script.cpp:2401).

If your own C# code calls `new MyNode()`, the constructor exception instead propagates through your calling C# code. For example, if you call it inside `_Ready` without catching it, it also aborts that `_Ready` invocation before Godot catches it.

**`_Ready`:** Godot does not retry it automatically or disable the node because it threw. In fact, Godot enables an overridden `_Process` **before** calling `_Ready`. Consequently, failed setup in `_Ready` can produce further exceptions in `_Process` every frame. See [Node’s ready handling](/C:/Users/k/Repository/External/Godot_4-7-2/scene/main/node.cpp:255).

**`_Process`:** Each frame is a new invocation. An exception ends the current invocation; it does not resume at the failing line next frame. Execution starts from the beginning of `_Process` again. Other nodes and the game loop normally continue. See [Godot’s callback catch](/C:/Users/k/Repository/External/Godot_4-7-2/modules/mono/glue/GodotSharp/GodotSharp/Core/Bridge/CSharpInstanceBridge.cs:39).

In all three cases, applicable `finally`/`using` cleanup runs, but **changes already made are not rolled back**. This describes ordinary synchronous exceptions; `async` methods and fatal runtime failures need separate treatment.

## User

in a godot 4.7.2 game, is it possible for an exception to happen in code that i didn't write? what happens then?

## Assistant

I’ll check how this differs between C# libraries and Godot’s native engine code.

## Assistant

**Yes.** Exceptions can originate in .NET itself, Godot’s C# bindings, third-party libraries, or plugins—even if none of your code explicitly uses `throw`. For example, reading a missing file with `System.IO.File.ReadAllText` can throw `FileNotFoundException`. That can be an expected failure condition, not necessarily a library bug. [Microsoft documentation](https://learn.microsoft.com/en-us/dotnet/standard/exceptions/)

**What happens depends on where the exception travels, not who wrote the code.**

For example:

```csharp
public override void _Ready()
{
    string text = System.IO.File.ReadAllText("MissingFile.txt");
    GD.Print("Loaded");
}
```

If the file is missing:

1. The .NET library throws the exception.
2. It propagates out of `ReadAllText` and then out of your `_Ready`.
3. `GD.Print("Loaded")` never executes.
4. Godot’s C# bridge catches and logs the exception. The game normally continues, with that initialization unfinished.

The same rules from our previous discussion apply:

- **During `_Process`:** the current invocation ends; processing normally runs again next frame.
- **During Godot-driven C# construction:** script creation fails.
- **Inside a `Task`:** the task normally becomes faulted; awaiting it propagates the exception.
- **On a separate thread with no handler:** the whole game process normally terminates. A library’s background thread is not protected by Godot’s callback catch. [Microsoft threading documentation](https://learn.microsoft.com/en-us/dotnet/standard/threading/exceptions-in-managed-threads)

If the library catches the exception internally, its own handling determines the outcome; it might retry, return a failure result, or log it.

**Godot’s native C++ engine is a different case.** It generally does not use C++ exceptions; the standard build disables them. Many engine checks instead print an error and return early, sometimes returning an error code or a default value. Such an error message **does not automatically throw a C# exception**, and your C# code may continue after the engine call. See [Godot’s build configuration](/C:/Users/k/Repository/External/Godot_4-7-2/SConstruct:929) and [error macros](/C:/Users/k/Repository/External/Godot_4-7-2/core/error/error_macros.h:412).

A serious native failure—such as an invalid memory access or an explicit crash check—can terminate the game. **Godot’s C# exception handling is not a general shield against engine, native plugin, or driver crashes.**
