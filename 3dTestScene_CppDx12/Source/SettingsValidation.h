#pragma once

#include "Settings.h"

// Application-domain checks on ordinary C++ values. Never mutates settings.
// Throws std::invalid_argument identifying the setting that failed validation.
void ValidateSettings(const Settings& settings);
void ValidatePipeline(const struct Settings::RenderPipeline& pipeline);
