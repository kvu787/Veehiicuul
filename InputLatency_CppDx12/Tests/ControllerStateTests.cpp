#include "ControllerState.h"

#include <iostream>
#include <stdexcept>

namespace
{
void Require(bool condition, const char* message) { if (!condition) throw std::runtime_error(message); }
}

int main()
{
    try {
        std::array<ControllerAvailability, 5> devices{{{true, false}, {false, true}, {true, true}, {true, true}, {true, false}}};
        Require(SelectController(devices, std::nullopt) == 2, "Selection must skip mice/keyboards and disconnected gamepads.");
        Require(SelectController(devices, 3) == 3, "A connected selected gamepad must remain selected when another is available.");
        devices[3].connected = false;
        Require(SelectController(devices, 3) == 2, "Disconnect must select another connected gamepad.");
        devices[2].connected = false;
        Require(!SelectController(devices, 2), "Disconnected controllers must not retain a live selection.");
        Require(!SelectController({}, 99), "Empty enumeration must reject obsolete selection indexes.");
        devices[1].connected = true;
        Require(SelectController(devices, 99) == 1, "A stale index must fall back to the first connected gamepad.");

        const auto center = MapStickPosition(0, 0, 150, 354, 56);
        const auto upperRight = MapStickPosition(1, 1, 150, 354, 56);
        const auto lowerLeft = MapStickPosition(-1, -1, 150, 354, 56);
        Require(center.x == 150 && center.y == 354, "Neutral stick must be centered.");
        Require(upperRight.x == 206 && upperRight.y == 298, "Positive stick X/Y must draw right/up.");
        Require(lowerLeft.x == 94 && lowerLeft.y == 410, "Negative stick X/Y must draw left/down.");
        const auto drift = MapStickPosition(0.001f, 0, 150, 354, 56);
        Require(drift.x > center.x, "The visualization must preserve small drift instead of adding a deadzone.");
        Require(TriggerFill(-1) == 0 && TriggerFill(0.5f) == 0.5f && TriggerFill(2) == 1, "Trigger fills must preserve the normalized range.");
        Require(FormatAnalogValue(0.125f, true).View() == "+0.1250", "Positive stick data must keep its sign and precision.");
        Require(FormatAnalogValue(-0.75f, true).View() == "-0.7500", "Negative stick data must keep its sign and precision.");
        Require(FormatAnalogValue(0.0001f, true).View() == "+0.0001", "Small raw stick values must remain visible.");
        Require(FormatAnalogValue(0.5f, false).View() == "0.5000", "Trigger data must use normalized numeric values.");
        std::cout << "Controller selection, disconnect fallback, stick coordinates, drift, and analog data formatting passed.\n";
        return 0;
    }
    catch (const std::exception& error) { std::cerr << error.what() << '\n'; return 1; }
}
