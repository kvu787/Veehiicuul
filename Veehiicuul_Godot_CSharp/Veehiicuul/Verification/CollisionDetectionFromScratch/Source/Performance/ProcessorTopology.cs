using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CollisionDetectionFromScratch;

/// <summary>
/// Pins the measuring process to one core. Hybrid processors have cores of
/// different speed; without pinning, the scheduler may move the measuring
/// thread between them in the middle of a sample.
/// </summary>
internal static partial class ProcessorTopology {
    private const int RelationProcessorCore = 0;

    public static string Apply(string affinity, string priority) {
        if (!OperatingSystem.IsWindows()) {
            throw new PlatformNotSupportedException("Only Windows is supported.");
        }

        using Process process = Process.GetCurrentProcess();
        process.PriorityClass = priority switch {
            "high" => ProcessPriorityClass.High,
            "normal" => ProcessPriorityClass.Normal,
            _ => throw new ArgumentException("Priority must be high or normal."),
        };
        List<(int Processor, int EfficiencyClass)> cores = ReadCores();
        int lowest = int.MaxValue, highest = int.MinValue;
        foreach ((int _, int efficiencyClass) in cores) {
            lowest = Math.Min(lowest, efficiencyClass);
            highest = Math.Max(highest, efficiencyClass);
        }

        int fastCores = 0, slowCores = 0;
        foreach ((int _, int efficiencyClass) in cores) {
            if (efficiencyClass == highest) {
                ++fastCores;
            } else {
                ++slowCores;
            }
        }

        string summary = $"cores={cores.Count}; highest efficiency class on {fastCores}; others {slowCores}; "
            + $"priority={priority}";
        if (affinity == "none") {
            return summary + "; affinity=none";
        }

        int wanted = affinity switch {
            "performance" => highest,
            "efficiency" => lowest,
            _ => throw new ArgumentException("Affinity must be performance, efficiency, or none."),
        };
        // The last matching core avoids processor zero, which services most interrupts.
        int selected = -1;
        foreach ((int processor, int efficiencyClass) in cores) {
            if (efficiencyClass == wanted) {
                selected = processor;
            }
        }

        if (selected is < 0 or > 62) {
            throw new InvalidOperationException("No usable core was found for the requested affinity.");
        }

        process.ProcessorAffinity = new IntPtr(1L << selected);
        return summary + $"; affinity={affinity} on logical processor {selected} (efficiency class {wanted})";
    }

    private static List<(int Processor, int EfficiencyClass)> ReadCores() {
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

            List<(int, int)> cores = [];
            int offset = 0;
            while (offset < length) {
                int relationship = Marshal.ReadInt32(buffer, offset);
                int size = Marshal.ReadInt32(buffer, offset + 4);
                if (relationship == RelationProcessorCore) {
                    int efficiencyClass = Marshal.ReadByte(buffer, offset + 9);
                    long mask = Marshal.ReadInt64(buffer, offset + 32);
                    int group = Marshal.ReadInt16(buffer, offset + 40);
                    if (group == 0 && mask != 0) {
                        cores.Add((System.Numerics.BitOperations.TrailingZeroCount(mask), efficiencyClass));
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
