# PresentMon and Godot log correlation conversation

## User

what is the best way to correlate a presentmon 2.6.0 capture with a gd.print log statement from a godot 4.7.2 game

## Assistant Commentary

I'll check PresentMon's timestamp fields and Godot's logging behavior, then work out a reliable way to align the two.

## Assistant Commentary

The best fit is a shared QPC timestamp: PresentMon 2.6.0 can write raw Windows performance-counter values, and C# can put the same clock value into `GD.Print`. That aligns the log event with the capture, though identifying the exact frame that shows its effect requires accounting for Godot's render pipeline.

## Assistant Final

**Use the same Windows QPC clock in both files:** capture PresentMon with `--qpc_time`, and include `Stopwatch.GetTimestamp()` in your `GD.Print` statements. On Windows, that C# method calls `QueryPerformanceCounter` directly, so the two timestamps share a clock even if capture and game start at different times. [.NET implementation](https://github.com/dotnet/runtime/blob/v10.0.0/src/libraries/System.Private.CoreLib/src/System/Diagnostics/Stopwatch.Windows.cs).

1. **Enable raw QPC timestamps in PresentMon:**

   ```text
   C:\Users\k\Program\PresentMon-2.6.0-x64.exe --process_name Veehiicuul.exe --qpc_time --output_file PresentMon.csv
   ```

   This produces a `CPUStartQPC` column containing raw counter ticks. [PresentMon 2.6.0 documentation](https://github.com/GameTechDev/PresentMon/blob/v2.6.0/README-ConsoleApplication.md).

2. **Timestamp the event inside the game, before printing:**

   ```csharp
   using System.Diagnostics;
   using Godot;

   // Print once at startup.
   GD.Print($"Clock ProcessId={System.Environment.ProcessId} QpcFrequency={Stopwatch.Frequency}");

   // At the event you want to correlate.
   long eventQpc = Stopwatch.GetTimestamp();
   GD.Print($"QPC={eventQpc} Event=CollisionDetected");
   ```

   The embedded timestamp identifies when that code ran, regardless of when the log line reaches the console or file.

3. **Put the marker on the capture timeline.** Filter the CSV to the logged process ID and relevant swapchain, then compare `eventQpc` with the surrounding `CPUStartQPC` values. To express both relative to the first captured row:

   ```text
   eventTimeMs = (eventQpc - firstCpuStartQpc) * 1000.0 / qpcFrequency
   ```

   Apply the same calculation to each row's `CPUStartQPC`. Preserve raw timestamps as 64-bit integers and subtract before converting to floating point.

**This gives precise time correlation, but does not establish which displayed frame contains the event's effect.** Godot's processing and rendering can occur at different points in the pipeline, so inspect neighboring frames when diagnosing a hitch or visual change.

For performance captures, use occasional markers; frequent `GD.Print` calls can disturb the timing you are measuring. If you need many events, record their timestamps in memory and write them afterward.

`Time.GetTicksUsec()` can also measure elapsed time, but it starts at Godot startup and needs an offset calibration to align with PresentMon. Raw QPC avoids that extra step. [Godot Time documentation](https://docs.godotengine.org/en/stable/classes/class_time.html#class-time-method-get-ticks-usec).

