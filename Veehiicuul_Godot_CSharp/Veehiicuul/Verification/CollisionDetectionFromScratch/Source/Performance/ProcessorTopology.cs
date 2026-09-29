using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace CollisionDetectionFromScratch;

/// <summary>The logical processors that an affinity request names.</summary>
/// <param name="TimingProcessor">Where a timing process runs; negative when it is not pinned.</param>
/// <param name="Mask">Every named logical processor; zero when the request names none.</param>
/// <param name="Description">The request and its outcome, for the records.</param>
internal readonly record struct Placement(int TimingProcessor, long Mask, string Description);

/// <summary>
/// Pins the measuring process to one core. Hybrid processors have cores of
/// different speed; without pinning, the scheduler may move the measuring
/// thread between them in the middle of a sample.
/// </summary>
internal static partial class ProcessorTopology {
    private const int RelationProcessorCore = 0;

    /// <summary>
    /// Resolves "performance", "efficiency", "none", or a list of logical processor
    /// numbers such as "19,20,21". A timing process runs on the last processor named.
    /// </summary>
    public static Placement Select(string affinity) {
        if (!OperatingSystem.IsWindows()) {
            throw new PlatformNotSupportedException("Only Windows is supported.");
        }

        List<(long Mask, int EfficiencyClass)> cores = ReadCores();
        int lowest = int.MaxValue, highest = int.MinValue;
        foreach ((long _, int efficiencyClass) in cores) {
            lowest = Math.Min(lowest, efficiencyClass);
            highest = Math.Max(highest, efficiencyClass);
        }

        int fastCores = 0, slowCores = 0;
        foreach ((long _, int efficiencyClass) in cores) {
            if (efficiencyClass == highest) {
                ++fastCores;
            } else {
                ++slowCores;
            }
        }

        string summary = $"cores={cores.Count}; highest efficiency class on {fastCores}; others {slowCores}";
        if (affinity == "none") {
            return new Placement(-1, 0, summary + "; affinity=none");
        }

        long mask = 0;
        int selected = -1;
        if (affinity is "performance" or "efficiency") {
            int wanted = affinity == "performance" ? highest : lowest;
            // The last matching core avoids processor zero, which services most interrupts.
            foreach ((long coreMask, int efficiencyClass) in cores) {
                if (efficiencyClass == wanted) {
                    mask |= coreMask;
                    selected = System.Numerics.BitOperations.TrailingZeroCount(coreMask);
                }
            }
        } else {
            foreach (string item in affinity.Split(',', StringSplitOptions.RemoveEmptyEntries)) {
                if (!int.TryParse(item.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out selected)
                    || selected > 62) {
                    throw new ArgumentException(
                        "Affinity must be performance, efficiency, none, or logical processor numbers.");
                }

                mask |= 1L << selected;
            }
        }

        if (selected is < 0 or > 62) {
            throw new InvalidOperationException("No usable core was found for the requested affinity.");
        }

        long existing = 0;
        int selectedClass = -1;
        foreach ((long coreMask, int efficiencyClass) in cores) {
            existing |= coreMask;
            if ((coreMask & (1L << selected)) != 0) {
                selectedClass = efficiencyClass;
            }
        }

        if ((mask & ~existing) != 0) {
            throw new ArgumentException("The affinity names a logical processor that does not exist.");
        }

        return new Placement(
            selected,
            mask,
            summary + $"; affinity={affinity} on logical processor {selected} (efficiency class {selectedClass})");
    }

    public static string Apply(string affinity, string priority) {
        if (!OperatingSystem.IsWindows()) {
            throw new PlatformNotSupportedException("Only Windows is supported.");
        }

        Placement placement = Select(affinity);
        using Process process = Process.GetCurrentProcess();
        process.PriorityClass = priority switch {
            "high" => ProcessPriorityClass.High,
            "normal" => ProcessPriorityClass.Normal,
            // For measuring while the machine is in use for something else.
            "belownormal" => ProcessPriorityClass.BelowNormal,
            _ => throw new ArgumentException("Priority must be high, normal, or belownormal."),
        };
        if (placement.TimingProcessor >= 0) {
            process.ProcessorAffinity = new IntPtr(1L << placement.TimingProcessor);
        }

        return placement.Description + $"; priority={priority}";
    }

    private static List<(long Mask, int EfficiencyClass)> ReadCores() {
        uint length = 0;
        _ = GetLogicalProcessorInformationEx(RelationProcessorCore, IntPtr.Zero, ref length);
        if (length == 0) {
            throw new InvalidOperationException("The processor topology is unavailable.");
        }

        IntPtr buffer = Marshal.AllocHGlobal((int)length);
        try {
            if (!GetLogicalProcessorInformationEx(RelationProcessorCore, buffer, ref length)) {
                throw new InvalidOperationException(
                    "The processor topology could not be read: error " + Marshal.GetLastPInvokeError() + ".");
            }

            List<(long, int)> cores = [];
            int offset = 0;
            while (offset < length) {
                int relationship = Marshal.ReadInt32(buffer, offset);
                int size = Marshal.ReadInt32(buffer, offset + 4);
                if (relationship == RelationProcessorCore) {
                    int efficiencyClass = Marshal.ReadByte(buffer, offset + 9);
                    long mask = Marshal.ReadInt64(buffer, offset + 32);
                    int group = Marshal.ReadInt16(buffer, offset + 40);
                    if (group == 0 && mask != 0) {
                        cores.Add((mask, efficiencyClass));
                    }
                }

                offset += size;
            }

            return cores;
        } finally {
            Marshal.FreeHGlobal(buffer);
        }
    }

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetLogicalProcessorInformationEx(
        int relationshipType, IntPtr buffer, ref uint returnedLength);
}
