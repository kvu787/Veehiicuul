# Settings architecture

[ApplicationSettings.h](../Source/ApplicationSettings.h) is the single C++ model
of [Settings.json](../assets/Settings.json). Its nested types group the fields
found in the file. Read this header to see the field names and C++ types.
For accepted values and when settings apply, see
[render pipeline configuration](RenderPipeline.md) for presets and custom controls,
and [SimplePaint usage](Usage.md#controls) for finite paint limits and
[application settings](Usage.md#run-and-edit-this-application) for sphere resolutions
and JSON format rules. The [specification](Specification.md#accepted-machine-inputs)
explains the paint limits in detail.

The model declares no configuration defaults: configured values come
from the JSON file, and every field is required. nlohmann value-initializes the
object before filling it; zero initialization never substitutes for a missing
JSON field. Direct C++ callers must supply a complete object before validation.
Tests use explicit test inputs, while JSON tests read the shipped file.

[ApplicationSettings.cpp](../Source/ApplicationSettings.cpp) reads the file in
two explicit steps:

```cpp
auto settings = Deserialize<ApplicationSettings>(input);
ValidateSettings(settings);
```

[ApplicationSettingsJson.h](../Source/ApplicationSettingsJson.h) lists fields
using `NLOHMANN_DEFINE_TYPE_NON_INTRUSIVE`. The generic
[JsonSerialization.h](../Source/JsonSerialization.h) delegates parsing and typed
conversion to nlohmann/json. `Serialize(output, settings)` performs the reverse
operation. A local nlohmann serializer policy in
[JsonIntegerConversion.h](../Source/JsonIntegerConversion.h) checks integer
conversion; other types retain the library's normal conversions. There are no
handwritten JSON walkers, parser callbacks, or per-field converters. nlohmann
reports syntax and missing-field errors; the adapter also rejects integer type
mismatches and overflow.
Unknown properties are ignored and the last duplicate wins.

[SettingsValidation.cpp](../Source/SettingsValidation.cpp) contains nonmutating
application checks using ordinary C++ values: allowed preset names, integer
count ranges, RGB length, and paint domains. It calls SimplePaint's pure C++
`ValidateParameters` for material rules. Neither this validator nor its test
target includes or links nlohmann/json.

Counts use `std::int32_t`. The JSON adapter accepts only integer tokens within
the destination type's representable range. For example, `64` is accepted;
`64.0`, `64.`, exponent notation, and quoted strings are rejected. The parser
already distinguishes integer tokens from floating-point tokens, so the adapter
does not examine or reparse token text. It checks signed and unsigned integer
storage before casting, preventing truncation and wraparound. These conversion
rules apply even when a fixed preset makes a count inactive. Application limits
such as sphere U resolution in [3, 512] remain in the separate validator.

`BaseColor` uses `std::vector<double>` to preserve the complete input array
until application validation requires exactly three sRGB channels in R, G, B
order. Paint numbers continue to follow the library's binary64 conversion and
domain checks.

[RenderPreparation.cpp](../Source/RenderPreparation.cpp) resolves a pipeline
preset into `ResolvedRenderPipeline` and compiles paint values into
`SimplePaint::GpuMaterial`. The renderer owns those derived values and converts
validated sphere counts to mesh dimensions. They are not another settings model.
Preparation also validates direct C++ inputs before conversion.

When adding a field, declare it in `ApplicationSettings.h`, list it in the
corresponding JSON macro, add it to the shipped JSON, and add any domain rule to
the plain C++ validator. Update the consuming renderer code and relevant tests.
