#include "ApplicationSettingsIO.h"
#include "ApplicationSettingsJson.h"
#include "SettingsValidation.h"

#include <fstream>
#include <format>
#include <stdexcept>

ApplicationSettings LoadApplicationSettings(const std::filesystem::path& path)
{
    try
    {
        std::ifstream input(path, std::ios::binary);
        if (!input) throw std::runtime_error("Could not open settings file.");

        auto settings = Deserialize<ApplicationSettings>(input);
        ValidateSettings(settings);
        return settings;
    }
    catch (const std::exception& error)
    {
        throw std::runtime_error(std::format("{}: {}", path.string(), error.what()));
    }
}
