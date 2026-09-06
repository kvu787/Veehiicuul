# C++ IDE recommendations

## User

list the best IDEs to use to read and develop the c++ code in this project. exclude vscode.

## Assistant (commentary)

I’ll check the project’s build setup and constraints, then compare suitable C++ IDEs, excluding VS Code.

## Assistant (commentary)

Visual Studio is the strongest fit because the launcher already uses its C++ toolchain, CMake, and Ninja. CLion is my second choice for code navigation and refactoring; Qt Creator is another viable CMake IDE. I’m recording the recommendation in `Conversations` as required by the repository instructions.

## Assistant

**My ranking for this project: Visual Studio, CLion, then Qt Creator.** The codebase uses C++20, Win32, DirectX 12, HLSL, and CMake, and its launcher already depends on Visual Studio’s build tools.

1. **Visual Studio — best overall fit.** Its native CMake support provides code browsing, IntelliSense, building, and debugging without creating a `.sln` file. This is my recommendation for developing and debugging this Windows application. Open the repository using **File → Open → Folder**, with the **Desktop development with C++** workload and CMake tools installed. [Microsoft documentation](https://learn.microsoft.com/en-us/cpp/build/cmake-projects-in-visual-studio?view=msvc-170)

2. **CLion — best alternative for reading and refactoring C++.** Strong navigation, usage searches, code analysis, and refactoring tools make it particularly appealing for exploring the renderer and SimplePaint module. It supports CMake and the MSVC toolchain, including an LLDB-based Windows debugger. For this repository, select the **Visual Studio/MSVC x64 toolchain**. [Code features](https://www.jetbrains.com/clion/features/), [Windows setup](https://www.jetbrains.com/help/clion/quick-tutorial-on-configuring-clion-on-windows.html)

3. **Qt Creator — a solid third option.** It supports CMake projects, semantic code navigation, and MSVC compilers. You can use it for this existing C++ project without converting the application to Qt. Configure an **MSVC x64 kit** with CMake and a Windows debugger. I would choose it primarily if you already like its workflow. [Project support](https://doc.qt.io/qtcreator/creator-project-opening.html), [Compiler support](https://doc.qt.io/qtcreator/creator-tool-chains.html)

**Project-specific setup:** Keep the Visual Studio components and Windows SDK installed even if you edit in CLion or Qt Creator: `Run.cmd` currently relies on them. IDE builds also need access to the SDK’s `dxc.exe` shader compiler. Give each IDE its own build directory to avoid conflicting with the launcher’s `build\release` cache.

