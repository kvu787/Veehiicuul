#pragma once

#include "ApplicationSettings.h"
#include "SimplePaint/Material.h"

// Translate configured paint controls into the portable shader module's input.
// Callers check the RGB length before this conversion; at() also bounds-checks.
inline SimplePaint::Parameters ToPaintParameters(const ApplicationSettings::Paint& paint)
{
    return {
        .baseColorSrgb = {paint.BaseColor.at(0), paint.BaseColor.at(1), paint.BaseColor.at(2)},
        .brightness = paint.Brightness,
        .shift = paint.Shift,
        .rotationDegrees = paint.RotationDegrees,
        .darkPoint = paint.DarkPoint,
        .lightPoint = paint.LightPoint,
    };
}
