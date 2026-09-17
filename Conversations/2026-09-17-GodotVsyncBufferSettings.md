# Conversation: Godot buffer settings with VSync disabled

## User

c:\Users\k\Repository\Veehiicuul\InputLatency_Godot\project.godot:34:1-35:42
Lines and columns are 1-based; columns count UTF-16 code units (a tab counts as one unit). Start inclusive, end exclusive.

rendering_device/vsync/swapchain_image_count=2
rendering_device/vsync/frame_queue_size=2

do these settings matter if vsync is off? refer to the actual source code at "C:\Users\k\Repository\External\Godot_4-7-2"

## Assistant

I’ll trace both settings through the local Godot source and check how they behave when VSync is disabled.

## Assistant

Both settings are used with VSync off in your project’s Direct3D 12 path. I’m checking the frame waits to explain precisely what `frame_queue_size` limits and how that differs from the swap-chain buffer count.

## Assistant

**Yes—both settings remain active with VSync off.** I traced your local Godot source through the Direct3D 12 backend selected by your project. Neither setting is conditional on VSync being enabled.

- **`swapchain_image_count=2` sets the swap chain to two image buffers.** Godot reads it with a minimum of `2` in [RenderingDevice::_get_swap_chain_desired_count](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/rendering_device.cpp:5356). The D3D12 backend passes that count to `ResizeBuffers` or assigns it to `swap_chain_desc.BufferCount`, regardless of VSync mode. See [swap-chain creation/resizing](C:/Users/k/Repository/External/Godot_4-7-2/drivers/d3d12/rendering_device_driver_d3d12.cpp:2795).

- **`frame_queue_size=2` sets two rotating rendering-frame slots.** For a device with a main window, Godot reads this setting with a minimum of `2`, without checking VSync. See [frame-count initialization](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/rendering_device.cpp:8366). Before reusing a slot, it waits for that slot’s GPU work to finish: [_begin_frame](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/rendering_device.cpp:8048) calls [_stall_for_frame](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/rendering_device.cpp:8214), which waits on its fence. This limits how far rendering work can run ahead even with VSync disabled.

**VSync off changes presentation behavior separately:** the D3D12 branch sets `sync_interval = 0` and enables the tearing flags when supported. It leaves both counts in effect. See [VSYNC_DISABLED](C:/Users/k/Repository/External/Godot_4-7-2/drivers/d3d12/rendering_device_driver_d3d12.cpp:2763).

For your exact configuration, removing `frame_queue_size=2` would preserve the default of **2**; removing `swapchain_image_count=2` would restore the default of **3**. Both current values are the minimum enforced by the code. See [setting defaults](C:/Users/k/Repository/External/Godot_4-7-2/core/config/project_settings.cpp:1823).

These settings control buffering and can affect latency or throughput, but `2` does **not** mean a fixed two-frame input-to-display delay. The source establishes that they are active; measuring the latency difference requires a runtime comparison.
