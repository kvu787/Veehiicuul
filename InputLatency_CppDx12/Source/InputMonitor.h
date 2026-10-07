#pragma once

#include <windows.h>
#include <GameInput.h>
#include <wrl/client.h>

#include "Measurement.h"
#include "ControllerState.h"
#include "DisplayTracker.h"

#include <filesystem>
#include <mutex>
#include <string>
#include <thread>
#include <vector>

namespace Input = GameInput::v3;
inline constexpr std::size_t MaximumDevices = 128;
inline constexpr Input::GameInputKind MeasuredKinds = static_cast<Input::GameInputKind>(
    Input::GameInputKindMouse | Input::GameInputKindKeyboard | Input::GameInputKindGamepad);

struct DeviceSlot
{
    Microsoft::WRL::ComPtr<Input::IGameInputDevice> device;
    std::string name;
    std::string identifier;
    Input::GameInputKind kinds{};
    std::uint16_t vendor{}, product{};
    std::atomic<bool> connected{};
    // Only GameInput's serialized callback producer accesses this value.
    std::uint64_t previousCallbackTimestamp{};
    std::uint64_t callbackFocusEpoch{};
};

struct DeviceSnapshot
{
    std::string name, identifier, kind;
    std::uint16_t vendor{}, product{};
    bool connected{}, gamepad{};
    StatisticSnapshot callbackDelay;
    StatisticSnapshot inputInterval;
    StatisticSnapshot sampleDelay;
    StatisticSnapshot presentDelay;
    StatisticSnapshot presentCallDuration;
    StatisticSnapshot displayDelay;
};

struct MonitorSnapshot
{
    std::vector<DeviceSnapshot> devices;
    std::uint64_t droppedCallbacks{}, droppedFrames{}, invalidTimestamps{}, deviceLimitEvents{}, pollErrors{};
    bool loggingFailed{};
    DisplayTrackingSnapshot displayTracking;
};

struct VisualState
{
    bool connected{}, active{}, newReading{}, visualized{};
    std::uint32_t buttons{}, keys{};
    std::int64_t mouseX{}, mouseY{};
    ControllerAnalogState controller;
    bool controllerReadingAvailable{};
    std::uint64_t timestamp{}, sampledAt{};
    std::uint64_t readingSerial{};
    bool measurementEligible{};
    Input::GameInputKind kind{};
};

class InputMonitor final
{
public:
    InputMonitor() = default;
    ~InputMonitor();
    void Initialize(const std::filesystem::path& executableDirectory, const std::filesystem::path& logDirectory);
    void Stop();
    void SetForeground(bool value) noexcept;
    std::uint64_t Now() const noexcept { return gameInput->GetCurrentTimestamp(); }
    std::size_t DeviceCount() const noexcept { return deviceCount.load(std::memory_order_acquire); }
    const DeviceSlot& Device(std::size_t index) const noexcept { return devices[index]; }
    void SampleLatest(std::array<VisualState, MaximumDevices>& states);
    void RecordPresentation(const std::array<VisualState, MaximumDevices>& states,
        std::uint64_t frame, std::uint64_t beginning, std::uint64_t ending, bool presented,
        std::uint64_t firstQpc, std::uint64_t lastQpc, std::uint64_t endQpc, std::uint64_t swapChain, std::uint32_t thread) noexcept;
    MonitorSnapshot Snapshot() const;
private:
    enum class EventType : std::uint8_t { Reading, Connection, Presentation };
    struct Event
    {
        EventType type{};
        std::uint16_t device{};
        bool foreground{}, connected{}, presented{}, visualized{};
        std::uint64_t reading{}, observed{}, previous{}, presentBeginning{}, presentEnding{}, frame{};
    };
    static void CALLBACK DeviceCallback(Input::GameInputCallbackToken, void*, Input::IGameInputDevice*,
        std::uint64_t, Input::GameInputDeviceStatus, Input::GameInputDeviceStatus) noexcept;
    static void CALLBACK ReadingCallback(Input::GameInputCallbackToken, void*, Input::IGameInputReading*) noexcept;
    void LogWorker(const std::filesystem::path& logDirectory) noexcept;
    HMODULE runtimeModule{};
    Microsoft::WRL::ComPtr<Input::IGameInput> gameInput;
    Input::GameInputCallbackToken deviceToken{}, readingToken{};
    bool deviceRegistered{}, readingRegistered{};
    std::array<DeviceSlot, MaximumDevices> devices;
    std::array<Microsoft::WRL::ComPtr<IUnknown>, MaximumDevices> previousFrameReadings;
    std::atomic<std::size_t> deviceCount{};
    std::uint64_t sessionBeginning{};
    std::atomic<std::uint64_t> focusBeginning{}, focusEpoch{};
    SingleProducerQueue<Event, 65536> callbackEvents;
    SingleProducerQueue<Event, 65536> frameEvents;
    std::atomic<std::uint64_t> droppedCallbacks{}, droppedFrames{}, deviceLimitEvents{}, pollErrors{};
    std::atomic<bool> foreground{}, stopping{}, loggingFailed{};
    std::thread loggingThread;
    std::unique_ptr<DisplayTracker> displayTracker;
    mutable std::mutex snapshotMutex;
    MonitorSnapshot publishedSnapshot;
};

std::string KindName(Input::GameInputKind kind);
