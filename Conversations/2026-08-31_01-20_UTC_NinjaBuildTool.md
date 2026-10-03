# Ninja build tool

Date: 2026-08-30

Repository: `Simple_DirectX12_3D_Game`

## User

> what is "ninja" in the context of building this app

## Assistant

Ninja is the build executor used by this project's CMake configuration. It is not the C++ compiler: CMake generates Ninja's build instructions, and Ninja efficiently runs the required MSVC compiler, DXC shader-compiler, and linker commands in the correct order.

In `Run.cmd`, the project locates the copy of `ninja.exe` bundled with Visual Studio, passes `-G Ninja` to CMake, and then invokes `cmake --build`. On the first build Ninja builds the program and its shaders; on later builds it normally rebuilds only inputs that changed. It fills roughly the same role that MSBuild would fill with another CMake generator.

No project source files were changed. No images were attached to this conversation.
