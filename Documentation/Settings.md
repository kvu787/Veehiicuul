# Settings architecture

[Settings.h](../Source/Settings.h) is the single C++ model
of [Settings.json](../assets/Settings.json). Its nested types group the fields
found in the file. Read this header to see the field names and C++ types.
For accepted values and when settings apply, see
[render pipeline configuration](RenderPipeline.md) for presets and custom controls,
and [SimplePaint usage](../Source/SimplePaint/Usage.md#controls) for finite paint limits and
[application settings](#run-and-edit-this-application) for sphere resolutions
and JSON format rules. The [specification](../Source/SimplePaint/Specification.md#accepted-machine-inputs)
explains the paint limits in detail.

The model declares no configuration defaults: configured values come
from the JSON file, and every field is required. nlohmann value-initializes the
object before filling it; zero initialization never substitutes for a missing
JSON field. Direct C++ callers must supply a complete object before validation.
Tests use explicit test inputs, while JSON tests read the shipped file.

[Settings.cpp](../Source/Settings.cpp) reads the file in
two explicit steps:

```cpp
auto settings = Deserialize<Settings>(input);
ValidateSettings(settings);
```

[SettingsJson.h](../Source/SettingsJson.h) lists fields
using a local input-only macro built from nlohmann field expansion helpers. It
produces only `from_json` overloads. The generic
[JsonDeserialization.h](../Source/JsonDeserialization.h) delegates parsing and typed
conversion to nlohmann/json. A local nlohmann serializer policy in
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

When adding a field, declare it in `Settings.h`, list it in the
corresponding JSON macro, add it to the shipped JSON, and add any domain rule to
the plain C++ validator. Update the consuming renderer code and relevant tests.

## Run and edit this application

Double-click [Run.cmd](../Run.cmd). It builds and launches the application using Visual Studio's C++ tools, CMake, Ninja, and the Windows SDK's DXC shader compiler. See [README.md](../README.md) for installation requirements and app controls.

Edit [assets/Settings.json](../assets/Settings.json), then launch again. The six `SimplePaintShader_*` objects provide independent controls for Axles, Body, Cabin, Headlights, Wheels, and Sphere. The setting name for Rotation is `RotationDegrees`.

For example, edit these fields within the complete file to change the sphere's
paint and select the minimum-latency pipeline:

```json
{
  "RenderPipeline": {
    "Preset": "MinimizeInputLatency",
    "VSync": false
  },
  "SimplePaintShader_Sphere": {
    "BaseColor": [0.107, 0.223, 0.578],
    "Brightness": 0.5,
    "Shift": 0.6,
    "RotationDegrees": 45,
    "DarkPoint": 0.05,
    "LightPoint": 0.95
  }
}
```

The example above shows sections to edit within the complete
[settings file](../assets/Settings.json); retain every other field, including
the six custom controls in `RenderPipeline`.

Every declared section and field is required, even when a fixed pipeline
preset makes a control inactive. Names and enum strings are case-sensitive.
JSON uses quoted names and strings, `true`/`false` booleans, and numeric arrays
for colors. Comments, trailing commas, malformed JSON, and wrong field types
fail. Unknown properties are ignored; the last duplicate property wins.
Errors include the file path; syntax errors include the parser's location,
and application validation identifies the offending setting or material.

Sphere U resolution must be an integer from 3 through 512, and V from 2
through 512. Count fields require JSON integer tokens within the signed 32-bit
range. Decimal points, exponent notation, quoted strings, and booleans are
rejected: use `64`, not `64.0`, `64.`, `6.4e1`, or `"64"`. See [render pipeline configuration](RenderPipeline.md)
for custom ranges and [Settings architecture](Settings.md) for the code.

Paint numbers are rounded to binary64 before material validation. Overflow fails;
underflow may round to zero, which is accepted only for parameters whose domain
includes zero. For example, `"Shift": 1e-999` becomes zero, while
`"Brightness": 1e-999` fails its lower bound. See the
[numerical contract](../Source/SimplePaint/Specification.md) for boundary details.

The removed `FacingCutoff` property has no effect, like any unknown property.
Settings load from the executable's adjacent `assets` directory;
`Run.cmd` copies the repository settings there during the build.
A missing or invalid file fails startup. There is no INI fallback.


## JSON numerical conversion

The application's loader uses the vendored [nlohmann/json 3.12.0](../ThirdParty/nlohmann_json/README.md).
JSON decimal numbers are rounded to binary64 before conversion to `Parameters`;
the contract is not exact arbitrary-precision decimal arithmetic. Paint controls
accept JSON integer or floating-point numbers, and `BaseColor` must contain
exactly three numeric array elements. Material validation still runs before
narrowing to binary32.

Malformed JSON, trailing content, comments, trailing commas, NaN/infinity
literals, overflow beyond finite binary64, and wrong field types fail.
nlohmann/json ignores unknown properties during typed conversion and retains
the last duplicate property. Overflow anywhere in the document fails parsing.

Underflow follows nlohmann/json's binary64 conversion: sufficiently tiny decimals
round to signed zero. The parsed binary64 value is then validated, so
`1e-999` is accepted as zero for Shift or Dark Point but rejected for
Brightness or an RGB channel. Representable subnormal values remain subject to
the same domain checks. Negative zero is accepted wherever zero is valid;
fractional notation (such as `-0.0`) retains its sign. This policy is covered
by settings tests and does not add shader clamps or coefficient floors.

All declared sections and properties are required. Deserialization uses
input-only declarative mappings to populate one `Settings` object.
`ValidateSettings` then checks that object using plain C++. The six custom
pipeline controls are always present and typed, but their domain limits are
checked only for `Custom`. Count fields use `std::int32_t`. The JSON adapter
requires integer tokens representable in that type before conversion; decimal
points, exponents, quoted strings, and overflow are rejected, even for inactive
controls. Thus `64` is accepted, while `64.0`, `64.`, and `6.4e1` are rejected.
Application ranges are checked afterward. See [Settings.md](Settings.md),
[application settings](#run-and-edit-this-application), and [RenderPipeline.md](RenderPipeline.md).
