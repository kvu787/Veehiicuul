using System;

namespace Veehiicuul_Godot_CSharp;

public static class GarbageCollectionUtility {
    public static void ForceGarbageCollection() {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}
