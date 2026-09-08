# nlohmann/json

Vendored **3.12.0**, with the upstream single header and MIT license unchanged.
Builds use this checked-in dependency and require no network access or package manager.

- [Release and published checksum](https://github.com/nlohmann/json/releases/tag/v3.12.0)
- [Header source](https://raw.githubusercontent.com/nlohmann/json/v3.12.0/single_include/nlohmann/json.hpp)
- [License source](https://raw.githubusercontent.com/nlohmann/json/v3.12.0/LICENSE.MIT)
- Header SHA-256: `aaf127c04cb31c406e5b04a63f1ae89369fccde6d8fa7cdda1ed4f32dfc5de63`

The local CMake interface target is `nlohmann_json::nlohmann_json`. It disables
implicit conversions; callers still check numeric types and ranges explicitly.
Only the application settings loader and its contract tests need this dependency.
The reusable SimplePaint module is independent of JSON.

To update, replace the header and license from a pinned upstream release, verify
its published header checksum, update this file, and run both Release and Debug
tests through `Run.ps1`.
