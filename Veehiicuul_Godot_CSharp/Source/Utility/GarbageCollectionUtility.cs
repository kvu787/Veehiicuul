using System;

namespace Veehiicuul_Godot_CSharp;

public static class GarbageCollectionUtility {
    public static void ForceGarbageCollection() {
        // .NET owns GC policy, rather than UnityEngine.Scripting.GarbageCollector.
        // Godot resources use reference counting; freeing a track releases its native nodes.
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}
