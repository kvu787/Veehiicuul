#pragma once

#include "ApplicationSettings.h"

// Application-domain checks on ordinary C++ values. Never mutates settings.
// Throws std::invalid_argument identifying the setting that failed validation.
void ValidateSettings(const ApplicationSettings& settings);
void ValidatePipeline(const struct ApplicationSettings::RenderPipeline& pipeline);
