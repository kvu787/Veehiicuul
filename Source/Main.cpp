#include "Application.h"
#include "SettingsIO.h"
#include "PlatformPaths.h"

#include <Windows.h>

#include <cstdlib>
#include <exception>
#include <fstream>
#include <iomanip>
#include <ctime>
#include <sstream>
#include <stdexcept>

int WINAPI wWinMain(HINSTANCE instance, HINSTANCE, PWSTR, const int showCommand)
{
    std::ofstream log;
    try
    {
        std::filesystem::path logDirectory;
        wchar_t* configuredDirectory = nullptr;
        std::size_t length = 0;
        if (_wdupenv_s(&configuredDirectory, &length, L"SIMPLE_DIRECTX12_LOG_DIRECTORY") != 0)
            throw std::runtime_error("Could not read the session log directory.");
        if (configuredDirectory && *configuredDirectory)
            logDirectory = configuredDirectory;
        std::free(configuredDirectory);
        if (logDirectory.empty())
        {
            const auto now = std::time(nullptr);
            std::tm localTime{};
            localtime_s(&localTime, &now);
            std::ostringstream timestamp;
            timestamp << std::put_time(&localTime, "%Y-%m-%d_%H-%M-%S");
            logDirectory = std::filesystem::path(APPLICATION_LOG_ROOT) / timestamp.str();
        }
        std::filesystem::create_directories(logDirectory);
        log.open(logDirectory / L"Application.log", std::ios::app);
        if (!log) throw std::runtime_error("Could not open Application.log.");
        log << "Application started.\n" << std::flush;
        Application application;
        const auto settings = LoadSettings(ModuleDirectory() / L"Assets" / L"Settings.json");
        log << "Settings loaded.\n" << std::flush;
        const int result = application.Run(instance, showCommand, settings);
        log << "Application exited with code " << result << ".\n";
        return result;
    }
    catch (const std::exception& error)
    {
        if (log) log << "Error: " << error.what() << std::endl;
        MessageBoxA(nullptr, error.what(), "Simple DirectX 12 Car", MB_OK | MB_ICONERROR);
        return EXIT_FAILURE;
    }
}
