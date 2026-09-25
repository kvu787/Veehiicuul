using System;

namespace Veehiicuul_Godot_CSharp;

public static class EnumExtensions {
    // The CLR creates a unique version of this class for every type T.
    // The static constructor runs exactly once per Enum type.
    private static class EnumCache<T> where T : struct, Enum {
        public static readonly T[] Values = Enum.GetValues<T>();
    }

    public static T Next<T>(this T value) where T : struct, Enum {
        T[] values = EnumCache<T>.Values;
        int i = Array.IndexOf(values, value);

        return values[(i + 1) % values.Length];
    }

    public static T Previous<T>(this T value) where T : struct, Enum {
        T[] values = EnumCache<T>.Values;
        int i = Array.IndexOf(values, value);

        return values[(i - 1 + values.Length) % values.Length];
    }
}
