#include "DisplayTracker.h"
#include "Measurement.h"

#include "PresentMonTraceConsumer.hpp"
#include "ProviderConfiguration.h"
#include "PresentMonWarnings.h"
#include "ETW/Microsoft_Windows_DXGI.h"
#include "ETW/Microsoft_Windows_DxgKrnl.h"
#include "ETW/Microsoft_Windows_DxgKrnl_Win7.h"
#include "ETW/Microsoft_Windows_Dwm_Core.h"
#include "ETW/Microsoft_Windows_Dwm_Core_Win7.h"
#include "ETW/Microsoft_Windows_Win32k.h"
#include "ETW/Microsoft_Windows_D3D9.h"
#include "ETW/Microsoft_Windows_EventMetadata.h"
#include "ETW/Microsoft_Windows_Kernel_Process.h"
#include "ETW/NV_DD.h"

#include <chrono>
#include <objbase.h>
#include <deque>
#include <format>
#include <fstream>
#include <map>
#include <thread>

namespace
{
struct TraceCompletion
{
    std::uint64_t presentQpc{}, displayQpc{}, swapChain{};
    std::uint32_t thread{}, mode{};
    bool displayed{}, lost{};
};
struct SessionProperties
{
    EVENT_TRACE_PROPERTIES properties{};
    wchar_t name[128]{};
};
}

struct DisplayTracker::Implementation
{
    explicit Implementation(const std::filesystem::path& directory) : logDirectory(directory), consumer(32768) {}
    std::filesystem::path logDirectory;
    PMTraceConsumer consumer;
    SessionProperties properties;
    std::atomic<TRACEHANDLE> session{};
    TRACEHANDLE trace{INVALID_PROCESSTRACE_HANDLE};
    std::wstring name;
    std::uint64_t frequency{};
    std::uint32_t process{GetCurrentProcessId()};
    std::atomic<bool> active{}, stopping{}, loggingFailed{};
    std::atomic<std::uint32_t> error{};
    std::atomic<std::uint64_t> droppedSubmissions{}, droppedCompletions{}, droppedMeasurements{}, lostEvents{}, lostBuffers{};
    SingleProducerQueue<DisplayFrameSubmission, 4096> submissions;
    SingleProducerQueue<TraceCompletion, 65536> completions;
    SingleProducerQueue<DisplayMeasurement, 65536> measurements;
    std::thread traceThread, workerThread;
    std::vector<std::shared_ptr<PresentEvent>> decoded;
    mutable std::mutex snapshotMutex;
    DisplayTrackingSnapshot snapshot;

    static void CALLBACK RecordEvent(EVENT_RECORD* event) noexcept
    {
        auto& self = *static_cast<Implementation*>(event->UserContext);
        try {
            PMTraceConsumer::EventProcessingScope scope(self.consumer);
            if (!scope) return;
            const auto& provider = event->EventHeader.ProviderId;
            if (provider == Microsoft_Windows_DxgKrnl::GUID) self.consumer.HandleDXGKEvent(event);
            else if (provider == Microsoft_Windows_DXGI::GUID) self.consumer.HandleDXGIEvent(event);
            else if (provider == Microsoft_Windows_Win32k::GUID) self.consumer.HandleWin32kEvent(event);
            else if (provider == Microsoft_Windows_Dwm_Core::GUID || provider == Microsoft_Windows_Dwm_Core::Win7::GUID) self.consumer.HandleDWMEvent(event);
            else if (provider == Microsoft_Windows_D3D9::GUID) self.consumer.HandleD3D9Event(event);
            else if (provider == Microsoft_Windows_EventMetadata::GUID) self.consumer.HandleMetadataEvent(event);
            else if (provider == Microsoft_Windows_Kernel_Process::GUID) self.consumer.HandleProcessEvent(event);
            else if (provider == NvidiaDisplayDriver_Events::GUID) self.consumer.HandleNvidiaDisplayDriverEvent(event);
            else if (provider == Microsoft_Windows_DxgKrnl::Win7::PRESENTHISTORY_GUID) self.consumer.HandleWin7DxgkPresentHistory(event);
            else if (provider == Microsoft_Windows_DxgKrnl::Win7::BLT_GUID) self.consumer.HandleWin7DxgkBlt(event);
            else if (provider == Microsoft_Windows_DxgKrnl::Win7::FLIP_GUID) self.consumer.HandleWin7DxgkFlip(event);
            else if (provider == Microsoft_Windows_DxgKrnl::Win7::QUEUEPACKET_GUID) self.consumer.HandleWin7DxgkQueuePacket(event);
            else if (provider == Microsoft_Windows_DxgKrnl::Win7::VSYNCDPC_GUID) self.consumer.HandleWin7DxgkVSyncDPC(event);
            else if (provider == Microsoft_Windows_DxgKrnl::Win7::MMIOFLIP_GUID) self.consumer.HandleWin7DxgkMMIOFlip(event);
            self.consumer.DequeuePresentEvents(self.decoded);
            for (const auto& present : self.decoded) {
                if (present->ProcessId != self.process || present->Runtime != Runtime::DXGI) continue;
                TraceCompletion completion{present->PresentStartTime, 0, present->SwapChainAddress,
                    present->ThreadId, static_cast<std::uint32_t>(present->PresentMode),
                    present->FinalState == PresentResult::Presented, present->IsLost};
                for (const auto& display : present->Displayed) {
                    if (display.second && (!completion.displayQpc || display.second < completion.displayQpc)) completion.displayQpc = display.second;
                }
                if (!self.completions.Push(completion)) ++self.droppedCompletions;
            }
            self.decoded.clear();
        }
        catch (...) { ++PresentMonWarningCount; }
    }

    void StartTraceSession()
    {
        LARGE_INTEGER counterFrequency{};
        if (!QueryPerformanceFrequency(&counterFrequency) || counterFrequency.QuadPart <= 0) { error = ERROR_INVALID_DATA; return; }
        frequency = static_cast<std::uint64_t>(counterFrequency.QuadPart);
        name = std::format(L"InputLatencyDisplay-{}-{}", process, GetTickCount64());
        auto& configuration = properties.properties;
        configuration.Wnode.BufferSize = sizeof(properties);
        configuration.Wnode.Flags = WNODE_FLAG_TRACED_GUID;
        configuration.Wnode.ClientContext = 1; // Raw QPC timestamps.
        if (FAILED(CoCreateGuid(&configuration.Wnode.Guid))) { error = ERROR_INVALID_DATA; return; }
        configuration.BufferSize = 64;
        configuration.MinimumBuffers = 16;
        configuration.MaximumBuffers = 128;
        configuration.FlushTimer = 1;
        configuration.LogFileMode = EVENT_TRACE_REAL_TIME_MODE;
        configuration.LoggerNameOffset = offsetof(SessionProperties, name);
        TRACEHANDLE createdSession{};
        auto status = StartTraceW(&createdSession, name.c_str(), &configuration);
        session.store(createdSession);
        if (status != ERROR_SUCCESS) { error = status; session = 0; return; }
        consumer.mFilteredEvents = true;
        consumer.mFilteredProcessIds = true;
        consumer.mDeferralTimeLimit = frequency * 2;
        consumer.AddTrackedProcessForFiltering(process);
        status = EnableProvidersListing(session, &configuration.Wnode.Guid, &consumer, true, true);
        if (status != ERROR_SUCCESS) { error = status; StopTraceSession(); return; }
        EVENT_TRACE_LOGFILEW logfile{};
        logfile.LoggerName = name.data();
        logfile.ProcessTraceMode = PROCESS_TRACE_MODE_REAL_TIME | PROCESS_TRACE_MODE_EVENT_RECORD | PROCESS_TRACE_MODE_RAW_TIMESTAMP;
        logfile.EventRecordCallback = RecordEvent;
        logfile.Context = this;
        trace = OpenTraceW(&logfile);
        if (trace == INVALID_PROCESSTRACE_HANDLE) { error = GetLastError(); StopTraceSession(); return; }
        active.store(true, std::memory_order_release);
        traceThread = std::thread([this] {
            SetThreadDescription(GetCurrentThread(), L"InputLatency display event decoding");
            const auto statusCode = ProcessTrace(&trace, 1, nullptr, nullptr);
            if (statusCode != ERROR_SUCCESS && statusCode != ERROR_CANCELLED) error.store(statusCode);
            active.store(false, std::memory_order_release);
        });
    }

    void StopTraceSession() noexcept
    {
        if (session) {
            // Stopping our uniquely named session flushes pending ETW buffers.
            auto finalProperties = properties;
            const auto result = ControlTraceW(session.load(), name.c_str(), &finalProperties.properties, EVENT_TRACE_CONTROL_STOP);
            if (result == ERROR_SUCCESS) {
                lostEvents.store(finalProperties.properties.EventsLost);
                lostBuffers.store(static_cast<std::uint64_t>(finalProperties.properties.LogBuffersLost) + finalProperties.properties.RealTimeBuffersLost);
            }
            session = 0;
        }
        if (traceThread.joinable()) traceThread.join();
        if (trace != INVALID_PROCESSTRACE_HANDLE) { CloseTrace(trace); trace = INVALID_PROCESSTRACE_HANDLE; }
        active.store(false, std::memory_order_release);
    }

    void Worker() noexcept
    {
        try {
            SetThreadDescription(GetCurrentThread(), L"InputLatency frame to display correlation");
            std::ofstream frames(logDirectory / L"DisplayFrames.csv"), inputs(logDirectory / L"DisplayReadings.csv");
            frames.exceptions(std::ios::failbit | std::ios::badbit); inputs.exceptions(std::ios::failbit | std::ios::badbit);
            frames << "Frame,PresentStartQpc,DisplayQpc,DisplayTimestampUs,ClockUncertaintyUs,PresentMode,Status\n";
            inputs << "Device,Frame,ReadingSerial,ReadingTimestampUs,SampleTimestampUs,DisplayQpc,DisplayTimestampUs,ReadingToDisplayUs,ClockUncertaintyUs,FirstDisplayForReading,Valid\n";
            std::map<std::uint64_t, DisplayFrameSubmission> pending;
            std::deque<TraceCompletion> waiting;
            std::array<std::uint64_t, MaximumDisplayInputs> lastDisplayedSerial{};
            DisplayTrackingSnapshot value;
            auto nextPublish = std::chrono::steady_clock::now();
            auto nextQuery = nextPublish;
            std::optional<DisplayFrameSubmission> previousAnchor;
            bool clockRateValid = true;
            const auto publish = [&] {
                value.active = active.load(); value.error = error.load(); value.loggingFailed = loggingFailed.load();
                value.lostEvents = lostEvents.load(); value.lostBuffers = lostBuffers.load();
                value.droppedSubmissions = droppedSubmissions.load(); value.droppedCompletions = droppedCompletions.load();
                value.droppedMeasurements = droppedMeasurements.load(); value.decoderWarnings = PresentMonWarningCount.load();
                value.decoderOverflows = consumer.GetNumOverflowedPresents();
                if (value.error == ERROR_ACCESS_DENIED) value.status = "Display tracking unavailable: run Run.cmd as administrator (error 5).";
                else if (value.error) value.status = std::format("Display tracking unavailable: Windows error {}.", value.error);
                else if (value.lostEvents || value.lostBuffers || value.droppedSubmissions || value.droppedCompletions || value.droppedMeasurements || value.decoderOverflows)
                    value.status = "Display tracking incomplete: lost records; inspect DisplayDiagnostics.txt.";
                else value.status = std::format("Display: {} shown, {} discarded, {} unresolved | Clock errors {} | <= {:.1f} us",
                    value.displayed, value.discarded, value.unresolved, value.invalidClocks, value.maximumClockUncertainty);
                std::lock_guard guard(snapshotMutex); snapshot = value;
            };
            const auto unknown = [&](const DisplayFrameSubmission& frame, const char* reason) {
                ++value.unresolved; frames << frame.frame << ",,,,,," << reason << '\n';
            };
            for (;;) {
                bool work = false;
                DisplayFrameSubmission frame;
                for (std::size_t batch = 0; batch < 4096 && submissions.Pop(frame); ++batch) {
                    work = true; ++value.submitted;
                    if (!frame.accepted) { frames << frame.frame << ",,,,,,PresentNotAccepted\n"; continue; }
                    if (previousAnchor && frame.clockFirstQpc > previousAnchor->clockLastQpc + frequency) {
                        const auto check = MapDisplayTimestamp(*previousAnchor, frame.clockFirstQpc + (frame.clockLastQpc - frame.clockFirstQpc) / 2, frequency);
                        clockRateValid = check && std::abs(static_cast<double>(check->microseconds) - static_cast<double>(frame.gameInputTime)) <= 200.0;
                        if (!clockRateValid) ++value.invalidClocks;
                        previousAnchor = frame;
                    }
                    if (!previousAnchor) previousAnchor = frame;
                    if (pending.size() >= 32768) { unknown(pending.begin()->second, "CorrelationCapacityExceeded"); pending.erase(pending.begin()); }
                    pending.emplace(frame.clockLastQpc, frame);
                }
                TraceCompletion completion;
                for (std::size_t batch = 0; batch < 8192 && completions.Pop(completion); ++batch) { work = true; waiting.push_back(completion); }
                LARGE_INTEGER currentCounter{}; QueryPerformanceCounter(&currentCounter);
                const auto nowQpc = static_cast<std::uint64_t>(currentCounter.QuadPart);
                for (auto iterator = waiting.begin(); iterator != waiting.end();) {
                    auto match = pending.upper_bound(iterator->presentQpc);
                    if (match != pending.begin()) --match;
                    if (match == pending.end() || !MatchesDisplayFrame(match->second, iterator->presentQpc, iterator->thread, iterator->swapChain)) {
                        if (stopping.load() || nowQpc > iterator->presentQpc + frequency * 5) { ++value.unmatched; iterator = waiting.erase(iterator); }
                        else ++iterator;
                        continue;
                    }
                    const auto& submitted = match->second;
                    const auto mapped = iterator->displayed && !iterator->lost && iterator->displayQpc
                        ? MapDisplayTimestamp(submitted, iterator->displayQpc, frequency) : std::nullopt;
                    const bool reliable = mapped && clockRateValid && !lostEvents.load() && !lostBuffers.load();
                    const char* status = iterator->lost ? "TraceLost" : !iterator->displayed ? "Discarded"
                        : !reliable ? "InvalidClockOrTraceLoss" : "Displayed";
                    if (iterator->lost) ++value.unresolved;
                    else if (!iterator->displayed) ++value.discarded;
                    else { ++value.displayed; if (!reliable) ++value.invalidClocks; }
                    frames << submitted.frame << ',' << iterator->presentQpc << ',' << iterator->displayQpc << ',';
                    if (mapped) { frames << mapped->microseconds << ',' << mapped->uncertaintyMicroseconds; value.maximumClockUncertainty = std::max(value.maximumClockUncertainty, mapped->uncertaintyMicroseconds); }
                    else frames << ',';
                    frames << ',' << iterator->mode << ',' << status << '\n';
                    if (reliable) {
                        for (std::size_t index = 0; index < submitted.inputCount; ++index) {
                            const auto& input = submitted.inputs[index];
                            const auto elapsed = ReadingToDisplayDuration(input, submitted, *mapped);
                            const bool firstDisplay = input.serial > lastDisplayedSerial[input.device];
                            const bool valid = elapsed.has_value();
                            inputs << input.device << ',' << submitted.frame << ',' << input.serial << ',' << input.reading << ',' << input.sampled
                                << ',' << iterator->displayQpc << ',' << mapped->microseconds << ',';
                            if (elapsed) inputs << *elapsed;
                            inputs << ',' << mapped->uncertaintyMicroseconds << ',' << firstDisplay << ',' << valid << '\n';
                            if (firstDisplay && valid) {
                                lastDisplayedSerial[input.device] = input.serial;
                                const DisplayMeasurement measurement{input.device, submitted.frame, input.reading, mapped->microseconds, *elapsed};
                                if (!measurements.Push(measurement)) ++droppedMeasurements;
                            }
                            if (!valid) ++value.invalidClocks;
                        }
                    }
                    pending.erase(match); iterator = waiting.erase(iterator); work = true;
                }
                while (!pending.empty() && (stopping.load() || nowQpc > pending.begin()->first + frequency * 5)) {
                    unknown(pending.begin()->second, "NoDisplayEvent"); pending.erase(pending.begin());
                }
                const auto now = std::chrono::steady_clock::now();
                if (now >= nextQuery && session) {
                    SessionProperties statistics = properties;
                    if (ControlTraceW(session.load(), name.c_str(), &statistics.properties, EVENT_TRACE_CONTROL_QUERY) == ERROR_SUCCESS) {
                        lostEvents.store(statistics.properties.EventsLost);
                        lostBuffers.store(static_cast<std::uint64_t>(statistics.properties.LogBuffersLost) + statistics.properties.RealTimeBuffersLost);
                    }
                    nextQuery = now + std::chrono::seconds(1);
                }
                if (now >= nextPublish) { publish(); nextPublish = now + std::chrono::milliseconds(250); }
                if (stopping.load() && !work && pending.empty() && waiting.empty()) break;
                if (!work) std::this_thread::sleep_for(std::chrono::milliseconds(2));
            }
            publish();
            std::ofstream diagnostics(logDirectory / L"DisplayDiagnostics.txt");
            diagnostics.exceptions(std::ios::failbit | std::ios::badbit);
            diagnostics << "TimestampSource=PresentMon v2.6.0 Windows display events\nQpcFrequency=" << frequency
                << "\nStartOrProcessingError=" << value.error << "\nSubmittedFrames=" << value.submitted << "\nDisplayedFrames=" << value.displayed
                << "\nDiscardedFrames=" << value.discarded << "\nUnresolvedFrames=" << value.unresolved << "\nUnmatchedEvents=" << value.unmatched
                << "\nEtwEventsLost=" << value.lostEvents << "\nEtwBuffersLost=" << value.lostBuffers
                << "\nDroppedSubmissions=" << value.droppedSubmissions << "\nDroppedCompletions=" << value.droppedCompletions
                << "\nDroppedMeasurements=" << value.droppedMeasurements << "\nInvalidClocks=" << value.invalidClocks
                << "\nDecoderWarnings=" << value.decoderWarnings << "\nDecoderOverflows=" << value.decoderOverflows
                << "\nMaximumClockUncertaintyUs=" << value.maximumClockUncertainty << '\n';
        }
        catch (...) { loggingFailed.store(true); std::lock_guard guard(snapshotMutex); snapshot.loggingFailed = true; }
    }
};

DisplayTracker::DisplayTracker(const std::filesystem::path& directory) : implementation(std::make_unique<Implementation>(directory)) {}
DisplayTracker::~DisplayTracker() { Stop(); }
void DisplayTracker::Start()
{
    implementation->StartTraceSession();
    implementation->workerThread = std::thread([self = implementation.get()] { self->Worker(); });
}
void DisplayTracker::Stop() noexcept
{
    if (!implementation->workerThread.joinable() && !implementation->traceThread.joinable()) return;
    // Shutdown only: let the final submitted image reach its presentation
    // boundary before disabling providers. Rendering never waits for tracing.
    if (implementation->active.load()) std::this_thread::sleep_for(std::chrono::milliseconds(100));
    implementation->StopTraceSession();
    implementation->stopping.store(true);
    if (implementation->workerThread.joinable()) implementation->workerThread.join();
}
void DisplayTracker::Submit(const DisplayFrameSubmission& frame) noexcept
{
    if (implementation->active.load(std::memory_order_acquire) && !implementation->submissions.Push(frame)) ++implementation->droppedSubmissions;
}
bool DisplayTracker::PopMeasurement(DisplayMeasurement& measurement) noexcept { return implementation->measurements.Pop(measurement); }
DisplayTrackingSnapshot DisplayTracker::Snapshot() const { std::lock_guard guard(implementation->snapshotMutex); return implementation->snapshot; }
