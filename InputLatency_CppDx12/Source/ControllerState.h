#pragma once

#include <algorithm>
#include <array>
#include <charconv>
#include <cmath>
#include <cstddef>
#include <optional>
#include <span>
#include <string_view>

struct ControllerAnalogState
{
    float leftStickX{}, leftStickY{}, rightStickX{}, rightStickY{};
    float leftTrigger{}, rightTrigger{};
};

struct ControllerAvailability { bool connected{}, gamepad{}; };

inline std::optional<std::size_t> SelectController(std::span<const ControllerAvailability> devices,
    std::optional<std::size_t> selected)
{
    const auto eligible = [&](std::size_t index) { return devices[index].connected && devices[index].gamepad; };
    if (selected && *selected < devices.size() && eligible(*selected)) return selected;
    for (std::size_t index = 0; index < devices.size(); ++index) if (eligible(index)) return index;
    return std::nullopt;
}

struct StickPosition { float x{}, y{}; };

inline StickPosition MapStickPosition(float x, float y, float centerX, float centerY, float radius)
{
    // Clamp only drawing coordinates. Displayed state keeps the raw values,
    // with positive Y up, without introducing a deadzone or smoothing.
    const auto coordinate = [](float value) { return std::isfinite(value) ? std::clamp(value, -1.0f, 1.0f) : 0.0f; };
    return {centerX + coordinate(x) * radius, centerY - coordinate(y) * radius};
}

inline float TriggerFill(float value)
{
    return std::isfinite(value) ? std::clamp(value, 0.0f, 1.0f) : 0.0f;
}

// Fixed storage avoids heap allocation/locale processing when drawing numbers
// from the final input sample immediately before submission.
struct AnalogValueText
{
    std::array<char, 32> characters{};
    std::size_t count{};
    std::string_view View() const noexcept { return {characters.data(), count}; }
};

inline AnalogValueText FormatAnalogValue(float value, bool signedValue)
{
    AnalogValueText text;
    if (signedValue && !std::signbit(value)) text.characters[text.count++] = '+';
    const auto result = std::to_chars(text.characters.data() + text.count, text.characters.data() + text.characters.size(),
        value, std::chars_format::fixed, 4);
    if (result.ec == std::errc{}) text.count = static_cast<std::size_t>(result.ptr - text.characters.data());
    else { text.characters[0] = '?'; text.count = 1; }
    return text;
}
