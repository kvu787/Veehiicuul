#include "ApplicationSettings.h"
#include <Windows.h>
#include <algorithm>
#include <charconv>
#include <cmath>
#include <cctype>
#include <format>
#include <fstream>
#include <set>
#include <stdexcept>
#include <string>
#include <vector>

namespace
{
std::string_view Trim(std::string_view value)
{
    while (!value.empty() && std::isspace(static_cast<unsigned char>(value.front())) != 0)
    {
        value.remove_prefix(1);
    }
    while (!value.empty() && std::isspace(static_cast<unsigned char>(value.back())) != 0)
    {
        value.remove_suffix(1);
    }
    return value;
}

double ParseFloat(const std::string_view text, const std::size_t lineNumber)
{
    const std::string_view trimmed = Trim(text);
    double value = 0.0;
    const auto [end, error] = std::from_chars(
        trimmed.data(),
        trimmed.data() + trimmed.size(),
        value);
    if (error != std::errc{} || end != trimmed.data() + trimmed.size() || !std::isfinite(value))
    {
        throw std::runtime_error(std::format(
            "Invalid finite floating-point value on Settings.ini line {}.",
            lineNumber));
    }
    return value;
}

std::array<double, 3> ParseColor(const std::string_view text, const std::size_t lineNumber)
{
    const std::size_t firstComma = text.find(',');
    const std::size_t secondComma = firstComma == std::string_view::npos
        ? std::string_view::npos
        : text.find(',', firstComma + 1);
    if (firstComma == std::string_view::npos ||
        secondComma == std::string_view::npos ||
        text.find(',', secondComma + 1) != std::string_view::npos)
    {
        throw std::runtime_error(std::format(
            "Expected an R, G, B triple on Settings.ini line {}.",
            lineNumber));
    }

    return {
        ParseFloat(text.substr(0, firstComma), lineNumber),
        ParseFloat(text.substr(firstComma + 1, secondComma - firstComma - 1), lineNumber),
        ParseFloat(text.substr(secondComma + 1), lineNumber),
    };
}

struct Entry
{
    std::string section;
    std::string key;
    std::string value;
    std::size_t line;
};

[[noreturn]] void Invalid(const Entry& entry, const std::string_view reason)
{
    throw std::runtime_error(std::format("[{}].{} on Settings.ini line {}: {}",
        entry.section, entry.key, entry.line, reason));
}

std::uint32_t ReadInteger(const Entry& entry, const std::uint32_t minimum, const std::uint32_t maximum)
{
    std::uint32_t result = 0;
    const auto [end, error] = std::from_chars(entry.value.data(), entry.value.data() + entry.value.size(), result);
    if (error != std::errc{} || end != entry.value.data() + entry.value.size() || result < minimum || result > maximum)
        Invalid(entry, std::format("expected an integer in [{}, {}]", minimum, maximum));
    return result;
}

bool ReadBool(const Entry& entry)
{
    if (entry.value == "true") return true;
    if (entry.value == "false") return false;
    Invalid(entry, "expected true or false");
}

constexpr std::array<std::string_view, 6> CustomRenderPipelineKeys = {
    "MaxGpuFramesInFlight", "MaxPresentLatency", "WaitForPresentation",
    "BackBufferCount", "AllowTearing", "WaitStrategy",
};

constexpr std::array<std::string_view, 6> MaterialSections = {
    "SimplePaintShader_Axles", "SimplePaintShader_Body", "SimplePaintShader_Cabin",
    "SimplePaintShader_Headlights", "SimplePaintShader_Wheels", "SimplePaintShader_Sphere",
};
}

std::filesystem::path ModuleDirectory()
{
    std::wstring modulePath(32768, L'\0');
    const DWORD length = GetModuleFileNameW(
        nullptr,
        modulePath.data(),
        static_cast<DWORD>(modulePath.size()));
    if (length == 0 || static_cast<std::size_t>(length) >= modulePath.size())
    {
        throw std::runtime_error(std::format(
            "GetModuleFileNameW failed with Win32 error {}.",
            GetLastError()));
    }
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
    // Collect raw values first: custom entries are not interpreted until the preset is known.
    std::vector<Entry> entries;
    std::string section;
    std::string line;
    std::size_t lineNumber = 0;
    while (std::getline(input, line))
    {
        ++lineNumber;
        const auto content = Trim(std::string_view(line).substr(0, line.find_first_of(";#")));
        if (content.empty()) continue;
        if (content.front() == '[')
        {
            if (content.back() != ']')
                throw std::runtime_error(std::format("Malformed Settings.ini section on line {}.", lineNumber));
            section = Trim(content.substr(1, content.size() - 2));
            if (section != "RenderPipeline" &&
                section != "Sphere" && std::find(MaterialSections.begin(), MaterialSections.end(), section) == MaterialSections.end())
                throw std::runtime_error(std::format("Unknown Settings.ini section [{}] on line {}.", section, lineNumber));
            continue;
        }
        const auto equals = content.find('=');
        if (section.empty() || equals == std::string_view::npos || Trim(content.substr(0, equals)).empty())
            throw std::runtime_error(std::format("Expected section and key = value on Settings.ini line {}.", lineNumber));
        entries.push_back({.section = section, .key = std::string(Trim(content.substr(0, equals))),
            .value = std::string(Trim(content.substr(equals + 1))), .line = lineNumber});
    }
    if (input.bad()) throw std::runtime_error("Could not read Settings.ini.");

    ApplicationSettings result;
    bool havePreset = false;
    for (const auto& entry : entries)
    {
        if (entry.section != "RenderPipeline" || entry.key != "Preset") continue;
        if (havePreset) Invalid(entry, "duplicate setting");
        havePreset = true;
        if (entry.value == "MinimizeInputLatency") result.renderPipelinePreset = RenderPipelinePreset::MinimizeInputLatency;
        else if (entry.value == "Standard") result.renderPipelinePreset = RenderPipelinePreset::Standard;
        else if (entry.value == "MaximizeFps") result.renderPipelinePreset = RenderPipelinePreset::MaximizeFps;
        else if (entry.value == "Custom") result.renderPipelinePreset = RenderPipelinePreset::Custom;
        else Invalid(entry, "expected MinimizeInputLatency, Standard, MaximizeFps, or Custom");
    }
    if (!havePreset) throw std::runtime_error("Missing [RenderPipeline].Preset in Settings.ini.");
    switch (result.renderPipelinePreset)
    {
    case RenderPipelinePreset::MinimizeInputLatency: result.renderPipeline = MinimizeInputLatencyRenderPipeline; break;
    case RenderPipelinePreset::MaximizeFps: result.renderPipeline = MaximizeFpsRenderPipeline; break;
    case RenderPipelinePreset::Standard:
    case RenderPipelinePreset::Custom: result.renderPipeline = StandardRenderPipeline; break;
    }

    std::array<SimplePaint::Parameters, 6> materials{};
    constexpr std::array<std::array<double, 3>, 6> colors = {{
        {0.678429127, 0.678431321, 0.678431321},
        {SimplePaint::Margin, 0.436627067, SimplePaint::InteriorMaximum},
        {0.506386429, 0.756053146, SimplePaint::InteriorMaximum},
        {SimplePaint::InteriorMaximum, 0.815686771, SimplePaint::Margin},
        {0.345097446, 0.345097446, 0.345097446}, {0.107, 0.223, 0.578},
    }};
    for (std::size_t i = 0; i < materials.size(); ++i)
        materials[i] = {.baseColorSrgb = colors[i], .brightness = i == 5 ? 0.126 : 0.5, .lightPoint = i == 5 ? 1.0 : 0.8};

    std::set<std::string> seen;
    for (const auto& entry : entries)
    {
        // Only the six custom controls are inactive under fixed presets. VSync and Preset always apply.
        if (entry.section == "RenderPipeline" && result.renderPipelinePreset != RenderPipelinePreset::Custom &&
            std::find(CustomRenderPipelineKeys.begin(), CustomRenderPipelineKeys.end(), entry.key) != CustomRenderPipelineKeys.end())
            continue;
        const auto qualified = entry.section + "." + entry.key;
        if (!seen.insert(qualified).second) Invalid(entry, "duplicate setting");
        if (qualified == "RenderPipeline.Preset") continue;
        if (entry.section == "RenderPipeline")
        {
            auto& renderPipeline = result.renderPipeline;
            if (entry.key == "VSync") result.vsync = ReadBool(entry);
            else if (entry.key == "MaxGpuFramesInFlight") renderPipeline.maxGpuFramesInFlight = ReadInteger(entry, 1, 16);
            else if (entry.key == "MaxPresentLatency") renderPipeline.maxPresentLatency = ReadInteger(entry, 1, 16);
            else if (entry.key == "WaitForPresentation") renderPipeline.waitForPresentation = ReadBool(entry);
            else if (entry.key == "BackBufferCount") renderPipeline.backBufferCount = ReadInteger(entry, 2, 16);
            else if (entry.key == "AllowTearing") renderPipeline.allowTearing = ReadBool(entry);
            else if (entry.key == "WaitStrategy")
            {
                if (entry.value == "Event") renderPipeline.waitStrategy = WaitStrategy::Event;
                else if (entry.value == "Spin") renderPipeline.waitStrategy = WaitStrategy::Spin;
                else Invalid(entry, "expected Event or Spin");
            }
            else Invalid(entry, "unknown setting");
        }
        else if (entry.section == "Sphere")
        {
            if (entry.key == "UResolution") result.sphereUResolution = ReadInteger(entry, 3, 512);
            else if (entry.key == "VResolution") result.sphereVResolution = ReadInteger(entry, 2, 512);
            else Invalid(entry, "unknown setting");
        }
        else
        {
            const auto material = std::find(MaterialSections.begin(), MaterialSections.end(), entry.section);
            if (material == MaterialSections.end()) Invalid(entry, "unknown setting");
            auto& paint = materials[static_cast<std::size_t>(material - MaterialSections.begin())];
            if (entry.key == "BaseColor") paint.baseColorSrgb = ParseColor(entry.value, entry.line);
            else if (entry.key == "Brightness") paint.brightness = ParseFloat(entry.value, entry.line);
            else if (entry.key == "Shift") paint.shift = ParseFloat(entry.value, entry.line);
            else if (entry.key == "RotationDegrees") paint.rotationDegrees = ParseFloat(entry.value, entry.line);
            else if (entry.key == "DarkPoint") paint.darkPoint = ParseFloat(entry.value, entry.line);
            else if (entry.key == "LightPoint") paint.lightPoint = ParseFloat(entry.value, entry.line);
            else Invalid(entry, "unknown setting");
        }
    }
    if (!seen.contains("RenderPipeline.VSync")) throw std::runtime_error("Missing [RenderPipeline].VSync in Settings.ini.");
    if (result.renderPipelinePreset == RenderPipelinePreset::Custom)
    {
        for (const auto key : CustomRenderPipelineKeys)
            if (!seen.contains(std::format("RenderPipeline.{}", key)))
                throw std::runtime_error(std::format("Missing [RenderPipeline].{} in Settings.ini.", key));
    }
    ValidateRenderPipelineSettings(result.renderPipeline);
    for (std::size_t i = 0; i < materials.size(); ++i)
    {
        try { result.paintMaterials[i] = SimplePaint::Material::Compile(materials[i]).Constants(); }
        catch (const std::invalid_argument& error)
        { throw std::invalid_argument(std::format("[{}]: {}", MaterialSections[i], error.what())); }
    }
    return result;
}

ApplicationSettings LoadApplicationSettings(const std::filesystem::path& path)
{
    std::ifstream input(path);
    if (!input) throw std::runtime_error(std::format("Settings were not found at {}.", path.string()));
    return ParseApplicationSettings(input);
}
