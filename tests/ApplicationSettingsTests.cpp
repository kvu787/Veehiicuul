#include "ApplicationSettings.h"
#include <nlohmann/json.hpp>
#include <bit>
#include <chrono>
#include <cmath>
#include <fstream>
#include <iostream>
#include <sstream>
#include <stdexcept>
#include <string>

namespace
{
using Json = nlohmann::json;

void Require(bool value, const char* reason) { if (!value) throw std::runtime_error(reason); }
ApplicationSettings Parse(const std::string& text) { std::istringstream input(text); return ParseApplicationSettings(input); }
ApplicationSettings Parse(const Json& document) { return Parse(document.dump()); }

Json Config(const std::string& preset = "Custom", bool vsync = false)
{
    return {{"RenderPipeline", {
        {"Preset", preset}, {"VSync", vsync}, {"MaxGpuFramesInFlight", 3}, {"MaxPresentLatency", 1},
        {"WaitForPresentation", false}, {"BackBufferCount", 2}, {"AllowTearing", true}, {"WaitStrategy", "Spin"}}}};
}

std::string WithSection(const std::string& section, const std::string& contents)
{
    return R"({"RenderPipeline":{"Preset":"Standard","VSync":false},")" + section + "\":{" + contents + "}}";
}

void Reject(const std::string& text, const std::string& fragment)
{
    try { (void)Parse(text); }
    catch (const std::exception& error)
    {
        const std::string message = error.what();
        Require(message.starts_with("Settings.json: "), error.what());
        Require(message.find(fragment) != std::string::npos, error.what());
        return;
    }
    throw std::runtime_error("Invalid settings accepted: " + text);
}
void Reject(const Json& document, const std::string& fragment) { Reject(document.dump(), fragment); }

bool SameMaterial(const SimplePaint::GpuMaterial& left, const SimplePaint::GpuMaterial& right)
{
    return std::bit_cast<std::array<std::uint32_t, 20>>(left) == std::bit_cast<std::array<std::uint32_t, 20>>(right);
}

void CheckPresets()
{
    for (const auto& preset : {"Standard", "MinimizeInputLatency", "MaximizeFps", "Custom"})
    {
        const auto off = Parse(Config(preset));
        const auto on = Parse(Config(preset, true));
        Require(!off.vsync && on.vsync && off.renderPipeline == on.renderPipeline, "VSync is not independent");
        const auto expected = std::string_view(preset) == "Standard" ? StandardRenderPipeline :
            std::string_view(preset) == "MinimizeInputLatency" ? MinimizeInputLatencyRenderPipeline :
            std::string_view(preset) == "MaximizeFps" ? MaximizeFpsRenderPipeline :
            RenderPipelineSettings{.maxGpuFramesInFlight=3, .maxPresentLatency=1, .waitForPresentation=false,
                .backBufferCount=2, .allowTearing=true, .waitStrategy=WaitStrategy::Spin};
        Require(off.renderPipeline == expected, "Incorrect effective pipeline");
        const std::wstring name(preset, preset + std::char_traits<char>::length(preset));
        Require(RenderPipelinePresetName(off.renderPipelinePreset) == name, "Incorrect preset name");
        Require(off.sphereUResolution == 64 && off.sphereVResolution == 32, "Sphere defaults changed");
        for (const auto key : {"Preset", "VSync"})
        {
            auto missing = Config(preset);
            missing["RenderPipeline"].erase(key);
            Reject(missing, std::string("RenderPipeline.") + key + ": missing");
        }
        for (const auto key : {"Unknown", "Mode", "FrameRateLimit"})
        {
            auto unknown = Config(preset);
            unknown["RenderPipeline"][key] = 1;
            Reject(unknown, std::string("RenderPipeline.") + key + ": unknown");
        }
        for (const auto value : {"0", "1", "\"false\"", "null", "[]", "{}"})
        {
            auto invalid = Config(preset);
            invalid["RenderPipeline"]["VSync"] = Json::parse(value);
            Reject(invalid, "RenderPipeline.VSync");
        }
    }

    const auto reordered = Parse(std::string(R"({"RenderPipeline":{
        "WaitStrategy":"Spin","AllowTearing":true,"BackBufferCount":2,
        "WaitForPresentation":false,"MaxPresentLatency":1,"MaxGpuFramesInFlight":3,
        "VSync":false,"Preset":"Custom"}})"));
    Require(reordered.renderPipeline == Parse(Config()).renderPipeline, "Property order changed settings");

    for (const auto preset : {"Standard", "MinimizeInputLatency", "MaximizeFps"})
    {
        auto inactive = Config(preset);
        for (const auto key : {"MaxGpuFramesInFlight", "MaxPresentLatency", "WaitForPresentation",
            "BackBufferCount", "AllowTearing", "WaitStrategy"})
            inactive["RenderPipeline"][key] = "invalid but inactive";
        // Exercise nested arrays/objects in the callback without treating sibling keys as duplicates.
        inactive["RenderPipeline"]["MaxGpuFramesInFlight"] = Json::parse(R"([{"x":1},{"x":2},[],{}])");
        Require(Parse(inactive).renderPipeline == Parse(Config(preset)).renderPipeline, "Inactive values applied");
    }

    for (const auto key : {"MaxGpuFramesInFlight", "MaxPresentLatency", "WaitForPresentation",
        "BackBufferCount", "AllowTearing", "WaitStrategy"})
    {
        auto missing = Config();
        missing["RenderPipeline"].erase(key);
        Reject(missing, std::string("RenderPipeline.") + key + ": missing");
    }
    for (const auto value : {"\"unknown\"", "\"MaximizeFPS\"", "0", "null"})
    {
        auto invalid = Config();
        invalid["RenderPipeline"]["Preset"] = Json::parse(value);
        Reject(invalid, "RenderPipeline.Preset");
    }
    for (const auto value : {"\"Sleep\"", "\"spin\"", "0", "null"})
    {
        auto invalid = Config();
        invalid["RenderPipeline"]["WaitStrategy"] = Json::parse(value);
        Reject(invalid, "RenderPipeline.WaitStrategy");
    }
    for (const auto key : {"WaitForPresentation", "AllowTearing"})
        for (const auto value : {"0", "1", "\"true\"", "null"})
        {
            auto invalid = Config();
            invalid["RenderPipeline"][key] = Json::parse(value);
            Reject(invalid, std::string("RenderPipeline.") + key);
        }
}

void CheckIntegers()
{
    for (const auto key : {"MaxGpuFramesInFlight", "MaxPresentLatency", "BackBufferCount"})
    {
        const int minimum = std::string_view(key) == "BackBufferCount" ? 2 : 1;
        for (const int value : {minimum, 16})
        {
            auto valid = Config();
            valid["RenderPipeline"][key] = value;
            (void)Parse(valid);
        }
        for (const auto value : {"-1", "0", "17", "3.0", "3e0", "3.5", "\"3\"", "null", "true",
            "4294967297", "18446744073709551615", "18446744073709551616", "-9223372036854775809"})
        {
            auto invalid = Config();
            invalid["RenderPipeline"][key] = Json::parse(value);
            Reject(invalid, std::string("RenderPipeline.") + key);
        }
    }
    auto invalidBuffers = Config();
    invalidBuffers["RenderPipeline"]["BackBufferCount"] = 1;
    Reject(invalidBuffers, "RenderPipeline.BackBufferCount");

    for (const auto key : {"UResolution", "VResolution"})
    {
        const int minimum = std::string_view(key) == "UResolution" ? 3 : 2;
        for (const int value : {minimum, 512})
        {
            const auto result = Parse(WithSection("Sphere", "\"" + std::string(key) + "\":" + std::to_string(value)));
            Require((std::string_view(key) == "UResolution" ? result.sphereUResolution : result.sphereVResolution) ==
                static_cast<std::uint32_t>(value), "Sphere value lost");
        }
        for (const auto value : {"-1", "0", "1", "513", "64.0", "6.4e1", "\"64\"", "true", "null",
            "4294967360", "18446744073709551615"})
            Reject(WithSection("Sphere", "\"" + std::string(key) + "\":" + value), std::string("Sphere.") + key);
    }
    Reject(WithSection("Sphere", R"("UResolution":2)"), "Sphere.UResolution");
}

void CheckSyntaxAndDuplicates()
{
    for (const auto text : {"", "{", "{}{}", "{/*comment*/}", "{\"x\":1,}", "[RenderPipeline]\nPreset=Standard",
        R"({"RenderPipeline":{"Preset":"Standard","VSync":false,}})", R"({"x":NaN})",
        R"({"x":Infinity})", R"({"x":01})", R"({"x":+1})"})
        Reject(std::string(text), "parse_error");
    for (const auto text : {"[]", "null", "false", "42", "\"text\""})
        Reject(std::string(text), "$: expected an object");
    Reject(std::string("{}"), "RenderPipeline: missing");
    for (const auto value : {"null", "[]", "true", "42", "\"text\""})
    {
        for (const auto section : {"RenderPipeline", "Sphere", "SimplePaintShader_Body"})
        {
            auto invalid = Config("Standard");
            invalid[section] = Json::parse(value);
            Reject(invalid, std::string(section) + ": expected an object");
        }
    }
    for (const auto section : {"Rendering", "Pipeline", "Pipeline.Custom", "Unknown"})
        Reject(WithSection(section, ""), std::string(section) + ": unknown");
    Reject(WithSection("Sphere", R"("Unknown":1)"), "Sphere.Unknown");
    Reject(WithSection("SimplePaintShader_Body", R"("FacingCutoff":0.01)"), "SimplePaintShader_Body.FacingCutoff");
    Reject(std::string(R"({"RenderPipeline":{},"RenderPipeline":{}})"), "RenderPipeline: duplicate property");
    Reject(std::string(R"({"RenderPipeline":{"Preset":"Standard","Preset":"Custom","VSync":false}})"),
        "RenderPipeline.Preset: duplicate property");
    Reject(std::string(R"({"RenderPipeline":{"Preset":"Standard","VSync":false,"\u0056Sync":true}})"),
        "RenderPipeline.VSync: duplicate property");
    Reject(WithSection("Sphere", R"("UResolution":64,"UResolution":32)"), "Sphere.UResolution: duplicate property");
    Reject(WithSection("SimplePaintShader_Body", R"("BaseColor":[0.2,0.3,0.4],"BaseColor":[0.3,0.4,0.5])"),
        "SimplePaintShader_Body.BaseColor: duplicate property");
    for (const auto preset : {"Standard", "MinimizeInputLatency", "MaximizeFps", "Custom"})
        Reject(std::string(R"({"RenderPipeline":{"Preset":")") + preset +
            R"(","VSync":false,"MaxGpuFramesInFlight":1,"MaxGpuFramesInFlight":2}})",
            "RenderPipeline.MaxGpuFramesInFlight: duplicate property");
    Reject(std::string(R"({"RenderPipeline":{"Preset":"Standard","VSync":false,
        "MaxGpuFramesInFlight":[{"x":1},{"x":2,"x":3}]}})"),
        "RenderPipeline.MaxGpuFramesInFlight[1].x: duplicate property");
    std::istringstream failed("{}");
    failed.setstate(std::ios::badbit);
    try { (void)ParseApplicationSettings(failed); throw std::logic_error("Bad stream accepted"); }
    catch (const std::runtime_error& error)
    {
        Require(std::string(error.what()).find("could not read settings") != std::string::npos, error.what());
    }
}

void CheckMaterials()
{
    constexpr std::array<std::string_view, 6> sections = {"SimplePaintShader_Axles", "SimplePaintShader_Body",
        "SimplePaintShader_Cabin", "SimplePaintShader_Headlights", "SimplePaintShader_Wheels", "SimplePaintShader_Sphere"};
    const auto defaults = Parse(Config("Standard"));
    const SimplePaint::Parameters edited{.baseColorSrgb={0.107, 0.223, 0.578}, .brightness=0.25, .shift=0.6,
        .rotationDegrees=45, .darkPoint=0.05, .lightPoint=0.95};
    const auto editedGpu = SimplePaint::Material::Compile(edited).Constants();
    for (std::size_t i = 0; i < sections.size(); ++i)
    {
        const auto changed = Parse(WithSection(std::string(sections[i]), R"(
            "BaseColor":[0.107,0.223,0.578],"Brightness":0.25,"Shift":0.6,
            "RotationDegrees":45,"DarkPoint":0.05,"LightPoint":0.95)"));
        for (std::size_t j = 0; j < sections.size(); ++j)
            Require(SameMaterial(changed.paintMaterials[j], j == i ? editedGpu : defaults.paintMaterials[j]),
                "Material mapping or defaults changed");
    }
    for (const auto color : {"[]", "[0.1,0.2]", "[0.1,0.2,0.3,0.4]", "\"0.1,0.2,0.3\"", "null",
        "[true,0.2,0.3]", "[0.1,\"0.2\",0.3]", "[0.1,0.2,null]", "[0,0.2,0.3]", "[0.1,1,0.3]"})
        Reject(WithSection("SimplePaintShader_Body", "\"BaseColor\":" + std::string(color)), "SimplePaintShader_Body");
    for (const auto key : {"Brightness", "Shift", "RotationDegrees", "DarkPoint", "LightPoint"})
        for (const auto value : {"true", "\"0.5\"", "null", "[]", "{}"})
            Reject(WithSection("SimplePaintShader_Body", "\"" + std::string(key) + "\":" + value),
                std::string("SimplePaintShader_Body.") + key);

    struct Range { const char* key; double minimum; double maximum; bool openMaximum = false; };
    for (const auto& range : {Range{"Brightness", SimplePaint::Margin, SimplePaint::InteriorMaximum},
        Range{"Shift", 0, SimplePaint::InteriorMaximum}, Range{"RotationDegrees", 0, 360, true},
        Range{"DarkPoint", 0, SimplePaint::InteriorMaximum}, Range{"LightPoint", SimplePaint::Margin, 1}})
    {
        for (const double value : {range.minimum, range.openMaximum ? std::nextafter(range.maximum, 0.0) : range.maximum})
            (void)Parse(WithSection("SimplePaintShader_Body", "\"" + std::string(range.key) + "\":" + Json(value).dump()));
        for (const double value : {std::nextafter(range.minimum, -1.0),
            range.openMaximum ? range.maximum : std::nextafter(range.maximum, 1000.0)})
            Reject(WithSection("SimplePaintShader_Body", "\"" + std::string(range.key) + "\":" + Json(value).dump()),
                "SimplePaintShader_Body");
    }
    for (const double value : {std::nextafter(SimplePaint::Margin, 0.0), std::nextafter(SimplePaint::InteriorMaximum, 1.0)})
        Reject(WithSection("SimplePaintShader_Body", "\"BaseColor\":[" + Json(value).dump() + ",0.2,0.3]"),
            "SimplePaintShader_Body");

    // JSON decimal numbers are rounded to binary64, including underflow to zero.
    const auto zero = Parse(WithSection("SimplePaintShader_Body", R"("Shift":0.0,"DarkPoint":0.0)"));
    const auto underflow = Parse(WithSection("SimplePaintShader_Body", R"("Shift":1e-999,"DarkPoint":-1e-999)"));
    Require(underflow.paintMaterials[1].warp[3] == 0 && underflow.paintMaterials[1].numeratorDark[0] == 0,
        "Underflow did not follow binary64 zero semantics");
    Require(SameMaterial(Parse(WithSection("SimplePaintShader_Body", R"("Shift":1e-999)")).paintMaterials[1],
        zero.paintMaterials[1]), "Positive underflow changed the material");
    Reject(WithSection("SimplePaintShader_Body", R"("Brightness":1e-999)"), "SimplePaintShader_Body");
    Reject(WithSection("SimplePaintShader_Body", R"("Shift":1e400)"), "out_of_range");
    Reject(std::string(R"({"RenderPipeline":{"Preset":"Standard","VSync":false,"MaxGpuFramesInFlight":1e400}})"),
        "out_of_range");
    const auto tinyShift = Parse(WithSection("SimplePaintShader_Body", R"("Shift":1e-100)"));
    Require(tinyShift.paintMaterials[1].warp[3] == 1, "Shift narrowed before double precision validation");
    (void)Parse(WithSection("SimplePaintShader_Body", R"("Shift":-0.0,"DarkPoint":0.8,"LightPoint":0.2)"));
    (void)Parse(WithSection("SimplePaintShader_Body", R"("DarkPoint":0.5,"LightPoint":0.5)"));
}

void CheckFiles()
{
    const auto shipped = LoadApplicationSettings(ModuleDirectory() / "assets" / "Settings.json");
    Require(shipped.renderPipelinePreset == RenderPipelinePreset::MinimizeInputLatency &&
        shipped.renderPipeline == MinimizeInputLatencyRenderPipeline && !shipped.vsync, "Shipped pipeline changed");
    Require(shipped.sphereUResolution == 64 && shipped.sphereVResolution == 32, "Shipped sphere changed");
    const auto defaults = Parse(Config("Standard"));
    for (std::size_t i = 0; i < shipped.paintMaterials.size(); ++i)
        Require(SameMaterial(shipped.paintMaterials[i], defaults.paintMaterials[i]), "Shipped material changed");

    const auto folder = std::filesystem::temp_directory_path() /
        ("SimpleDirectX12Settings-" + std::to_string(std::chrono::steady_clock::now().time_since_epoch().count()));
    Require(std::filesystem::create_directory(folder), "Could not create settings test directory");
    struct Cleanup
    {
        std::filesystem::path folder;
        ~Cleanup()
        {
            std::error_code error;
            std::filesystem::remove(folder / "malformed.json", error);
            std::filesystem::remove(folder / "invalid.json", error);
            std::filesystem::remove(folder, error);
        }
    } cleanup{folder};
    const auto malformed = folder / "malformed.json";
    { std::ofstream output(malformed); output << "{"; }
    const auto invalid = folder / "invalid.json";
    { std::ofstream output(invalid); output << WithSection("Sphere", R"("UResolution":2)"); }
    for (const auto& path : {malformed, invalid, folder / "missing.json"})
    {
        try { (void)LoadApplicationSettings(path); throw std::logic_error("Invalid file accepted"); }
        catch (const std::runtime_error& error)
        {
            Require(std::string(error.what()).starts_with(path.string() + ": "), "File path missing from diagnostic");
            if (path == invalid) Require(std::string(error.what()).find("Sphere.UResolution") != std::string::npos,
                "Property path missing from diagnostic");
        }
    }
}
}

int main()
{
    try
    {
        CheckPresets();
        CheckIntegers();
        CheckSyntaxAndDuplicates();
        CheckMaterials();
        CheckFiles();
        std::cout << "JSON settings, presets, strict validation, material precision, defaults, and file diagnostics passed.\n";
        return 0;
    }
    catch (const std::exception& error) { std::cerr << error.what() << '\n'; return 1; }
}
