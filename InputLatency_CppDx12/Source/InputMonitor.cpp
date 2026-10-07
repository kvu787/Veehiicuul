#include "InputMonitor.h"

#include <chrono>
#include <format>
#include <fstream>
#include <iomanip>
#include <sstream>
#include <stdexcept>

using Microsoft::WRL::ComPtr;

namespace
{
void Check(HRESULT result, const char* operation)
{
    if (FAILED(result)) throw std::runtime_error(std::format("{} failed: 0x{:08X}", operation, static_cast<unsigned long>(result)));
}

std::string CsvString(const std::string& value)
{
    std::string result = "\"";
    for (char character : value) { if (character == '"') result += '"'; result += character; }
    return result + '"';
}
}

std::string KindName(Input::GameInputKind kind)
{
    std::string result;
    if (kind & Input::GameInputKindMouse) result = "Mouse";
    if (kind & Input::GameInputKindKeyboard) result += result.empty() ? "Keyboard" : "+Keyboard";
    if (kind & Input::GameInputKindGamepad) result += result.empty() ? "Gamepad" : "+Gamepad";
    return result;
}

InputMonitor::~InputMonitor()
{
    Stop();
    for (auto& reading : previousFrameReadings) reading.Reset();
    for (auto& slot : devices) slot.device.Reset();
    gameInput.Reset();
    if (runtimeModule) FreeLibrary(runtimeModule);
}

void InputMonitor::Initialize(const std::filesystem::path& executableDirectory, const std::filesystem::path& logDirectory)
{
    const auto runtimePath = executableDirectory / L"GameInputRedist.dll";
    // Load the bundled runtime explicitly; do not silently select an installed
    // runtime of another version through the SDK's version-selecting loader.
    runtimeModule = LoadLibraryExW(runtimePath.c_str(), nullptr,
        LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR | LOAD_LIBRARY_SEARCH_SYSTEM32);
    if (!runtimeModule) throw std::runtime_error(std::format("Cannot load bundled GameInputRedist.dll (Windows error {}). Rebuild the application.", GetLastError()));
    using InitializeFunction = HRESULT (WINAPI*)(REFIID, void**);
    const auto initialize = reinterpret_cast<InitializeFunction>(GetProcAddress(runtimeModule, "GameInputInitialize"));
    if (!initialize) throw std::runtime_error("The bundled GameInput runtime does not export GameInputInitialize.");
    Check(initialize(Input::IID_IGameInput, reinterpret_cast<void**>(gameInput.GetAddressOf())), "GameInputInitialize v3");
    gameInput->SetFocusPolicy(Input::GameInputDefaultFocusPolicy);
    sessionBeginning = Now();
    focusBeginning.store(sessionBeginning, std::memory_order_relaxed);
    // The logger must be started before registering callbacks. All its writes
    // and percentile sorting run outside both the input and rendering paths.
    loggingThread = std::thread(&InputMonitor::LogWorker, this, logDirectory);
    Check(gameInput->RegisterDeviceCallback(nullptr, MeasuredKinds, Input::GameInputDeviceConnected,
        Input::GameInputBlockingEnumeration, this, DeviceCallback, &deviceToken), "RegisterDeviceCallback");
    deviceRegistered = true;
    Check(gameInput->RegisterReadingCallback(nullptr, MeasuredKinds, this, ReadingCallback, &readingToken), "RegisterReadingCallback");
    readingRegistered = true;
}

void InputMonitor::SetForeground(bool value) noexcept
{
    if (foreground.load(std::memory_order_relaxed) == value) return;
    focusBeginning.store(Now(), std::memory_order_relaxed);
    foreground.store(value, std::memory_order_relaxed);
    focusEpoch.fetch_add(1, std::memory_order_relaxed);
    for (auto& reading : previousFrameReadings) reading.Reset();
}

void InputMonitor::Stop()
{
    if (gameInput && readingRegistered) {
        gameInput->StopCallback(readingToken);
        gameInput->UnregisterCallback(readingToken);
        readingRegistered = false;
    }
    if (gameInput && deviceRegistered) {
        gameInput->StopCallback(deviceToken);
        gameInput->UnregisterCallback(deviceToken);
        deviceRegistered = false;
    }
    stopping.store(true, std::memory_order_release);
    if (loggingThread.joinable()) loggingThread.join();
}

void CALLBACK InputMonitor::DeviceCallback(Input::GameInputCallbackToken, void* context, Input::IGameInputDevice* device,
    std::uint64_t timestamp, Input::GameInputDeviceStatus current, Input::GameInputDeviceStatus) noexcept
{
    auto& monitor = *static_cast<InputMonitor*>(context);
    try {
        std::size_t index = 0;
        const auto count = monitor.DeviceCount();
        while (index < count && monitor.devices[index].device.Get() != device) ++index;
        if (index == count) {
            if (count == MaximumDevices) { ++monitor.deviceLimitEvents; return; }
            const Input::GameInputDeviceInfo* information{};
            if (FAILED(device->GetDeviceInfo(&information)) || !information) return;
            auto& slot = monitor.devices[index];
            slot.device = device;
            slot.name = information->displayName && information->displayName[0] ? information->displayName : KindName(information->supportedInput);
            slot.kinds = information->supportedInput & MeasuredKinds;
            slot.vendor = information->vendorId;
            slot.product = information->productId;
            for (const auto byte : information->deviceId.value) slot.identifier += std::format("{:02X}", byte);
            slot.connected.store((current & Input::GameInputDeviceConnected) != 0, std::memory_order_relaxed);
            monitor.deviceCount.store(count + 1, std::memory_order_release);
        }
        auto& slot = monitor.devices[index];
        const bool connected = (current & Input::GameInputDeviceConnected) != 0;
        slot.connected.store(connected, std::memory_order_relaxed);
        slot.previousCallbackTimestamp = 0;
        const Event event{.type = EventType::Connection, .device = static_cast<std::uint16_t>(index),
            .connected = connected, .observed = timestamp};
        if (!monitor.callbackEvents.Push(event)) ++monitor.droppedCallbacks;
    }
    catch (...) { ++monitor.deviceLimitEvents; }
}

void CALLBACK InputMonitor::ReadingCallback(Input::GameInputCallbackToken, void* context, Input::IGameInputReading* reading) noexcept
{
    auto& monitor = *static_cast<InputMonitor*>(context);
    // Capture before resolving device identity or doing any other work.
    const auto observed = monitor.Now();
    const auto timestamp = reading->GetTimestamp();
    if (timestamp < monitor.sessionBeginning) return;
    ComPtr<Input::IGameInputDevice> device;
    reading->GetDevice(&device);
    const auto count = monitor.DeviceCount();
    for (std::size_t index = 0; index < count; ++index) {
        auto& slot = monitor.devices[index];
        if (slot.device.Get() != device.Get()) continue;
        const bool isForeground = monitor.foreground.load(std::memory_order_relaxed)
            && timestamp >= monitor.focusBeginning.load(std::memory_order_relaxed);
        const auto epoch = monitor.focusEpoch.load(std::memory_order_relaxed);
        if (slot.callbackFocusEpoch != epoch) { slot.previousCallbackTimestamp = 0; slot.callbackFocusEpoch = epoch; }
        const Event event{.type = EventType::Reading, .device = static_cast<std::uint16_t>(index), .foreground = isForeground,
            .reading = timestamp, .observed = observed, .previous = slot.previousCallbackTimestamp};
        slot.previousCallbackTimestamp = isForeground ? timestamp : 0;
        if (!monitor.callbackEvents.Push(event)) ++monitor.droppedCallbacks;
        return;
    }
}

void InputMonitor::SampleLatest(std::array<VisualState, MaximumDevices>& states)
{
    const auto count = DeviceCount();
    for (std::size_t index = 0; index < count; ++index) {
        auto& state = states[index];
        state.newReading = false;
        const auto& slot = devices[index];
        state.connected = slot.connected.load(std::memory_order_relaxed);
        if (!state.connected) {
            state.active = false; state.controller = {}; state.controllerReadingAvailable = false;
            previousFrameReadings[index].Reset(); continue;
        }
        ComPtr<Input::IGameInputReading> reading;
        // Composite controllers can advertise mouse/keyboard input as well.
        // Request their gamepad stream so the analog panel gets gamepad state.
        const auto requestedKinds = slot.kinds & Input::GameInputKindGamepad ? Input::GameInputKindGamepad : slot.kinds;
        const auto result = gameInput->GetCurrentReading(requestedKinds, slot.device.Get(), &reading);
        const auto sampled = Now();
        if (FAILED(result)) {
            if (result != Input::GAMEINPUT_E_READING_NOT_FOUND && result != Input::GAMEINPUT_E_DEVICE_DISCONNECTED) ++pollErrors;
            state.controller = {}; state.controllerReadingAvailable = false;
            previousFrameReadings[index].Reset();
            continue;
        }
        ComPtr<IUnknown> identity;
        if (FAILED(reading.As(&identity))) { ++pollErrors; continue; }
        const bool changed = identity.Get() != previousFrameReadings[index].Get();
        const bool hadBaseline = previousFrameReadings[index] != nullptr;
        previousFrameReadings[index] = identity;
        if (!changed) continue;
        state.timestamp = reading->GetTimestamp();
        state.sampledAt = sampled;
        state.kind = reading->GetInputKind();
        // Establish a baseline instead of measuring a reading cached before
        // connection/focus. Held state is still shown immediately.
        state.newReading = hadBaseline && state.timestamp >= focusBeginning.load(std::memory_order_relaxed) && foreground.load(std::memory_order_relaxed);
        state.buttons = 0;
        state.keys = reading->GetKeyCount();
        Input::GameInputMouseState mouse{};
        if (reading->GetMouseState(&mouse)) {
            state.buttons |= mouse.buttons;
            state.mouseX = mouse.positionX;
            state.mouseY = mouse.positionY;
        }
        Input::GameInputGamepadState gamepad{};
        state.controllerReadingAvailable = reading->GetGamepadState(&gamepad);
        state.controller = {};
        if (state.controllerReadingAvailable) {
            state.buttons |= gamepad.buttons;
            state.controller = {gamepad.leftThumbstickX, gamepad.leftThumbstickY,
                gamepad.rightThumbstickX, gamepad.rightThumbstickY, gamepad.leftTrigger, gamepad.rightTrigger};
        }
        state.active = state.buttons != 0 || state.keys != 0;
    }
}

void InputMonitor::RecordPresentation(const std::array<VisualState, MaximumDevices>& states,
    std::uint64_t frame, std::uint64_t beginning, std::uint64_t ending, bool presented) noexcept
{
    const auto count = DeviceCount();
    for (std::size_t index = 0; index < count; ++index) {
        const auto& state = states[index];
        if (!state.newReading) continue;
        const Event event{.type = EventType::Presentation, .device = static_cast<std::uint16_t>(index),
            .foreground = true, .presented = presented, .visualized = state.visualized, .reading = state.timestamp, .observed = state.sampledAt,
            .presentBeginning = beginning, .presentEnding = ending, .frame = frame};
        if (!frameEvents.Push(event)) ++droppedFrames;
    }
}

MonitorSnapshot InputMonitor::Snapshot() const
{
    std::lock_guard guard(snapshotMutex);
    return publishedSnapshot;
}

void InputMonitor::LogWorker(const std::filesystem::path& logDirectory) noexcept
{
    try {
        SetThreadDescription(GetCurrentThread(), L"InputLatency statistics and logging");
        struct DeviceStatistics { Statistics callback, interval, sample, present, presentCall; };
        std::vector<DeviceStatistics> statistics;
        std::ofstream readings(logDirectory / L"Readings.csv");
        std::ofstream presentations(logDirectory / L"Presentations.csv");
        std::ofstream connections(logDirectory / L"Devices.csv");
        for (auto* stream : {&readings, &presentations, &connections}) {
            stream->exceptions(std::ios::badbit | std::ios::failbit);
        }
        readings << "Device,ReadingTimestampUs,CallbackTimestampUs,CallbackDelayUs,ChangeIntervalUs,Foreground,ValidTimestamp\n";
        presentations << "Device,Frame,ReadingTimestampUs,SampleTimestampUs,PresentBeginTimestampUs,PresentEndTimestampUs,SampleDelayUs,PresentBeginDelayUs,PresentCallDurationUs,PresentAccepted,Visualized,ValidTimestamp\n";
        connections << "Device,TimestampUs,Connected,Kind,VendorId,ProductId,DeviceId,Name\n";
        std::uint64_t invalidTimestamps = 0;
        auto nextSnapshot = std::chrono::steady_clock::now();
        const auto ensureDevices = [&] {
            const auto count = DeviceCount();
            if (statistics.size() < count) statistics.resize(count);
        };
        const auto publish = [&] {
            ensureDevices();
            MonitorSnapshot snapshot;
            for (std::size_t index = 0; index < statistics.size(); ++index) {
                const auto& slot = devices[index];
                const auto& value = statistics[index];
                snapshot.devices.push_back({slot.name, slot.identifier, KindName(slot.kinds), slot.vendor, slot.product,
                    slot.connected.load(std::memory_order_relaxed), (slot.kinds & Input::GameInputKindGamepad) != 0,
                    value.callback.Snapshot(), value.interval.Snapshot(),
                    value.sample.Snapshot(), value.present.Snapshot(), value.presentCall.Snapshot()});
            }
            snapshot.droppedCallbacks = droppedCallbacks.load(std::memory_order_relaxed);
            snapshot.droppedFrames = droppedFrames.load(std::memory_order_relaxed);
            snapshot.invalidTimestamps = invalidTimestamps;
            snapshot.deviceLimitEvents = deviceLimitEvents.load(std::memory_order_relaxed);
            snapshot.pollErrors = pollErrors.load(std::memory_order_relaxed);
            snapshot.loggingFailed = loggingFailed.load(std::memory_order_relaxed);
            std::lock_guard guard(snapshotMutex);
            publishedSnapshot = std::move(snapshot);
        };
        for (;;) {
            ensureDevices();
            Event event;
            bool work = false;
            // Bounded batches prevent continuous mouse input from starving
            // presentation records, publication, or graceful shutdown.
            for (std::size_t batch = 0; batch < 8192 && callbackEvents.Pop(event); ++batch) {
                work = true;
                ensureDevices();
                const auto& slot = devices[event.device];
                auto& value = statistics[event.device];
                if (event.type == EventType::Connection) {
                    connections << event.device << ',' << event.observed << ',' << event.connected << ',' << KindName(slot.kinds)
                        << ',' << slot.vendor << ',' << slot.product << ',' << slot.identifier << ',' << CsvString(slot.name) << '\n';
                    continue;
                }
                const auto elapsed = ElapsedMicroseconds(event.reading, event.observed);
                const auto interval = event.previous ? ElapsedMicroseconds(event.previous, event.reading) : std::nullopt;
                if (!elapsed) ++invalidTimestamps;
                if (event.foreground && elapsed) value.callback.Add(*elapsed);
                if (event.foreground && interval) value.interval.Add(*interval);
                readings << event.device << ',' << event.reading << ',' << event.observed << ',';
                if (elapsed) readings << *elapsed;
                readings << ',';
                if (interval) readings << *interval;
                readings << ',' << event.foreground << ',' << elapsed.has_value() << '\n';
            }
            for (std::size_t batch = 0; batch < 8192 && frameEvents.Pop(event); ++batch) {
                work = true;
                ensureDevices();
                auto& value = statistics[event.device];
                const auto sample = ElapsedMicroseconds(event.reading, event.observed);
                const auto present = ElapsedMicroseconds(event.reading, event.presentBeginning);
                const auto duration = ElapsedMicroseconds(event.presentBeginning, event.presentEnding);
                const bool valid = sample && present && duration;
                if (!valid) ++invalidTimestamps;
                if (valid && event.presented) {
                    value.sample.Add(*sample);
                    if (event.visualized) { value.present.Add(*present); value.presentCall.Add(*duration); }
                }
                presentations << event.device << ',' << event.frame << ',' << event.reading << ',' << event.observed << ','
                    << event.presentBeginning << ',' << event.presentEnding << ',';
                if (sample) presentations << *sample;
                presentations << ',';
                if (present) presentations << *present;
                presentations << ',';
                if (duration) presentations << *duration;
                presentations << ',' << event.presented << ',' << event.visualized << ',' << valid << '\n';
            }
            const auto now = std::chrono::steady_clock::now();
            if (now >= nextSnapshot) { publish(); nextSnapshot = now + std::chrono::milliseconds(250); }
            if (!work && stopping.load(std::memory_order_acquire)) break;
            // Only the statistics thread sleeps. No frame or input pacing.
            if (!work) std::this_thread::sleep_for(std::chrono::milliseconds(2));
        }
        publish();
        std::ofstream summary(logDirectory / L"Summary.csv");
        summary.exceptions(std::ios::badbit | std::ios::failbit);
        summary << "Device,Kind,Name,Metric,Count,PercentileWindowCount,LastUs,MinimumUs,MeanUs,MaximumUs,P50Us,P95Us,P99Us\n";
        const auto finalSnapshot = Snapshot();
        for (std::size_t index = 0; index < finalSnapshot.devices.size(); ++index) {
            const auto& device = finalSnapshot.devices[index];
            const std::array<std::pair<const char*, StatisticSnapshot>, 5> metrics{{
                {"ReadingToCallback", device.callbackDelay}, {"StateChangeInterval", device.inputInterval},
                {"ReadingToLateSample", device.sampleDelay}, {"ReadingToPresentBegin", device.presentDelay},
                {"PresentCallDuration", device.presentCallDuration}}};
            for (const auto& [name, value] : metrics) {
                summary << index << ',' << device.kind << ',' << CsvString(device.name) << ',' << name << ',' << value.count
                    << ',' << value.percentileCount << ',' << value.last << ',' << value.minimum << ',' << value.mean << ','
                    << value.maximum << ',' << value.median << ',' << value.percentile95 << ',' << value.percentile99 << '\n';
            }
        }
        std::ofstream diagnostics(logDirectory / L"MeasurementDiagnostics.txt");
        diagnostics.exceptions(std::ios::badbit | std::ios::failbit);
        diagnostics << "DroppedCallbackRecords=" << finalSnapshot.droppedCallbacks << "\nDroppedPresentationRecords=" << finalSnapshot.droppedFrames
            << "\nInvalidTimestamps=" << invalidTimestamps << "\nDeviceLimitOrMetadataErrors=" << finalSnapshot.deviceLimitEvents
            << "\nUnexpectedPollingErrors=" << finalSnapshot.pollErrors << '\n';
        readings.flush(); presentations.flush(); connections.flush(); summary.flush(); diagnostics.flush();
    }
    catch (...) {
        loggingFailed.store(true, std::memory_order_relaxed);
        std::lock_guard guard(snapshotMutex);
        publishedSnapshot.loggingFailed = true;
    }
}
