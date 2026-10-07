#pragma once
#include "DisplayTiming.h"
#include <filesystem>
#include <memory>
#include <string>

struct DisplayMeasurement
{
    std::uint16_t device{};
    std::uint64_t frame{}, reading{}, display{}, duration{};
};
struct DisplayTrackingSnapshot
{
    bool active{}, loggingFailed{};
    std::uint32_t error{};
    std::uint64_t submitted{}, displayed{}, discarded{}, unresolved{}, unmatched{}, lostEvents{}, lostBuffers{},
        droppedSubmissions{}, droppedCompletions{}, droppedMeasurements{}, invalidClocks{}, decoderWarnings{}, decoderOverflows{};
    double maximumClockUncertainty{};
    std::string status;
};

class DisplayTracker final
{
public:
    explicit DisplayTracker(const std::filesystem::path& logDirectory);
    ~DisplayTracker();
    void Start();
    void Stop() noexcept;
    void Submit(const DisplayFrameSubmission& frame) noexcept;
    bool PopMeasurement(DisplayMeasurement& measurement) noexcept;
    DisplayTrackingSnapshot Snapshot() const;
private:
    struct Implementation;
    std::unique_ptr<Implementation> implementation;
};
