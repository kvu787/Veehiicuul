# Godot delta smoothing without vsync

## User

analyze the godot 4.7.2 source to see if `application/run/delta_smoothing` does anything if vsync is off

## Assistant

I’ll trace `delta_smoothing` through the local Godot source and check whether disabling vsync bypasses it.

## Assistant

**No—when Godot’s main-window vsync mode is disabled, `application/run/delta_smoothing` does not modify frame deltas.**

I checked the local source at tag `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`). In [`MainTimerSync::DeltaSmoother::smooth_delta()`](C:/Users/k/Repository/External/Godot_4-7-2/main/main_timer_sync.cpp:264), it explicitly returns the input unchanged unless the mode is exactly `VSYNC_ENABLED`:

```cpp
DisplayServerEnums::VSyncMode vsync_mode = DisplayServer::get_singleton()->window_get_vsync_mode(DisplayServerEnums::MAIN_WINDOW_ID);
if (vsync_mode != DisplayServerEnums::VSYNC_ENABLED) {
    return p_delta;
}
```

This happens before the FPS checks, refresh-rate estimator, and smoothing calculations. Adaptive and mailbox modes also bypass smoothing.

With vsync off, leaving the setting enabled only adds the mode check; disabling it returns slightly earlier. **There is no difference in the resulting delta from this feature.**

The check uses Godot’s reported mode. If a driver forces vsync on while Godot reports it disabled, smoothing still stays bypassed, as the [source comments explain](C:/Users/k/Repository/External/Godot_4-7-2/main/main_timer_sync.cpp:253).
