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
        _ = report.AppendLine("================ SYSTEM INFORMATION ================");
        AppendValue(report, "Captured at (UTC)", DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture));
        _ = report.AppendLine("Snapshot only; collect before benchmarking. Hardware/driver values are provider-reported.");

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
        _ = report.AppendLine("  VRAM capacity is omitted: WMI AdapterRAM is 32-bit and can misreport modern GPUs.");

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

        AppendSection(report, "Displays and main window", () => {
            if (DisplayServer.GetName() == "headless") {
                _ = report.AppendLine("  Unavailable (headless).");
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
            _ = report.AppendLine("  Driver overrides and actual VRR/G-Sync/FreeSync engagement are not measured.");
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
        _ = report.AppendLine("============== END SYSTEM INFORMATION ==============");
        GD.Print(report.ToString());
    }

    private static void AppendSection(StringBuilder report, string title, Action collect) {
        _ = report.AppendLine().Append('[').Append(title).AppendLine("]");
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
                    _ = report.Append("  Entry ").Append(++index).AppendLine(":");
                    foreach ((string Property, string Label) field in fields) {
                        AppendValue(report, field.Label, item[field.Property]);
                    }
                }
            }
            if (index == 0) {
                _ = report.AppendLine("  No entries reported.");
            }
        });
    }

    private static void AppendValue(StringBuilder report, string label, object? value) {
        string? text = value is IFormattable formattable
            ? formattable.ToString(null, CultureInfo.InvariantCulture) : value?.ToString();
        _ = report.Append("  ").Append(label).Append(": ").AppendLine(string.IsNullOrWhiteSpace(text) ? "Unavailable" : text.Trim());
    }

    private static string FormatBytes(long bytes) {
        return string.Create(CultureInfo.InvariantCulture, $"{bytes:N0} bytes ({bytes / 1073741824.0:F2} GiB)");
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
