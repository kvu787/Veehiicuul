#include "SettingsJson.h"
#include "SettingsIO.h"
#include "SettingsValidation.h"
#include <chrono>
#include <fstream>
#include <iostream>
#include <limits>
#include <utility>
#include <type_traits>
#include <sstream>
#include <stdexcept>

// Settings and every nested model expose input conversion only.
static_assert(!std::is_constructible_v<JsonIO::Json, Settings>);
static_assert(!std::is_constructible_v<JsonIO::Json, struct Settings::RenderPipeline>);
static_assert(!std::is_constructible_v<JsonIO::Json, Settings::SphereMesh>);
static_assert(!std::is_constructible_v<JsonIO::Json, Settings::Paint>);

namespace
{
void Require(bool value) { if (!value) throw std::runtime_error("JSON settings contract failed"); }
Settings Parse(const std::string& text)
{
    std::istringstream input(text);
    return Deserialize<Settings>(input);
}
template<class Action> void Reject(Action action)
{
    try { action(); }
    catch (const std::exception&) { return; }
    throw std::runtime_error("Invalid input accepted");
}

template<class T> T ParseValue(const char* text)
{
    std::istringstream input(text);
    return Deserialize<T>(input);
}

// Replace a placeholder with raw JSON text so token spelling is preserved.
std::string WithIntegerToken(const nlohmann::json& document, const char* section,
    const char* field, const char* token)
{
    auto modified = document;
    modified[section][field] = "__INTEGER_TOKEN__";
    auto text = modified.dump();
    const std::string placeholder = "\"__INTEGER_TOKEN__\"";
    text.replace(text.find(placeholder), placeholder.size(), token);
    return text;
}

void CheckIntegerConversion(const nlohmann::json& document)
{
    // This exercises nested macro mappings for every count, including controls
    // whose application ranges are inactive for the shipped fixed preset.
    for (const auto& preset : {"Custom", "MinimizeInputLatency"})
    {
        auto configured = document;
        configured["RenderPipeline"]["Preset"] = preset;
        for (const auto& [section, field] : {
            std::pair{"RenderPipeline", "MaxGpuFramesInFlight"},
            std::pair{"RenderPipeline", "MaxPresentLatency"},
            std::pair{"RenderPipeline", "BackBufferCount"},
            std::pair{"Sphere", "UResolution"}, std::pair{"Sphere", "VResolution"}})
        {
            for (const auto* token : {"8.0", "64.0", "64.", "64.5", "8e0", "8E+0", "-0.0",
                "1e-999", "\"64\"", "\"64.0\"", "\"64.\"", "true", "false", "null",
                "[]", "{}", "+8", "08", "0x8", "2147483648", "-2147483649",
                "4294967304", "18446744073709551615", "18446744073709551616",
                "-9223372036854775809", "1e999"})
                Reject([&] { Parse(WithIntegerToken(configured, section, field, token)); });
            ValidateSettings(Parse(WithIntegerToken(configured, section, field, "8")));
            // Values that fit int32_t must reach the independent domain validator unchanged.
            for (const auto* token : {"-2147483648", "2147483647", "-1", "-0"})
            {
                const auto parsed = Parse(WithIntegerToken(configured, section, field, token));
                const auto actual = std::string_view(section) == "Sphere"
                    ? (std::string_view(field) == "UResolution" ? parsed.Sphere.UResolution : parsed.Sphere.VResolution)
                    : (std::string_view(field) == "MaxGpuFramesInFlight" ? parsed.RenderPipeline.MaxGpuFramesInFlight
                        : std::string_view(field) == "MaxPresentLatency" ? parsed.RenderPipeline.MaxPresentLatency
                        : parsed.RenderPipeline.BackBufferCount);
                Require(actual == std::stoll(token));
                if (std::string_view(section) == "Sphere" || std::string_view(preset) == "Custom")
                    Reject([&] { ValidateSettings(parsed); });
                else
                    ValidateSettings(parsed);
            }
        }
    }

    // The policy is reusable for ordinary signed/unsigned integer types.
    Require(ParseValue<std::int32_t>("2147483647") == std::numeric_limits<std::int32_t>::max());
    Require(ParseValue<std::int64_t>("-9223372036854775808") == std::numeric_limits<std::int64_t>::min());
    Require(ParseValue<std::int64_t>("9223372036854775807") == std::numeric_limits<std::int64_t>::max());
    Require(ParseValue<std::uint64_t>("18446744073709551615") == std::numeric_limits<std::uint64_t>::max());
    Require(ParseValue<std::int8_t>("-128") == -128);
    Require(ParseValue<std::uint8_t>("255") == 255);
    Reject([] { ParseValue<std::int8_t>("128"); });
    Reject([] { ParseValue<std::uint8_t>("256"); });
    Reject([] { ParseValue<std::uint32_t>("-1"); });
    Reject([] { ParseValue<std::int64_t>("9223372036854775808"); });
    Reject([] { ParseValue<std::uint64_t>("18446744073709551616"); });
    Require(ParseValue<bool>("true"));
    Require(ParseValue<double>("64.0") == 64.0);
    Require(ParseValue<double>("64e0") == 64.0);
    Require(ParseValue<std::string>("\"64.0\"") == "64.0");
}
}
int main()
{
    try
    {
        std::ifstream input("Assets/Settings.json");
        const auto document = nlohmann::json::parse(input);
        CheckIntegerConversion(document);
        const auto shipped = LoadSettings("Assets/Settings.json");
        Require(Parse(document.dump()) == shipped);
        // Every root section and every nested field is required, including inactive controls.
        for (auto section = document.begin(); section != document.end(); ++section)
        {
            auto missing = document;
            missing.erase(section.key());
            Reject([&] { Parse(missing.dump()); });
            for (auto field = section.value().begin(); field != section.value().end(); ++field)
            {
                missing = document;
                missing[section.key()].erase(field.key());
                Reject([&] { Parse(missing.dump()); });
                auto wrong = document;
                wrong[section.key()][field.key()] = nullptr;
                Reject([&] { Parse(wrong.dump()); });
            }
        }
        for (const auto& bad : {"", "{", "[]", "null", "{\"x\":NaN}", "{\"x\":1e999}", "{} trailing"})
            Reject([&] { Parse(bad); });
        std::istringstream failedInput(document.dump());
        failedInput.setstate(std::ios::badbit);
        Reject([&] { Deserialize<Settings>(failedInput); });
        for (const auto& wrong : {nlohmann::json(true), nlohmann::json("64"), nlohmann::json::object()})
        {
            auto invalid = document;
            invalid["Sphere"]["UResolution"] = wrong;
            Reject([&] { Parse(invalid.dump()); });
        }
        auto tinyDocument = document;
        tinyDocument["SimplePaintShader_Sphere"]["Shift"] = 0.0;
        auto tiny = tinyDocument.dump();
        const auto shift = tiny.find("\"Shift\":0.0");
        Require(shift != std::string::npos);
        tiny.replace(shift, 11, "\"Shift\":1e-999");
        ValidateSettings(Parse(tiny));
        auto custom = shipped;
        custom.RenderPipeline = {.Preset="Custom", .VSync=true, .MaxGpuFramesInFlight=7,
            .MaxPresentLatency=4, .WaitForPresentation=false, .BackBufferCount=8,
            .AllowTearing=true, .WaitStrategy="Spin"};
        custom.Sphere = {.UResolution=80, .VResolution=40};
        for (auto member : {&Settings::SimplePaintShader_Axles, &Settings::SimplePaintShader_Body,
            &Settings::SimplePaintShader_Cabin, &Settings::SimplePaintShader_Headlights,
            &Settings::SimplePaintShader_Wheels, &Settings::SimplePaintShader_Sphere})
            custom.*member = {.BaseColor={.2,.4,.6}, .Brightness=.7, .Shift=.3,
                .RotationDegrees=123, .DarkPoint=.8, .LightPoint=.2};
        auto customDocument = document;
        customDocument["RenderPipeline"] = {{"Preset", "Custom"}, {"VSync", true},
            {"MaxGpuFramesInFlight", 7}, {"MaxPresentLatency", 4}, {"WaitForPresentation", false},
            {"BackBufferCount", 8}, {"AllowTearing", true}, {"WaitStrategy", "Spin"}};
        customDocument["Sphere"] = {{"UResolution", 80}, {"VResolution", 40}};
        for (const auto* section : {"SimplePaintShader_Axles", "SimplePaintShader_Body",
            "SimplePaintShader_Cabin", "SimplePaintShader_Headlights", "SimplePaintShader_Wheels",
            "SimplePaintShader_Sphere"})
            customDocument[section] = {{"BaseColor", {.2, .4, .6}}, {"Brightness", .7},
                {"Shift", .3}, {"RotationDegrees", 123}, {"DarkPoint", .8}, {"LightPoint", .2}};
        Require(Parse(customDocument.dump()) == custom);
        ValidateSettings(custom);
        auto extra = document;
        extra["Unknown"] = true;
        extra["Sphere"]["Unknown"] = false;
        Require(Parse(extra.dump()) == shipped);
        auto duplicate = document.dump();
        duplicate.insert(1, "\"Sphere\":{\"UResolution\":1,\"VResolution\":1},");
        Require(Parse(duplicate) == shipped); // nlohmann keeps the last duplicate.
        extra = document;
        extra["SimplePaintShader_Body"]["BaseColor"].push_back(.5);
        auto parsed = Parse(extra.dump());
        Require(parsed.SimplePaintShader_Body.BaseColor.size() == 4);
        Reject([&] { ValidateSettings(parsed); });
        extra = document;
        extra["RenderPipeline"]["Preset"] = "Typo";
        parsed = Parse(extra.dump());
        Require(parsed.RenderPipeline.Preset == "Typo");
        Reject([&] { ValidateSettings(parsed); });

        const auto path = std::filesystem::temp_directory_path() /
            ("settings-contract-" + std::to_string(std::chrono::steady_clock::now().time_since_epoch().count()) + ".json");
        struct Cleanup { std::filesystem::path path; ~Cleanup() { std::error_code error; std::filesystem::remove(path, error); } } cleanup{path};
        Reject([&] { (void)LoadSettings(path); });
        { std::ofstream file(path); file << customDocument.dump(); }
        Require(LoadSettings(path) == custom);
        { std::ofstream file(path); file << WithIntegerToken(document, "Sphere", "UResolution", "64.0"); }
        Reject([&] { (void)LoadSettings(path); });
        { std::ofstream file(path); file << extra.dump(); }
        try { (void)LoadSettings(path); throw std::logic_error("Invalid file accepted"); }
        catch (const std::runtime_error& error) { Require(std::string(error.what()).find(path.string()) != std::string::npos); }
        std::cout << "Typed JSON, required fields, and validated file loading passed.\n";
        return 0;
    }
    catch (const std::exception& error) { std::cerr << error.what() << '\n'; return 1; }
}

