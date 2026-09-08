#pragma once

#include "ApplicationSettings.h"
#include <filesystem>

// Reads the complete file model, validates it, and returns it without resolving presets.
[[nodiscard]] ApplicationSettings LoadApplicationSettings(const std::filesystem::path& path);
