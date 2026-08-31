# HLSL file layout

Date: 2026-08-30

Repository: `Simple_DirectX12_3D_Game`

## User

> is hlsl shader code always in this format: C:\Users\k\Repository\Simple_DirectX12_3D_Game\src\SceneShader.h ?
>
> can it be in a separate hlsl file or something like that

## Assistant

No. `SceneShader.h` embeds normal HLSL in a C++ raw string so the executable can compile it from memory. The project passes that string to `D3DCompile`; the supplied name `SceneShader.hlsl` is only a diagnostic source name and does not refer to an actual file.

HLSL is commonly stored in a separate `.hlsl` file. It can be compiled at runtime with `D3DCompileFromFile`, although that requires locating and shipping the source file, or compiled during the build into shader bytecode (`.cso`/DXIL), which the application loads without shipping the source or compiler. Build-time compilation is the recommended production layout; runtime file compilation is convenient during development. Vertex and pixel entry points may share one `.hlsl` file, as they do here, or use separate files.

For this project, a clean layout would be `shaders/Scene.hlsl`, compiled twice for `VSMain` and `PSMain`. Because the renderer currently uses the legacy `d3dcompiler` API and Shader Model 5.1 targets, moving the text to a file is a small change; adopting modern Shader Model 6 would instead use DXC.

No project source files were changed. No images were attached to this conversation.

## User

> For this project, I’d use shaders/Scene.hlsl and compile it twice—once for VSMain and once for PSMain.
>
> why would you "compile it twice" if there is only one file?

## Assistant

Because a shader compilation produces bytecode for one entry point and one pipeline stage, not one output for the entire source file. `Scene.hlsl` contains two separate GPU programs: `VSMain` is compiled with the vertex-shader target `vs_5_1`, and `PSMain` is compiled with the pixel-shader target `ps_5_1`. This produces two distinct bytecode blobs, which DirectX 12 places into the vertex-shader and pixel-shader fields of the pipeline state respectively.

“Compile it twice” therefore means running the compiler twice against the same source text with different entry-point and target arguments. This project already does exactly that at runtime in `Renderer.cpp`. Splitting the functions into `SceneVS.hlsl` and `ScenePS.hlsl` would still require two compiler invocations—one per shader stage.
