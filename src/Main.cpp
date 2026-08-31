#include "Application.h"

#include <Windows.h>

#include <cstdlib>
#include <exception>

int WINAPI wWinMain(HINSTANCE instance, HINSTANCE, PWSTR, const int showCommand)
{
    try
    {
        Application application;
        return application.Run(instance, showCommand);
    }
    catch (const std::exception& error)
    {
        MessageBoxA(nullptr, error.what(), "Simple DirectX 12 Scene", MB_OK | MB_ICONERROR);
        return EXIT_FAILURE;
    }
}
