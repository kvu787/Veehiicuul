# Exception handling verification

This standalone .NET program tests the built application's exception handler in
three child processes. It checks that a thrown exception stops the process before
its catch block runs, that an explicit fatal stop exits immediately, and that a
failure while writing the fatal log still stops the process. Every child must
exit with a failure status and leave no continuation marker.

From the Godot application's folder:

```powershell
dotnet run --project Verification/ExceptionHandling/ExceptionHandlingVerification.csproj --configuration Debug
```

Logs and probe markers go under `MyLogOutput/yyyy-MM-dd_HH-mm-ss/ExceptionHandling`.
All cases install the first-chance handler with a precreated directory, so these
tests need no running Godot engine.

The verifier references the application assembly and invokes its internal handler
through reflection. It does not change application code, execute `Main._Ready`,
or bypass the intentional startup exception. It is excluded from the Godot build.
