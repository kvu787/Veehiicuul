using System;
using System.Globalization;

namespace Veehiicuul_Godot_CSharp;

public static class ParseUtility {
    public static float ParseFloat(string text) {
        ArgumentException.ThrowIfNullOrEmpty(text);
        if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) || !float.IsFinite(value)) {
            throw new FormatException($"Failed to parse '{text}' as a finite float.");
        }
        return value;
    }
}
