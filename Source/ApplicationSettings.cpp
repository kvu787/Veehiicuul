#include "ApplicationSettings.h"
#include <Windows.h>
#include <nlohmann/json.hpp>
#include <algorithm>
#include <cmath>
#include <format>
#include <fstream>
#include <initializer_list>
#include <set>
#include <stdexcept>
#include <string>
#include <vector>

namespace
{
using Json = nlohmann::json;

constexpr std::array<std::string_view, 6> MaterialSections = {
    "SimplePaintShader_Axles", "SimplePaintShader_Body", "SimplePaintShader_Cabin",
    "SimplePaintShader_Headlights", "SimplePaintShader_Wheels", "SimplePaintShader_Sphere",
};

[[noreturn]] void Invalid(const std::string_view path, const std::string_view reason)
{
    throw std::runtime_error(std::format("{}: {}", path, reason));
}

std::string PropertyPath(const std::string_view parent, const std::string_view key)
{
    return parent == "$" ? std::string(key) : std::format("{}.{}", parent, key);
}

Json ReadJson(std::istream& input)
{
    // Check keys before the DOM can overwrite them, even inside inactive controls.
    struct Container
    {
        std::string path;
        bool array;
        std::set<std::string> keys;
        std::string key;
        std::size_t nextIndex = 0;
    };
    std::vector<Container> containers;
    const auto nextPath = [&]() -> std::string {
        if (containers.empty()) return "$";
        auto& parent = containers.back();
        if (parent.array) return std::format("{}[{}]", parent.path, parent.nextIndex++);
        return PropertyPath(parent.path, parent.key);
    };
    const auto callback = [&](int, const Json::parse_event_t event, Json& value) {
        switch (event)
        {
        case Json::parse_event_t::object_start:
        case Json::parse_event_t::array_start:
        {
            auto path = nextPath();
            containers.push_back({.path = std::move(path), .array = event == Json::parse_event_t::array_start,
                .keys = {}, .key = {}});
            break;
        }
        case Json::parse_event_t::key:
        {
            auto& object = containers.back();
            object.key = value.get<std::string>();
            if (!object.keys.insert(object.key).second)
                Invalid(PropertyPath(object.path, object.key), "duplicate property");
            break;
        }
        case Json::parse_event_t::object_end:
        case Json::parse_event_t::array_end:
            containers.pop_back();
            break;
        case Json::parse_event_t::value:
            (void)nextPath();
            break;
        }
        return true;
    };
    if (!input.good()) Invalid("$", "could not read settings");
    auto document = Json::parse(input, callback);
    if (input.bad() || (input.fail() && !input.eof())) Invalid("$", "could not read settings");
    return document;
}

void CheckObject(const Json& object, const std::string_view path,
    const std::initializer_list<std::string_view> keys)
{
    if (!object.is_object()) Invalid(path, "expected an object");
    for (const auto& [key, value] : object.items())
    {
        (void)value;
        if (std::find(keys.begin(), keys.end(), key) == keys.end())
            Invalid(PropertyPath(path, key), "unknown setting");
    }
}

const Json& Required(const Json& object, const std::string_view key, const std::string_view path)
{
    const auto found = object.find(key);
    if (found == object.end()) Invalid(PropertyPath(path, key), "missing required setting");
    return *found;
}

std::uint32_t ReadInteger(const Json& value, const std::string_view path,
    const std::uint32_t minimum, const std::uint32_t maximum)
{
    // Inspect signedness and range before narrowing: get<uint32_t>() alone can wrap or truncate.
    if (value.is_number_unsigned())
    {
        const auto number = value.get<std::uint64_t>();
        if (number >= minimum && number <= maximum) return static_cast<std::uint32_t>(number);
    }
    else if (value.is_number_integer())
    {
        const auto number = value.get<std::int64_t>();
        if (number >= minimum && number <= maximum) return static_cast<std::uint32_t>(number);
    }
    Invalid(path, std::format("expected an integer in [{}, {}]", minimum, maximum));
}

bool ReadBool(const Json& value, const std::string_view path)
{
    if (!value.is_boolean()) Invalid(path, "expected true or false");
    return value.get<bool>();
}

std::string ReadString(const Json& value, const std::string_view path)
{
    if (!value.is_string()) Invalid(path, "expected a string");
    return value.get<std::string>();
}

double ReadNumber(const Json& value, const std::string_view path)
{
    if (!value.is_number()) Invalid(path, "expected a finite number");
    const auto number = value.get<double>();
    if (!std::isfinite(number)) Invalid(path, "expected a finite number");
    return number;
}

std::array<double, 3> ReadColor(const Json& value, const std::string_view path)
{
    if (!value.is_array() || value.size() != 3) Invalid(path, "expected an array of three RGB numbers");
    return {ReadNumber(value[0], std::format("{}[0]", path)),
        ReadNumber(value[1], std::format("{}[1]", path)),
        ReadNumber(value[2], std::format("{}[2]", path))};
}

ApplicationSettings ParseSettings(std::istream& input)
{
    const auto document = ReadJson(input);
    CheckObject(document, "$", {"RenderPipeline", "Sphere",
        "SimplePaintShader_Axles", "SimplePaintShader_Body", "SimplePaintShader_Cabin",
        "SimplePaintShader_Headlights", "SimplePaintShader_Wheels", "SimplePaintShader_Sphere"});
    const auto& pipeline = Required(document, "RenderPipeline", "$");
    CheckObject(pipeline, "RenderPipeline", {"Preset", "VSync", "MaxGpuFramesInFlight",
        "MaxPresentLatency", "WaitForPresentation", "BackBufferCount", "AllowTearing", "WaitStrategy"});
    const auto preset = ReadString(Required(pipeline, "Preset", "RenderPipeline"), "RenderPipeline.Preset");

    ApplicationSettings result;
    if (preset == "MinimizeInputLatency")
    {
        result.renderPipelinePreset = RenderPipelinePreset::MinimizeInputLatency;
        result.renderPipeline = MinimizeInputLatencyRenderPipeline;
    }
    else if (preset == "Standard")
        result.renderPipelinePreset = RenderPipelinePreset::Standard;
    else if (preset == "MaximizeFps")
    {
        result.renderPipelinePreset = RenderPipelinePreset::MaximizeFps;
        result.renderPipeline = MaximizeFpsRenderPipeline;
    }
    else if (preset == "Custom")
        result.renderPipelinePreset = RenderPipelinePreset::Custom;
    else
        Invalid("RenderPipeline.Preset", "expected MinimizeInputLatency, Standard, MaximizeFps, or Custom");

    result.vsync = ReadBool(Required(pipeline, "VSync", "RenderPipeline"), "RenderPipeline.VSync");
    if (result.renderPipelinePreset == RenderPipelinePreset::Custom)
    {
        auto& p = result.renderPipeline;
        p.maxGpuFramesInFlight = ReadInteger(Required(pipeline, "MaxGpuFramesInFlight", "RenderPipeline"),
            "RenderPipeline.MaxGpuFramesInFlight", 1, 16);
        p.maxPresentLatency = ReadInteger(Required(pipeline, "MaxPresentLatency", "RenderPipeline"),
            "RenderPipeline.MaxPresentLatency", 1, 16);
        p.waitForPresentation = ReadBool(Required(pipeline, "WaitForPresentation", "RenderPipeline"),
            "RenderPipeline.WaitForPresentation");
        p.backBufferCount = ReadInteger(Required(pipeline, "BackBufferCount", "RenderPipeline"),
            "RenderPipeline.BackBufferCount", 2, 16);
        p.allowTearing = ReadBool(Required(pipeline, "AllowTearing", "RenderPipeline"), "RenderPipeline.AllowTearing");
        const auto wait = ReadString(Required(pipeline, "WaitStrategy", "RenderPipeline"), "RenderPipeline.WaitStrategy");
        if (wait == "Event") p.waitStrategy = WaitStrategy::Event;
        else if (wait == "Spin") p.waitStrategy = WaitStrategy::Spin;
        else Invalid("RenderPipeline.WaitStrategy", "expected Event or Spin");
    }
    ValidateRenderPipelineSettings(result.renderPipeline);

    if (const auto sphere = document.find("Sphere"); sphere != document.end())
    {
        CheckObject(*sphere, "Sphere", {"UResolution", "VResolution"});
        if (sphere->contains("UResolution"))
            result.sphereUResolution = ReadInteger(sphere->at("UResolution"), "Sphere.UResolution", 3, 512);
        if (sphere->contains("VResolution"))
            result.sphereVResolution = ReadInteger(sphere->at("VResolution"), "Sphere.VResolution", 2, 512);
    }

    constexpr std::array<std::array<double, 3>, 6> colors = {{
        {0.678429127, 0.678431321, 0.678431321},
        {SimplePaint::Margin, 0.436627067, SimplePaint::InteriorMaximum},
        {0.506386429, 0.756053146, SimplePaint::InteriorMaximum},
        {SimplePaint::InteriorMaximum, 0.815686771, SimplePaint::Margin},
        {0.345097446, 0.345097446, 0.345097446}, {0.107, 0.223, 0.578},
    }};
    for (std::size_t i = 0; i < MaterialSections.size(); ++i)
    {
        const auto section = MaterialSections[i];
        SimplePaint::Parameters paint{
            .baseColorSrgb = colors[i], .brightness = i == 5 ? 0.126 : 0.5, .lightPoint = i == 5 ? 1.0 : 0.8};
        if (const auto material = document.find(section); material != document.end())
        {
            CheckObject(*material, section, {"BaseColor", "Brightness", "Shift", "RotationDegrees", "DarkPoint", "LightPoint"});
            for (const auto& [key, value] : material->items())
            {
                const auto path = PropertyPath(section, key);
                if (key == "BaseColor") paint.baseColorSrgb = ReadColor(value, path);
                else if (key == "Brightness") paint.brightness = ReadNumber(value, path);
                else if (key == "Shift") paint.shift = ReadNumber(value, path);
                else if (key == "RotationDegrees") paint.rotationDegrees = ReadNumber(value, path);
                else if (key == "DarkPoint") paint.darkPoint = ReadNumber(value, path);
                else if (key == "LightPoint") paint.lightPoint = ReadNumber(value, path);
            }
        }
        try { result.paintMaterials[i] = SimplePaint::Material::Compile(paint).Constants(); }
        catch (const std::invalid_argument& error) { Invalid(section, error.what()); }
    }
    return result;
}

ApplicationSettings ParseWithSource(std::istream& input, const std::string_view source)
{
    try { return ParseSettings(input); }
    catch (const std::exception& error) { throw std::runtime_error(std::format("{}: {}", source, error.what())); }
}
}

std::filesystem::path ModuleDirectory()
{
    std::wstring modulePath(32768, L'\0');
    const DWORD length = GetModuleFileNameW(nullptr, modulePath.data(), static_cast<DWORD>(modulePath.size()));
    if (length == 0 || static_cast<std::size_t>(length) >= modulePath.size())
        throw std::runtime_error(std::format("GetModuleFileNameW failed with Win32 error {}.", GetLastError()));
    modulePath.resize(length);
    return std::filesystem::path(modulePath).parent_path();
}

std::wstring_view RenderPipelinePresetName(const RenderPipelinePreset preset)
{
    switch (preset)
    {
    case RenderPipelinePreset::MinimizeInputLatency: return L"MinimizeInputLatency";
    case RenderPipelinePreset::Standard: return L"Standard";
    case RenderPipelinePreset::MaximizeFps: return L"MaximizeFps";
    case RenderPipelinePreset::Custom: return L"Custom";
    }
    throw std::invalid_argument("Invalid render pipeline preset.");
}

void ValidateRenderPipelineSettings(const RenderPipelineSettings& settings)
{
    if (settings.maxGpuFramesInFlight < 1 || settings.maxGpuFramesInFlight > 16 ||
        settings.maxPresentLatency < 1 || settings.maxPresentLatency > 16 ||
        settings.backBufferCount < 2 || settings.backBufferCount > 16 ||
        (settings.waitStrategy != WaitStrategy::Event && settings.waitStrategy != WaitStrategy::Spin))
        throw std::invalid_argument("Invalid render pipeline limits or wait strategy.");
}

ApplicationSettings ParseApplicationSettings(std::istream& input)
{
    return ParseWithSource(input, "Settings.json");
}

ApplicationSettings LoadApplicationSettings(const std::filesystem::path& path)
{
    std::ifstream input(path, std::ios::binary);
    if (!input) throw std::runtime_error(std::format("{}: could not open settings file.", path.string()));
    return ParseWithSource(input, path.string());
}
