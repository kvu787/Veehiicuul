#include "SettingsIO.h"
#include "SettingsJson.h"
#include "SettingsValidation.h"

#include <fstream>
#include <format>
#include <stdexcept>

Settings LoadSettings(const std::filesystem::path& path)
{
    try
    {
        std::ifstream input(path, std::ios::binary);
        if (!input) throw std::runtime_error("Could not open settings file.");

        auto settings = Deserialize<Settings>(input);
        ValidateSettings(settings);
        return settings;
    }
    catch (const std::exception& error)
    {
        throw std::runtime_error(std::format("{}: {}", path.string(), error.what()));
    }
}
