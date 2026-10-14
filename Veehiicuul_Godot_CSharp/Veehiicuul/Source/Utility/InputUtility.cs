using Godot;
using System;

namespace Veehiicuul_Godot_CSharp;

public static class InputUtility {
    public static float AxialDeadzone(float value, float innerDeadzone, float outerDeadzone) {
        if (!(0f <= innerDeadzone && innerDeadzone <= 1f)) {
            throw new ArgumentOutOfRangeException(nameof(innerDeadzone), innerDeadzone, "Inner deadzone must be in [0, 1].");
        }
        if (!(0f <= outerDeadzone && outerDeadzone <= 1f)) {
            throw new ArgumentOutOfRangeException(nameof(outerDeadzone), outerDeadzone, "Outer deadzone must be in [0, 1].");
        }
        if (innerDeadzone >= outerDeadzone) {
            throw new ArgumentException("Inner deadzone must be less than outer deadzone.", nameof(innerDeadzone));
        }
        if (value == 0f) {
            return 0f;
        }
        float magnitude = Mathf.Abs(value);
        if (magnitude < innerDeadzone) {
            return 0f;
        }
        float sign = Mathf.Sign(value);
        return magnitude > outerDeadzone ? sign : sign * (magnitude - innerDeadzone) / (outerDeadzone - innerDeadzone);
    }

    public static Vector3 RadialDeadzone(Vector3 value, float innerDeadzone, float outerDeadzone) {
        if (!(0f <= innerDeadzone && innerDeadzone <= 1f)) {
            throw new ArgumentOutOfRangeException(nameof(innerDeadzone), innerDeadzone, "Inner deadzone must be in [0, 1].");
        }
        if (!(0f <= outerDeadzone && outerDeadzone <= 1f)) {
            throw new ArgumentOutOfRangeException(nameof(outerDeadzone), outerDeadzone, "Outer deadzone must be in [0, 1].");
        }
        if (innerDeadzone >= outerDeadzone) {
            throw new ArgumentException("Inner deadzone must be less than outer deadzone.", nameof(innerDeadzone));
        }
        float magnitude = value.Length();
        if (magnitude <= innerDeadzone) {
            return Vector3.Zero;
        }
        Vector3 direction = value / magnitude;
        float adjustedMagnitude = magnitude >= outerDeadzone ? 1f : (magnitude - innerDeadzone) / (outerDeadzone - innerDeadzone);
        return direction * adjustedMagnitude;
    }
}
