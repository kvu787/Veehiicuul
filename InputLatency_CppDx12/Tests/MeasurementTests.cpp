#include "Measurement.h"

#include <iostream>
#include <numeric>
#include <stdexcept>
#include <thread>

namespace
{
void Require(bool condition, const char* message) { if (!condition) throw std::runtime_error(message); }
}

int main()
{
    try {
        Require(!ElapsedMicroseconds(100, 99), "Future timestamps must be rejected without unsigned underflow.");
        Require(ElapsedMicroseconds(100, 100) == 0, "Equal timestamps are valid distinct observations.");
        Require(ElapsedMicroseconds(7, 107) == 100, "Microsecond clock subtraction is wrong.");
        Statistics statistics;
        Require(statistics.Snapshot().count == 0, "Empty statistics must have no samples.");
        for (std::uint64_t value = 1; value <= 100; ++value) statistics.Add(value);
        const auto snapshot = statistics.Snapshot();
        Require(snapshot.count == 100 && snapshot.mean == 50.5 && snapshot.minimum == 1 && snapshot.maximum == 100,
            "Cumulative statistics are incorrect.");
        Require(snapshot.median == 50 && snapshot.percentile95 == 95 && snapshot.percentile99 == 99, "Nearest-rank percentiles are incorrect.");
        for (std::uint64_t index = 0; index < Statistics::WindowCapacity; ++index) statistics.Add(200);
        const auto rolling = statistics.Snapshot();
        Require(rolling.percentileCount == Statistics::WindowCapacity && rolling.median == 200 && rolling.percentile99 == 200,
            "Rolling percentiles retain expired values.");
        Require(rolling.minimum == 1 && rolling.count == 100 + Statistics::WindowCapacity, "Cumulative and window statistics were confused.");
        SingleProducerQueue<std::uint64_t, 4> small;
        std::uint64_t result{};
        Require(!small.Pop(result), "Empty queue must fail immediately.");
        for (std::uint64_t value = 0; value < 4; ++value) Require(small.Push(value), "Queue lost capacity.");
        Require(!small.Push(4), "Full queue must report overflow without overwriting.");
        for (std::uint64_t value = 0; value < 4; ++value) Require(small.Pop(result) && result == value, "Queue order is incorrect.");
        SingleProducerQueue<std::uint64_t, 1024> concurrent;
        constexpr std::uint64_t Count = 1000000;
        std::thread producer([&] {
            for (std::uint64_t value = 0; value < Count; ++value) while (!concurrent.Push(value)) std::this_thread::yield();
        });
        bool ordered = true;
        for (std::uint64_t value = 0; value < Count; ++value) {
            while (!concurrent.Pop(result)) std::this_thread::yield();
            if (result != value) ordered = false;
        }
        producer.join();
        Require(ordered, "Concurrent queue corrupted/reordered observations.");
        std::cout << "Clock rejection, cumulative/window statistics, overflow, and concurrent ordering passed.\n";
        return 0;
    }
    catch (const std::exception& error) { std::cerr << error.what() << '\n'; return 1; }
}
