using System;
using System.Globalization;

namespace Veehiicuul_Godot_CSharp;

public static class DateTimeUtility {
    // yyyy/MM/dd hh:mm:ss tt is 22 characters. Shared storage is safe on the one game thread.
    private static readonly char[] Chars = new char[22];
    private const string Format = "yyyy'/'MM'/'dd hh':'mm':'ss tt";

    public static char[] GetDateTimeNowChars_Buffered() {
        bool success = DateTime.Now.TryFormat(Chars.AsSpan(), out int charactersWritten, Format.AsSpan(), CultureInfo.InvariantCulture);
        if (!success || charactersWritten != Chars.Length) {
            throw new InvalidOperationException($"Date/time format did not fit the static buffer (size={Chars.Length}).");
        }
        return Chars;
    }
}
