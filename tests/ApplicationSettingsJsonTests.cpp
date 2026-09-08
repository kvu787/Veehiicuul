#include "ApplicationSettingsJson.h"
#include "ApplicationSettingsIO.h"
#include "SettingsValidation.h"
#include <chrono>
#include <fstream>
#include <iostream>
#include <sstream>
#include <stdexcept>

namespace
{
void Require(bool value) { if (!value) throw std::runtime_error("JSON settings contract failed"); }
ApplicationSettings Parse(const std::string& text)
{
    std::istringstream input(text);
    return Deserialize<ApplicationSettings>(input);
}
template<class Action> void Reject(Action action)
{
    try { action(); }
    catch (const std::exception&) { return; }
    throw std::runtime_error("Invalid input accepted");
}
}
int main()
{
    try
    {
        std::ifstream input("assets/Settings.json");
        const auto document = nlohmann::json::parse(input);
        const auto shipped = LoadApplicationSettings("assets/Settings.json");
        Require(nlohmann::json(shipped) == document);
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
        Reject([&] { Deserialize<ApplicationSettings>(failedInput); });
        std::ostringstream failedOutput;
        failedOutput.setstate(std::ios::badbit);
        Reject([&] { Serialize(failedOutput, shipped); });
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
        for (auto member : {&ApplicationSettings::SimplePaintShader_Axles, &ApplicationSettings::SimplePaintShader_Body,
            &ApplicationSettings::SimplePaintShader_Cabin, &ApplicationSettings::SimplePaintShader_Headlights,
            &ApplicationSettings::SimplePaintShader_Wheels, &ApplicationSettings::SimplePaintShader_Sphere})
            custom.*member = {.BaseColor={.2,.4,.6}, .Brightness=.7, .Shift=.3,
                .RotationDegrees=123, .DarkPoint=.8, .LightPoint=.2};
        std::ostringstream output;
        Serialize(output, custom);
        Require(Parse(output.str()) == custom);
        ValidateSettings(custom);
        auto extra = document;
        extra["Unknown"] = true;
        extra["Sphere"]["Unknown"] = false;
        Require(Parse(extra.dump()) == shipped);
        auto duplicate = document.dump();
        duplicate.insert(1, "\"Sphere\":{\"UResolution\":1,\"VResolution\":1},");
        Require(Parse(duplicate) == shipped); // nlohmann keeps the last duplicate.
        auto fractional = document;
        fractional["Sphere"]["UResolution"] = 64.5;
        auto parsed = Parse(fractional.dump());
        Require(parsed.Sphere.UResolution == 64.5);
        Reject([&] { ValidateSettings(parsed); });
        fractional["Sphere"]["UResolution"] = 64.0;
        ValidateSettings(Parse(fractional.dump()));
        extra = document;
        extra["SimplePaintShader_Body"]["BaseColor"].push_back(.5);
        parsed = Parse(extra.dump());
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
        Reject([&] { (void)LoadApplicationSettings(path); });
        { std::ofstream file(path); Serialize(file, custom); }
        Require(LoadApplicationSettings(path) == custom);
        { std::ofstream file(path); file << fractional.dump(); }
        Require(LoadApplicationSettings(path).Sphere.UResolution == 64);
        { std::ofstream file(path); file << extra.dump(); }
        try { (void)LoadApplicationSettings(path); throw std::logic_error("Invalid file accepted"); }
        catch (const std::runtime_error& error) { Require(std::string(error.what()).find(path.string()) != std::string::npos); }
        std::cout << "Typed JSON, required fields, round trips, and validated file loading passed.\n";
        return 0;
    }
    catch (const std::exception& error) { std::cerr << error.what() << '\n'; return 1; }
}

