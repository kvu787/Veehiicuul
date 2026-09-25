using Godot;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Numerics;
using System.Reflection;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Versioning;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Environment = System.Environment;

namespace Veehiicuul_Godot_CSharp;

public partial class DebugInfo {
    /// <summary>Prints a diagnostic snapshot to Godot's output and log.</summary>
    /// <remarks>
    /// Call on the Godot main thread after engine initialization (for example, from _Ready),
    /// outside timed benchmark runs. WMI and driver queries are synchronous and can take seconds.
    /// Unavailable optional information is reported without discarding the other sections.
    /// </remarks>
    /// <exception cref="PlatformNotSupportedException">
    /// The operating system or the current process is not Windows x64 (including x64 emulation on ARM64).
    /// </exception>
    public static void PrintSystemInfo_WindowsX64() {
        // Check before touching Godot or any Windows-only APIs.
        if (!OperatingSystem.IsWindows() || RuntimeInformation.OSArchitecture != Architecture.X64 ||
            RuntimeInformation.ProcessArchitecture != Architecture.X64) {
            throw new PlatformNotSupportedException(
                $"System information requires Windows x64 and an x64 process. " +
                $"Detected {RuntimeInformation.OSDescription}; OS architecture: {RuntimeInformation.OSArchitecture}; " +
                $"process architecture: {RuntimeInformation.ProcessArchitecture}.");
        }

        PrintWindowsReport();
    }

    [SupportedOSPlatform("windows")]
    private static void PrintWindowsReport() {
        Stopwatch elapsed = Stopwatch.StartNew();
        StringBuilder report = new(16384);
        // Use LF explicitly; Godot's Output panel adds extra spacing for CRLF.
        _ = report.Append("================ SYSTEM INFORMATION ================").Append('\n');
        AppendValue(report, "Captured at (UTC)", DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture));
        _ = report.Append("Snapshot only; collect before benchmarking. Hardware/driver values are provider-reported.").Append('\n');

        AppendSection(report, "Windows", () => {
            AppendValue(report, "Operating system", OS.GetDistributionName());
            AppendValue(report, "Windows version", OS.GetVersion());
            AppendValue(report, "Runtime OS description", RuntimeInformation.OSDescription);
            AppendValue(report, "OS / process architecture", $"{RuntimeInformation.OSArchitecture} / {RuntimeInformation.ProcessArchitecture}");
            AppendValue(report, "System uptime", TimeSpan.FromMilliseconds(Environment.TickCount64));
            AppendValue(report, "Time zone", TimeZoneInfo.Local.Id);
            AppendValue(report, "Culture / UI culture", $"{CultureInfo.CurrentCulture.Name} / {CultureInfo.CurrentUICulture.Name}");
            using RegistryKey? version = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            AppendValue(report, "Windows release", version?.GetValue("DisplayVersion"));
            AppendValue(report, "Edition", version?.GetValue("EditionID"));
            AppendValue(report, "Build", version?.GetValue("CurrentBuildNumber"));
            AppendValue(report, "Build revision", version?.GetValue("UBR"));
        });

        AppendManagementSection(report, "Computer", "Win32_ComputerSystem",
            ("Manufacturer", "Manufacturer"), ("Model", "Model"),
            ("SystemType", "System type"), ("NumberOfProcessors", "Processor sockets"),
            ("NumberOfLogicalProcessors", "System logical processors"),
            ("TotalPhysicalMemory", "System-reported physical memory (bytes)"),
            ("HypervisorPresent", "Hypervisor present"));
        AppendManagementSection(report, "Motherboard", "Win32_BaseBoard",
            ("Manufacturer", "Manufacturer"), ("Product", "Product"), ("Version", "Version"));
        AppendManagementSection(report, "Firmware", "Win32_BIOS",
            ("Manufacturer", "Manufacturer"), ("SMBIOSBIOSVersion", "BIOS version"),
            ("ReleaseDate", "Release date (DMTF)"),
            ("SMBIOSMajorVersion", "SMBIOS major version"), ("SMBIOSMinorVersion", "SMBIOS minor version"));
        AppendManagementSection(report, "Processors", "Win32_Processor",
            ("DeviceID", "Device"), ("Name", "Name"), ("Manufacturer", "Manufacturer"),
            ("NumberOfCores", "Physical cores"), ("NumberOfEnabledCore", "Enabled cores"),
            ("NumberOfLogicalProcessors", "Logical processors"),
            ("MaxClockSpeed", "SMBIOS maximum clock (MHz)"),
            ("CurrentClockSpeed", "SMBIOS current clock (MHz; not a live boost measurement)"),
            ("L2CacheSize", "L2 cache (KiB)"), ("L3CacheSize", "L3 cache (KiB)"),
            ("VirtualizationFirmwareEnabled", "Firmware virtualization enabled"));

        AppendSection(report, "CPU capabilities available to this process", () => {
            AppendValue(report, "Processor name", OS.GetProcessorName());
            AppendValue(report, ".NET available logical processors", Environment.ProcessorCount);
            AppendValue(report, "Vector acceleration / float lanes", $"{Vector.IsHardwareAccelerated} / {Vector<float>.Count}");
            AppendValue(report, "SSE / SSE2 / SSE3 / SSSE3", $"{Sse.IsSupported} / {Sse2.IsSupported} / {Sse3.IsSupported} / {Ssse3.IsSupported}");
            AppendValue(report, "SSE4.1 / SSE4.2", $"{Sse41.IsSupported} / {Sse42.IsSupported}");
            AppendValue(report, "AVX / AVX2 / AVX512F / FMA", $"{Avx.IsSupported} / {Avx2.IsSupported} / {Avx512F.IsSupported} / {Fma.IsSupported}");
            AppendValue(report, "AES / BMI1 / BMI2 / POPCNT", $"{Aes.IsSupported} / {Bmi1.IsSupported} / {Bmi2.IsSupported} / {Popcnt.IsSupported}");
        });

        AppendManagementSection(report, "Memory and operating system snapshot", "Win32_OperatingSystem",
            ("Caption", "Windows name"), ("Version", "Version"), ("BuildNumber", "Build"),
            ("LastBootUpTime", "Last boot (DMTF)"),
            ("TotalVisibleMemorySize", "OS-visible physical memory (KiB)"),
            ("FreePhysicalMemory", "Free physical memory (KiB)"),
            ("TotalVirtualMemorySize", "Total virtual memory (KiB)"),
            ("FreeVirtualMemory", "Free virtual memory (KiB)"),
            ("SizeStoredInPagingFiles", "Page file size (KiB)"),
            ("NumberOfProcesses", "Running process count"));
        AppendManagementSection(report, "Memory modules", "Win32_PhysicalMemory",
            ("DeviceLocator", "Slot"), ("BankLabel", "Bank"), ("Manufacturer", "Manufacturer"),
            ("PartNumber", "Part number"), ("Capacity", "Capacity (bytes)"),
            ("SMBIOSMemoryType", "SMBIOS memory type code"),
            ("Speed", "Rated speed (provider-reported MHz)"),
            ("ConfiguredClockSpeed", "Configured speed (provider-reported MHz)"),
            ("DataWidth", "Data width (bits)"), ("TotalWidth", "Total width including ECC (bits)"));
        AppendManagementSection(report, "Installed graphics adapters", "Win32_VideoController",
            ("Name", "Name"), ("AdapterCompatibility", "Vendor"),
            ("VideoProcessor", "Video processor"), ("DriverVersion", "Driver version"),
            ("DriverDate", "Driver date (DMTF)"), ("Status", "Status"));

        AppendSection(report, "Godot and active renderer", () => {
            Godot.Collections.Dictionary version = Engine.GetVersionInfo();
            AppendValue(report, "Godot version", version["string"]);
            AppendValue(report, "Godot commit", version["hash"]);
            AppendValue(report, "Editor / debug engine", $"{Engine.IsEditorHint()} / {OS.IsDebugBuild()}");
            AppendValue(report, "Display driver", DisplayServer.GetName());
            if (DisplayServer.GetName() == "headless") {
                AppendValue(report, "Active renderer", "Unavailable (headless)");
                return;
            }
            AppendValue(report, "Rendering method", RenderingServer.GetCurrentRenderingMethod());
            AppendValue(report, "Rendering driver", RenderingServer.GetCurrentRenderingDriverName());
            AppendValue(report, "Active GPU", RenderingServer.GetVideoAdapterName());
            AppendValue(report, "GPU vendor", RenderingServer.GetVideoAdapterVendor());
            AppendValue(report, "GPU type", RenderingServer.GetVideoAdapterType());
            AppendValue(report, "Graphics API version", RenderingServer.GetVideoAdapterApiVersion());
            AppendValue(report, "Active GPU driver", string.Join(" / ", OS.GetVideoAdapterDriverInfo()));
        });

        AppendSection(report, "Active GPU memory (DXGI)", () => AppendGraphicsMemory(report));

        AppendSection(report, "Displays and main window", () => {
            if (DisplayServer.GetName() == "headless") {
                _ = report.Append("  Unavailable (headless).").Append('\n');
                return;
            }
            int count = DisplayServer.GetScreenCount();
            AppendValue(report, "Screen count", count);
            for (int screen = 0; screen < count; screen++) {
                AppendSection(report, $"Screen {screen}", () => {
                    AppendValue(report, "Position / size (pixels)", $"{DisplayServer.ScreenGetPosition(screen)} / {DisplayServer.ScreenGetSize(screen)}");
                    AppendValue(report, "Usable rectangle", DisplayServer.ScreenGetUsableRect(screen));
                    AppendValue(report, "DPI", DisplayServer.ScreenGetDpi(screen));
                    float refreshRate = DisplayServer.ScreenGetRefreshRate(screen);
                    AppendValue(report, "Reported refresh rate (Hz)", refreshRate > 0 ? refreshRate : "Unavailable");
                });
            }
            AppendValue(report, "Window screen", DisplayServer.WindowGetCurrentScreen());
            AppendValue(report, "Window size (pixels)", DisplayServer.WindowGetSize());
            AppendValue(report, "Window mode", DisplayServer.WindowGetMode());
            AppendValue(report, "Godot VSync mode", DisplayServer.WindowGetVsyncMode());
            _ = report.Append("  Driver overrides and actual VRR/G-Sync/FreeSync engagement are not measured.").Append('\n');
        });

        AppendSection(report, "Timing and engine configuration", () => {
            AppendValue(report, "Maximum FPS (0 = unlimited)", Engine.MaxFps);
            AppendValue(report, "Physics ticks per second", Engine.PhysicsTicksPerSecond);
            AppendValue(report, "Maximum physics steps per frame", Engine.MaxPhysicsStepsPerFrame);
            AppendValue(report, "Time scale", Engine.TimeScale);
            AppendValue(report, "Low processor usage mode", OS.LowProcessorUsageMode);
            AppendValue(report, "Low processor usage sleep (microseconds)", OS.LowProcessorUsageModeSleepUsec);
            AppendValue(report, "Delta smoothing", OS.DeltaSmoothing);
            AppendValue(report, "Accumulated input", Input.UseAccumulatedInput);
            AppendValue(report, "Stopwatch high resolution", Stopwatch.IsHighResolution);
            AppendValue(report, "Stopwatch frequency (Hz)", Stopwatch.Frequency);
            AppendValue(report, "Stopwatch tick interval (ns; not scheduler resolution)", 1_000_000_000.0 / Stopwatch.Frequency);
            string[] settings = [
                "rendering/rendering_device/vsync/swapchain_image_count",
                "rendering/driver/threads/thread_model",
                "application/run/max_fps",
                "application/run/frame_delay_msec",
                "application/run/low_processor_mode",
                "physics/2d/physics_engine",
                "physics/3d/physics_engine",
                "rendering/renderer/rendering_method",
                "rendering/rendering_device/driver",
                "rendering/anti_aliasing/quality/msaa_3d",
                "rendering/anti_aliasing/quality/use_taa",
                "rendering/scaling_3d/scale"
            ];
            foreach (string setting in settings) {
                AppendValue(report, $"Project setting: {setting}", ProjectSettings.HasSetting(setting)
                    ? ProjectSettings.GetSettingWithOverride(setting).ToString() : "Unavailable");
            }
        });

        AppendSection(report, ".NET and process", () => {
            AppendValue(report, "Runtime", RuntimeInformation.FrameworkDescription);
            AppendValue(report, "Runtime identifier", RuntimeInformation.RuntimeIdentifier);
            AppendValue(report, "Application assembly", typeof(DebugInfo).Assembly.FullName);
#if DEBUG
            AppendValue(report, "C# build configuration", "Debug");
#else
            AppendValue(report, "C# build configuration", "Release");
#endif
            AppendValue(report, "JIT optimizations disabled by assembly", typeof(DebugInfo).Assembly.GetCustomAttribute<DebuggableAttribute>()?.IsJITOptimizerDisabled);
            AppendValue(report, "Managed debugger attached", Debugger.IsAttached);
            AppendValue(report, "Server garbage collection", GCSettings.IsServerGC);
            AppendValue(report, "Garbage collection latency mode", GCSettings.LatencyMode);
            AppendValue(report, "Managed heap (no forced collection)", FormatBytes(GC.GetTotalMemory(false)));
            AppendValue(report, "GC memory budget", FormatBytes(GC.GetGCMemoryInfo().TotalAvailableMemoryBytes));
            AppendValue(report, "GC collections (generation 0 / 1 / 2)", $"{GC.CollectionCount(0)} / {GC.CollectionCount(1)} / {GC.CollectionCount(2)}");
            using Process process = Process.GetCurrentProcess();
            AppendValue(report, "Process ID", process.Id);
            AppendValue(report, "Executable", Environment.ProcessPath);
            AppendValue(report, "Priority", process.PriorityClass);
            AppendValue(report, "Affinity mask (current processor group)", $"0x{process.ProcessorAffinity.ToInt64():X16}");
            AppendValue(report, "Working set", FormatBytes(process.WorkingSet64));
            AppendValue(report, "Peak working set", FormatBytes(process.PeakWorkingSet64));
            AppendValue(report, "Private committed memory", FormatBytes(process.PrivateMemorySize64));
            AppendValue(report, "Process CPU time", process.TotalProcessorTime);
            AppendValue(report, "Process thread count", process.Threads.Count);
        });

        AppendSection(report, "Power supply", () => {
            if (!GetSystemPowerStatus(out SystemPowerStatus power)) {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            AppendValue(report, "AC power", power.AcLineStatus switch { 0 => "Offline", 1 => "Online", _ => "Unavailable" });
            AppendValue(report, "Battery status flags (128 = no battery; 255 = unknown)", power.BatteryFlag);
            AppendValue(report, "Battery charge (%)", power.BatteryLifePercent <= 100 ? power.BatteryLifePercent : "Unavailable");
            AppendValue(report, "Battery saver", power.SystemStatusFlag != 0);
            AppendValue(report, "Estimated battery time (seconds)", power.BatteryLifeTime >= 0 ? power.BatteryLifeTime : "Unavailable");
        });
        AppendSection(report, "Active Windows power plan", () => AppendPowerPlan(report));
        AppendManagementSection(report, "Physical storage", "Win32_DiskDrive",
            ("Index", "Disk index"), ("Model", "Model"), ("FirmwareRevision", "Firmware"),
            ("InterfaceType", "WMI interface type"), ("MediaType", "Media type"),
            ("Size", "Size (bytes)"), ("BytesPerSector", "Bytes per sector"), ("Status", "Status"));
        AppendManagementSection(report, "Local fixed volumes", "Win32_LogicalDisk",
            [("DeviceID", "Drive"), ("FileSystem", "File system"), ("Size", "Size (bytes)"),
             ("FreeSpace", "Free space (bytes)")], @"root\cimv2", "DriveType = 3");

        AppendValue(report, "Report collection duration (ms)", elapsed.Elapsed.TotalMilliseconds);
        _ = report.Append("============== END SYSTEM INFORMATION ==============").Append('\n');
        GD.Print(report.ToString());
    }

    private static void AppendSection(StringBuilder report, string title, Action collect) {
        _ = report.Append('\n').Append('[').Append(title).Append(']').Append('\n');
        try {
            collect();
        } catch (Exception exception) when (exception is ManagementException or COMException or
            Win32Exception or UnauthorizedAccessException or SecurityException or IOException or
            InvalidOperationException or NotSupportedException or TimeoutException) {
            AppendValue(report, "Unavailable", $"{exception.GetType().Name} (0x{exception.HResult:X8}): {exception.Message}");
        }
    }

    [SupportedOSPlatform("windows")]
    private static void AppendManagementSection(StringBuilder report, string title, string className,
        params (string Property, string Label)[] fields) {
        AppendManagementSection(report, title, className, fields, @"root\cimv2", null);
    }

    [SupportedOSPlatform("windows")]
    private static void AppendManagementSection(StringBuilder report, string title, string className,
        (string Property, string Label)[] fields, string scopePath, string? condition) {
        AppendSection(report, title, () => {
            // WMI timeouts apply to connection/enumeration, not a hard deadline for every provider.
            TimeSpan timeout = TimeSpan.FromSeconds(3);
            ManagementScope scope = new(scopePath, new ConnectionOptions { Timeout = timeout });
            ObjectQuery query = new($"SELECT {string.Join(", ", fields.Select(field => field.Property))} FROM {className}" +
                (condition == null ? "" : $" WHERE {condition}"));
            System.Management.EnumerationOptions options = new() { Timeout = timeout, ReturnImmediately = true, Rewindable = false };
            using ManagementObjectSearcher searcher = new(scope, query, options);
            using ManagementObjectCollection results = searcher.Get();
            int index = 0;
            foreach (ManagementBaseObject item in results) {
                using (item) {
                    _ = report.Append("  Entry ").Append(++index).Append(':').Append('\n');
                    foreach ((string Property, string Label) field in fields) {
                        AppendValue(report, field.Label, item[field.Property]);
                    }
                }
            }
            if (index == 0) {
                _ = report.Append("  No entries reported.").Append('\n');
            }
        });
    }

    private static void AppendValue(StringBuilder report, string label, object? value) {
        string? text = value is IFormattable formattable
            ? formattable.ToString(null, CultureInfo.InvariantCulture) : value?.ToString();
        _ = report.Append("  ").Append(label).Append(": ").Append(string.IsNullOrWhiteSpace(text) ? "Unavailable" : text.Trim()).Append('\n');
    }

    private static string FormatBytes(long bytes) {
        return string.Create(CultureInfo.InvariantCulture, $"{bytes:N0} bytes ({bytes / 1073741824.0:F2} GiB)");
    }

    private static string FormatBytes(ulong bytes) {
        return string.Create(CultureInfo.InvariantCulture, $"{bytes:N0} bytes ({bytes / 1073741824.0:F2} GiB)");
    }

    [SupportedOSPlatform("windows")]
    private static void AppendGraphicsMemory(StringBuilder report) {
        if (DisplayServer.GetName() == "headless") {
            AppendValue(report, "Unavailable", "No active GPU in headless mode.");
            return;
        }
        string driver = RenderingServer.GetCurrentRenderingDriverName();
        if (driver != "d3d12") {
            // Other backends expose a different native handle, not an IDXGIAdapter.
            AppendValue(report, "Unavailable", $"Active-adapter DXGI reporting requires D3D12; current driver: {driver}.");
            return;
        }

        TaskCompletionSource<string> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        RenderingServer.CallOnRenderThread(Callable.From(() => {
            try {
                // Keep the result private so a delayed callback cannot modify a timed-out report.
                completion.SetResult(CollectGraphicsMemory());
            } catch (Exception exception) {
                // Transfer errors to the calling thread's normal diagnostic error handling.
                completion.SetException(exception);
            }
        }));
        _ = report.Append(completion.Task.WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult());
    }

    [SupportedOSPlatform("windows")]
    private static unsafe string CollectGraphicsMemory() {
        StringBuilder report = new();
        RenderingDevice? device = RenderingServer.GetRenderingDevice();
        if (device == null) {
            throw new InvalidOperationException("Godot has no active rendering device.");
        }

        // This IDXGIAdapter is borrowed from Godot. Do not release or wrap it in an owning COM object.
        IntPtr adapter = (IntPtr)device.GetDriverResource(RenderingDevice.DriverResource.PhysicalDevice, default, 0);
        if (adapter == IntPtr.Zero) {
            throw new InvalidOperationException("Godot did not provide its D3D12 adapter.");
        }
        GraphicsAdapterDescription description = default;
        void** adapterMethods = *(void***)adapter;
        // IDXGIAdapter::GetDesc is COM vtable slot 8, including IUnknown and IDXGIObject.
        int result = ((delegate* unmanaged[Stdcall]<IntPtr, GraphicsAdapterDescription*, int>)adapterMethods[8])(adapter, &description);
        CheckGraphicsResult(result, "IDXGIAdapter.GetDesc");
        AppendValue(report, "Adapter", new string(description.Description, 0, 128).TrimEnd('\0'));
        AppendValue(report, "Adapter LUID", $"{description.AdapterIdentifierHigh:X8}:{description.AdapterIdentifierLow:X8}");
        AppendValue(report, "Dedicated video memory capacity", FormatBytes((ulong)description.DedicatedVideoMemory));
        AppendValue(report, "Dedicated system memory", FormatBytes((ulong)description.DedicatedSystemMemory));
        AppendValue(report, "Shared system memory limit", FormatBytes((ulong)description.SharedSystemMemory));
        _ = report.Append("  Budgets and usage below apply to this process on GPU node 0; they are not GPU-wide free memory.").Append('\n');
        _ = report.Append("  Local memory is VRAM on discrete GPUs and shared system RAM on integrated/UMA GPUs.").Append('\n');

        // Preserve capacity information even if the driver cannot provide budget information.
        AppendSection(report, "GPU memory budgets and usage", () => {
            Guid adapterInterface = new("645967A4-1392-4310-A798-8053CE3E93FD"); // IID_IDXGIAdapter3
            CheckGraphicsResult(Marshal.QueryInterface(adapter, in adapterInterface, out IntPtr budgetAdapter), "QueryInterface(IDXGIAdapter3)");
            try {
                AppendSection(report, "Local GPU memory", () => AppendGraphicsMemoryBudget(report, budgetAdapter, 0));
                AppendSection(report, "Non-local GPU memory (system RAM on discrete GPUs)", () => AppendGraphicsMemoryBudget(report, budgetAdapter, 1));
            } finally {
                // QueryInterface acquired this reference; only this reference is ours to release.
                _ = Marshal.Release(budgetAdapter);
            }
        });
        return report.ToString();
    }

    private static unsafe void AppendGraphicsMemoryBudget(StringBuilder report, IntPtr adapter, uint segmentGroup) {
        GraphicsMemoryBudget memory = default;
        void** methods = *(void***)adapter;
        // IDXGIAdapter3::QueryVideoMemoryInfo is slot 14. Segment groups: LOCAL = 0, NON_LOCAL = 1.
        int result = ((delegate* unmanaged[Stdcall]<IntPtr, uint, uint, GraphicsMemoryBudget*, int>)methods[14])(adapter, 0, segmentGroup, &memory);
        CheckGraphicsResult(result, "IDXGIAdapter3.QueryVideoMemoryInfo");
        AppendValue(report, "Windows budget for this process", FormatBytes(memory.Budget));
        AppendValue(report, "Current process usage", FormatBytes(memory.CurrentUsage));
        // Usage can exceed a shrinking budget; avoid unsigned subtraction underflow.
        AppendValue(report, "Remaining process budget", FormatBytes(memory.Budget > memory.CurrentUsage ? memory.Budget - memory.CurrentUsage : 0UL));
        AppendValue(report, "Process usage above budget", FormatBytes(memory.CurrentUsage > memory.Budget ? memory.CurrentUsage - memory.Budget : 0UL));
        AppendValue(report, "Current process reservation", FormatBytes(memory.CurrentReservation));
        AppendValue(report, "Available for reservation", FormatBytes(memory.AvailableForReservation));
    }

    private static void CheckGraphicsResult(int result, string operation) {
        if (result < 0) {
            throw new InvalidOperationException($"{operation} failed (HRESULT 0x{result:X8}).", Marshal.GetExceptionForHR(result));
        }
    }

    // Native DXGI_ADAPTER_DESC (dxgi.h). SIZE_T fields use the process pointer size.
    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct GraphicsAdapterDescription {
        public fixed char Description[128];
        public uint VendorIdentifier;
        public uint DeviceIdentifier;
        public uint SubsystemIdentifier;
        public uint Revision;
        public nuint DedicatedVideoMemory;
        public nuint DedicatedSystemMemory;
        public nuint SharedSystemMemory;
        public uint AdapterIdentifierLow;
        public int AdapterIdentifierHigh;
    }

    // Native DXGI_QUERY_VIDEO_MEMORY_INFO (dxgi1_4.h); all sizes are UINT64 bytes.
    [StructLayout(LayoutKind.Sequential)]
    private struct GraphicsMemoryBudget {
        public ulong Budget;
        public ulong CurrentUsage;
        public ulong AvailableForReservation;
        public ulong CurrentReservation;
    }

    private static void AppendPowerPlan(StringBuilder report) {
        uint result = PowerGetActiveScheme(IntPtr.Zero, out IntPtr schemePointer);
        if (result != 0) {
            throw new Win32Exception((int)result);
        }
        Guid scheme;
        try {
            scheme = Marshal.PtrToStructure<Guid>(schemePointer);
        } finally {
            _ = LocalFree(schemePointer);
        }
        AppendValue(report, "Plan identifier", scheme);
        uint bufferSize = 0;
        result = PowerReadFriendlyName(IntPtr.Zero, in scheme, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, ref bufferSize);
        if (result != 0 && result != 234) { // ERROR_MORE_DATA is expected when requesting the size.
            throw new Win32Exception((int)result);
        }
        IntPtr buffer = Marshal.AllocHGlobal(checked((int)bufferSize));
        try {
            result = PowerReadFriendlyName(IntPtr.Zero, in scheme, IntPtr.Zero, IntPtr.Zero, buffer, ref bufferSize);
            if (result != 0) {
                throw new Win32Exception((int)result);
            }
            AppendValue(report, "Plan", Marshal.PtrToStringUni(buffer));
        } finally {
            Marshal.FreeHGlobal(buffer);
        }
    }

    [LibraryImport("powrprof.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial uint PowerGetActiveScheme(IntPtr rootPowerKey, out IntPtr scheme);

    [LibraryImport("powrprof.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial uint PowerReadFriendlyName(IntPtr rootPowerKey, in Guid scheme,
        IntPtr subgroup, IntPtr setting, IntPtr buffer, ref uint bufferSize);

    [LibraryImport("kernel32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial IntPtr LocalFree(IntPtr memory);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetSystemPowerStatus(out SystemPowerStatus status);

    [StructLayout(LayoutKind.Sequential)]
    private struct SystemPowerStatus {
        public byte AcLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte SystemStatusFlag;
        public int BatteryLifeTime;
        public int BatteryFullLifeTime;
    }
}
