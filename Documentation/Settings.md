# Settings architecture

[ApplicationSettings.h](../Source/ApplicationSettings.h) is the single C++ model
of [Settings.json](../assets/Settings.json). Its nested types group the fields
found in the file. Read this header to see all available settings and their
domains. Initializers match the shipped file and support direct C++ construction;
missing JSON fields are errors, not requests for defaults.

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
operation. There are no handwritten JSON walkers, parser callbacks, or custom
field converters. nlohmann reports syntax, missing-field, and type errors;
unknown properties are ignored and the last duplicate wins.

[SettingsValidation.cpp](../Source/SettingsValidation.cpp) contains nonmutating
application checks using ordinary C++ values: allowed preset names, whole-number
count ranges, RGB length, and paint domains. It calls SimplePaint's pure C++
`ValidateParameters` for material rules. Neither this validator nor its test
target includes or links nlohmann/json.

Counts use `double` to retain fractional values until validation, avoiding
silent integer truncation during deserialization. RGB uses `std::vector<double>`
so its length can be checked afterward. Numeric values follow the library's
binary64 conversion; this is not arbitrary-precision decimal validation.

[RenderPreparation.cpp](../Source/RenderPreparation.cpp) resolves a pipeline
preset into `ResolvedRenderPipeline` and compiles paint values into
`SimplePaint::GpuMaterial`. The renderer owns those derived values and converts
validated sphere counts to mesh dimensions. They are not another settings model.
Preparation also validates direct C++ inputs before conversion.

When adding a field, declare it in `ApplicationSettings.h`, list it in the
corresponding JSON macro, add it to the shipped JSON, and add any domain rule to
the plain C++ validator. Update the consuming renderer code and relevant tests.
