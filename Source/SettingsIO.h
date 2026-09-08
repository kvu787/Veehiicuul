#pragma once

#include "Settings.h"
#include <filesystem>

// Reads the complete file model, validates it, and returns it without resolving presets.
[[nodiscard]] Settings LoadSettings(const std::filesystem::path& path);
