# QuitOnException explanation

## User

explain the purpose of C:\Users\k\Repository\Veehiicuul\Veehiicuul_Godot_CSharp\Source\EngineIntegration\QuitOnException.cs and how it works

## Assistant Commentary

I’ll read the file and check how the application uses it so I can explain its purpose and behavior.

## Assistant

**`QuitOnException.cs` enforces the application’s “stop immediately when an exception occurs” policy.** Its purpose is to prevent Godot from logging a C# exception and then continuing to run.

In [QuitOnException.cs](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Source/EngineIntegration/QuitOnException.cs:17), the flow is:

1. **`Install(logDirectory)` enables monitoring.** It remembers where to write logs and subscribes to .NET’s `FirstChanceException` event. That event fires when an exception is thrown, before ordinary `catch` handling. Consequently, even an exception that a library intends to catch and recover from will terminate the application once this handler is installed.
2. **`OnExceptionThrown(...)` calls `Stop(exception)`.** `Stop` can also be called directly, which is how `Main` handles exceptions from its startup and frame callbacks.
3. **`Stop(...)` attempts to record the failure.** It uses `exception.ToString()` to obtain the exception details, including its stack trace, writes them to standard error, and writes `Fatal.log` in the session’s log directory. If `Install` has not supplied a directory, it obtains one from `SessionLog.CreateDirectory()`.
4. **`Terminate()` forcibly ends the current process.** It unsubscribes from the exception event, calls `Process.GetCurrentProcess().Kill()`, and uses `Environment.FailFast(...)` as a fallback if killing fails or the call returns before termination takes effect.

Two details make the failure path robust:

- **`Interlocked.Exchange(ref Stopping, 1)` prevents recursive reporting.** It atomically marks shutdown as started. If logging itself throws—or another thread reports an exception—the next `Stop` call immediately terminates instead of trying to log another failure.
- **The `finally` block attempts termination even if reporting fails.** Creating or writing `Fatal.log` is therefore best effort; a logging problem must not allow the game to continue.

The comments explain the forceful shutdown choice: `SceneTree.Quit()` waits for another frame, while `Environment.Exit()` can trigger a CLR shutdown assertion when Godot still owns native callbacks. Killing the process avoids those shutdown paths. It also means normal cleanup, pending saves, and other `finally` blocks are not guaranteed to run. `[DoesNotReturn]` tells the compiler and analysis tools that `Terminate()` never returns normally.

**In the current application, the global handler is never installed.** [Main._Ready](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Main.cs:25) deliberately throws before reaching `Install(...)`. Its surrounding `catch` instead calls `Stop(exception)` directly, which logs that startup exception and kills the app. Removing the deliberate throw would allow installation before game initialization.

This monitors managed C# exceptions after installation; it is not a general handler for every Godot error message or native crash.
