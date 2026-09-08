#pragma once

#include <nlohmann/json.hpp>
#include <istream>
#include <ostream>
#include <stdexcept>

// Generic JSON I/O only. T supplies its declarative nlohmann member mappings.
// Application validation happens explicitly after Deserialize returns.
template<class T>
T Deserialize(std::istream& input)
{
    if (!input.good()) throw std::runtime_error("Could not read JSON input.");
    auto value = nlohmann::json::parse(input).get<T>();
    if (input.bad() || (input.fail() && !input.eof()))
        throw std::runtime_error("Could not read JSON input.");
    return value;
}

template<class T>
void Serialize(std::ostream& output, const T& value)
{
    output << nlohmann::json(value).dump(2) << '\n';
    if (!output) throw std::runtime_error("Could not write JSON output.");
}
