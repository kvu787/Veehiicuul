# CSharp Language Version

## User

answer for the following question for each of ["general c# project", "godot 4.7.2 c# project"]:
what is the right way to specify the c# language version

## Assistant Commentary

I’ll check the official C# and Godot documentation for how each project type selects its language version.

## Assistant

**Both use the same setting: `<LangVersion>` in the project’s `.csproj`.** Usually, though, you should omit it and let the target framework select the appropriate C# version.

**General C# project**

For a modern .NET project, the recommended default is to specify `TargetFramework` and leave `LangVersion` unset. For example, `net8.0` defaults to C# 12, `net9.0` to C# 13, and `net10.0` to C# 14. [Microsoft’s versioning documentation](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-versioning)

If you want an explicit language version, add it to a property group:

```xml
<PropertyGroup>
  <TargetFramework>net8.0</TargetFramework>
  <LangVersion>12.0</LangVersion>
</PropertyGroup>
```

For multiple projects, you can put `LangVersion` in a shared `Directory.Build.props` instead. Choose a version supported by your compiler and target framework. [Microsoft’s configuration documentation](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/configure-language-version)

**Godot 4.7.2 C# project**

Use that same `LangVersion` property in the Godot-generated `.csproj`, inside an existing `PropertyGroup`. Keep its existing Godot SDK declaration and target framework. Godot 4.7.2’s SDK imports `Microsoft.NET.Sdk`, so the standard .NET mechanism applies. [Godot 4.7.2 SDK source](https://raw.githubusercontent.com/godotengine/godot/4.7.2-stable/modules/mono/editor/Godot.NET.Sdk/Godot.NET.Sdk/Sdk/Sdk.props)

For example, **if your Godot project targets `net8.0`**, omit `LangVersion` for C# 12 automatically, or add `<LangVersion>12.0</LangVersion>` to make that choice explicit.

**For both:** avoid `latest` for reproducible builds. Also, `<LangVersion>default</LangVersion>` is **not equivalent to omitting the property**: it selects the compiler’s latest major language version instead of the framework-appropriate version. [Microsoft’s explanation](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-versioning)
