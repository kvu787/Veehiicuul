#include "Application.h"
#include "SettingsIO.h"
#include "PlatformPaths.h"

#include <Windows.h>

#include <cstdlib>
#include <exception>

int WINAPI wWinMain(HINSTANCE instance, HINSTANCE, PWSTR, const int showCommand)
{
    try
    {
        Application application;
        const auto settings = LoadSettings(ModuleDirectory() / L"Assets" / L"Settings.json");
        return application.Run(instance, showCommand, settings);
    }
    catch (const std::exception& error)
    {
        MessageBoxA(nullptr, error.what(), "Simple DirectX 12 Car", MB_OK | MB_ICONERROR);
        return EXIT_FAILURE;
    }
}
