#pragma once
#include <array>
#include <cmath>
#include <cstdint>
#include <limits>
#include <optional>

inline constexpr std::size_t MaximumDisplayInputs = 128;
struct DisplayFrameInput
{
    std::uint16_t device{};
    std::uint64_t reading{}, serial{}, sampled{};
};
struct DisplayFrameSubmission
{
    std::uint64_t frame{}, swapChain{}, clockFirstQpc{}, clockLastQpc{}, presentEndQpc{}, gameInputTime{};
    std::uint32_t thread{}, inputCount{};
    bool accepted{};
    std::array<DisplayFrameInput, MaximumDisplayInputs> inputs{};
};
struct DisplayTimestamp
{
    std::uint64_t microseconds{};
    double uncertaintyMicroseconds{};
};

// Convert relative to a tightly bracketed GameInput clock observation, rather
// than assuming that GameInput and QPC have the same epoch or units.
inline std::optional<DisplayTimestamp> MapDisplayTimestamp(const DisplayFrameSubmission& frame,
    std::uint64_t displayQpc, std::uint64_t frequency) noexcept
{
    if (!frequency || frame.clockLastQpc < frame.clockFirstQpc || displayQpc < frame.clockLastQpc) return std::nullopt;
    const auto span = frame.clockLastQpc - frame.clockFirstQpc;
    const double uncertainty = static_cast<double>(span) * 500000.0 / static_cast<double>(frequency) + 1.0;
    if (uncertainty > 100.0) return std::nullopt; // Reject preempted calibration calls.
    const auto midpoint = frame.clockFirstQpc + span / 2;
    const long double mapped = static_cast<long double>(frame.gameInputTime)
        + static_cast<long double>(displayQpc - midpoint) * 1000000.0L / static_cast<long double>(frequency);
    if (mapped < 0 || mapped >= static_cast<long double>((std::numeric_limits<std::uint64_t>::max)())) return std::nullopt;
    return DisplayTimestamp{static_cast<std::uint64_t>(std::round(mapped)), uncertainty};
}

inline bool MatchesDisplayFrame(const DisplayFrameSubmission& frame, std::uint64_t presentQpc,
    std::uint32_t thread, std::uint64_t swapChain) noexcept
{
    return frame.thread == thread && frame.swapChain == swapChain
        && presentQpc >= frame.clockLastQpc && presentQpc <= frame.presentEndQpc;
}

inline std::optional<std::uint64_t> ReadingToDisplayDuration(const DisplayFrameInput& input,
    const DisplayFrameSubmission& frame, const DisplayTimestamp& display) noexcept
{
    if (input.reading > input.sampled || input.sampled > frame.gameInputTime || display.microseconds < frame.gameInputTime) return std::nullopt;
    return display.microseconds - input.reading;
}
