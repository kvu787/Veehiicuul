#pragma once

#include <nlohmann/json.hpp>
#include <cstdint>
#include <map>
#include <memory>
#include <stdexcept>
#include <string>
#include <type_traits>
#include <utility>
#include <vector>

namespace JsonIO
{
// Use nlohmann's normal declarative conversions except for integer destinations.
template<class T, class Enable = void>
struct Serializer : nlohmann::adl_serializer<T, Enable> {};

template<class T>
struct Serializer<T, std::enable_if_t<std::is_integral_v<T> && !std::is_same_v<T, bool>>>
    : nlohmann::adl_serializer<T>
{
    template<class Json>
    static void from_json(const Json& source, T& destination)
    {
        const auto assign = [&](auto value) {
            if (!std::in_range<T>(value))
                throw std::out_of_range("JSON integer is outside the destination C++ type's range.");
            destination = static_cast<T>(value);
        };

        // The parser distinguishes integer tokens from decimal/exponent tokens.
        // Check unsigned first: is_number_integer() includes unsigned integers.
        if (source.is_number_unsigned())
            assign(source.template get_ref<const typename Json::number_unsigned_t&>());
        else if (source.is_number_integer())
            assign(source.template get_ref<const typename Json::number_integer_t&>());
        else
            throw std::invalid_argument("Expected a JSON integer token (no quotes, decimal point, or exponent).");
    }
};

// This policy is local to our I/O adapter; the vendored library is unchanged.
using Json = nlohmann::basic_json<std::map, std::vector, std::string, bool,
    std::int64_t, std::uint64_t, double, std::allocator, Serializer>;
}
