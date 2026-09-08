#include "PlatformPaths.h"
#include <Windows.h>
#include <format>
#include <stdexcept>
#include <string>

std::filesystem::path ModuleDirectory()
{
    std::wstring modulePath(32768, L'\0');
    const DWORD length = GetModuleFileNameW(nullptr, modulePath.data(), static_cast<DWORD>(modulePath.size()));
    if (length == 0 || static_cast<std::size_t>(length) >= modulePath.size())
        throw std::runtime_error(std::format("GetModuleFileNameW failed with Win32 error {}.", GetLastError()));
    modulePath.resize(length);
    return std::filesystem::path(modulePath).parent_path();
}
