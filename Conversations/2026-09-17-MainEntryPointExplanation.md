# Main entry point explanation

## User

provide a step by step breakdown explanation of this:
<<<LOCATION_START
Lines and columns are 1-based. Columns are StartInclusive:EndExclusive. Columns count UTF-16 code units.
c:\Users\k\Repository\Veehiicuul\Cpp\Source\Main.cpp:15:1-54:2
>>>LOCATION_END

## Assistant commentary

I’ll read the selected code and its surrounding context, then explain it step by step.

This function handles startup: it prepares the log, loads settings, and runs the application. It also shows an error dialog if a standard C++ exception occurs. I’ll break down the syntax and execution order, and record the explanation as required by the repository instructions.

## Assistant

The selected code in [Main.cpp](C:/Users/k/Repository/Veehiicuul/Cpp/Source/Main.cpp:15) is the application’s **entry point**: the function called when the Windows application starts. It prepares logging, loads settings, runs the application, and handles errors.

1. **Declare the entry-point function — line 15**

   ```cpp
   int WINAPI wWinMain(HINSTANCE instance, HINSTANCE, PWSTR, const int showCommand)
   ```

   - `int` means the function returns an integer exit code.
   - `WINAPI` specifies the Windows calling convention; Windows x64 uses its standard unified calling convention.
   - `wWinMain` is the Unicode Windows GUI entry point.
   - `instance` identifies this running executable module and is passed to the application when creating its window.
   - The second `HINSTANCE` is a legacy parameter. It is unnamed because this code does not use it.
   - `PWSTR` is the wide-character command-line string. It is also unnamed and unused.
   - `showCommand` supplies Windows’ requested initial window display state, such as normal or minimized. `const` prevents reassignment within this function.

2. **Create a log stream and begin the protected work — lines 17–19**

   ```cpp
   std::ofstream log;
   try
   {
   ```

   `log` is an output-file stream, initially without an open file. It is declared outside `try` so the later `catch` block can also use it.

   If code inside `try` throws an exception matching the `catch`, execution leaves the normal path and enters the error handler.

3. **Look for a supplied log directory — lines 20–24**

   ```cpp
   std::filesystem::path logDirectory;
   wchar_t* configuredDirectory = nullptr;
   std::size_t length = 0;
   if (_wdupenv_s(&configuredDirectory, &length, L"VEEHIICUUL_LOG_DIRECTORY") != 0)
       throw std::runtime_error("Could not read the session log directory.");
   ```

   `logDirectory` starts empty. `_wdupenv_s` reads the environment variable `VEEHIICUUL_LOG_DIRECTORY`, allocating a wide-character string for its value.

   The `&` operators pass the addresses of the pointer and length variables, allowing the function to fill them in. The `L` prefix marks a wide-character string literal. `length` receives the buffer length but is not used afterward.

   A nonzero return value indicates a read/allocation error, so the code throws an exception. An absent environment variable is handled later as a reason to use the default directory. The repository’s `Run.ps1` supplies this variable when launching the application.

4. **Copy the directory and release the temporary buffer — lines 25–27**

   ```cpp
   if (configuredDirectory && *configuredDirectory)
       logDirectory = configuredDirectory;
   std::free(configuredDirectory);
   ```

   The condition checks two things, in order:

   - `configuredDirectory`: the pointer is not null.
   - `*configuredDirectory`: the first character is not the terminating zero character, so the string is not empty.

   `&&` short-circuits, meaning the second check runs only if the first succeeds. This avoids dereferencing a null pointer.

   Assignment copies the string into `logDirectory`. `std::free` then releases the buffer allocated by `_wdupenv_s`; the copied path remains valid. Freeing a null pointer is also safe.

5. **Otherwise, generate a timestamped directory — lines 28–36**

   When no nonempty directory was supplied:

   ```cpp
   const auto now = std::time(nullptr);
   std::tm localTime{};
   localtime_s(&localTime, &now);
   ```

   This gets the current time and converts it into local calendar components. `{}` zero-initializes the `std::tm` structure before it is filled. `auto` asks the compiler to infer the variable’s type.

   ```cpp
   std::ostringstream timestamp;
   timestamp << std::put_time(&localTime, "%Y-%m-%d_%H-%M-%S");
   logDirectory = std::filesystem::path(APPLICATION_LOG_ROOT) / timestamp.str();
   ```

   The string stream builds a timestamp such as `2026-09-17_14-30-05`. `APPLICATION_LOG_ROOT` is defined by the build configuration as the application’s `MyLogOutput` directory.

   Here, `/` joins filesystem path components. The resulting path is something like `Cpp/MyLogOutput/2026-09-17_14-30-05`.

6. **Create the directory and open the log — lines 37–40**

   ```cpp
   std::filesystem::create_directories(logDirectory);
   log.open(logDirectory / L"Application.log", std::ios::app);
   if (!log) throw std::runtime_error("Could not open Application.log.");
   log << "Application started.\n" << std::flush;
   ```

   `create_directories` creates any missing directories in the path. `std::ios::app` opens the file for appending, preserving existing contents.

   `!log` checks whether the stream is in a failure state. If opening succeeded, `<<` writes the startup message, `\n` adds a newline, and `std::flush` pushes buffered output to the underlying file immediately.

7. **Create the application and load settings — lines 41–43**

   ```cpp
   Application application;
   const auto settings = LoadSettings(ModuleDirectory() / L"Assets" / L"Settings.json");
   log << "Settings loaded.\n" << std::flush;
   ```

   The first statement constructs an `Application` object. `ModuleDirectory()` returns the directory containing the executable, so settings are loaded from its adjacent `Assets/Settings.json`.

   `const auto` infers the returned settings type and prevents modifying this local settings object. The success message is written only after `LoadSettings` returns successfully.

8. **Run the application and return its exit code — lines 44–46**

   ```cpp
   const int result = application.Run(instance, showCommand, settings);
   log << "Application exited with code " << result << ".\n";
   return result;
   ```

   `Run` creates the window, initializes rendering, and processes messages and frames until the application exits. This call normally occupies most of the program’s lifetime.

   Once it returns, its exit code is logged and passed back to the operating system. Local objects are automatically destroyed when leaving their scopes; the log stream closes its file as part of that cleanup.

9. **Handle standard C++ exceptions — lines 48–53**

   ```cpp
   catch (const std::exception& error)
   {
       if (log) log << "Error: " << error.what() << std::endl;
       MessageBoxA(nullptr, error.what(), "Veehiicuul", MB_OK | MB_ICONERROR);
       return EXIT_FAILURE;
   }
   ```

   This catches `std::exception` and its derived exception types, including the `std::runtime_error` exceptions thrown above. The exception is accessed through a constant reference, avoiding a copy.

   - `error.what()` provides the error message.
   - `if (log)` writes it to the log only when the stream is in a usable state. `std::endl` adds a newline and flushes.
   - `MessageBoxA` displays the message in a dialog titled **Veehiicuul**. `nullptr` means no owner window; `MB_OK | MB_ICONERROR` combines an OK button with an error icon.
   - `EXIT_FAILURE` reports unsuccessful termination to the operating system.

   This handler covers matching C++ exceptions; it does not catch every possible kind of program failure.


## User

explain this in greater detail:
int WINAPI wWinMain(HINSTANCE instance, HINSTANCE, PWSTR, const int showCommand)

is this form of main only necessary if you want to create a graphical window?
for windows os, is the gui part of kernel or userspace?

## Assistant commentary

I’ll explain each part of the signature, check Microsoft’s documentation on Windows startup and the GUI architecture, and record the explanation in the conversation log.

## Assistant

**No—`wWinMain` is not required to create a graphical window.** A program starting at ordinary `main` can create windows too. This signature primarily goes with Windows’ convention for starting an application that does not automatically need a console.

**The Windows GUI spans both user mode and kernel mode.** Your application runs in user mode; some of the Windows infrastructure it calls runs in kernel mode.

Let’s unpack how those facts connect to this declaration.

```cpp
int WINAPI wWinMain(
    HINSTANCE instance,
    HINSTANCE,
    PWSTR,
    const int showCommand)
```

1. **`int`: the result returned when the application finishes**

   This is the function’s return type, just like in `int main()`. With normal runtime startup, its return value becomes the process exit status, which a launcher or script can inspect. Conventionally, zero means success and a nonzero value indicates failure.

2. **`WINAPI`: how the function is called at the machine level**

   A *calling convention* specifies details such as where arguments are placed and how the caller and called function cooperate.

   `WINAPI` is a Windows-header macro associated with `__stdcall`. The distinction mattered on 32-bit x86, where multiple conventions differed in argument passing and stack cleanup.

   **For your Windows x64 application, `__stdcall` is accepted but ignored:** the standard x64 calling convention applies. Keeping `WINAPI` follows the documented signature. It does not grant privileges, create a window, or make the function execute in kernel mode. [Microsoft: `__stdcall`](https://learn.microsoft.com/en-us/cpp/cpp/stdcall?view=msvc-170)

3. **`wWinMain`: the function name expected by this startup convention**

   `WinMain` is the traditional Windows application entry-function name. The leading `w` selects the variant that receives a wide-character command-line string, using UTF-16 on Windows.

   It does not automatically make every string or API call in the program Unicode.

   More precisely than my earlier explanation, the normal startup sequence is:

   ```text
   Windows loads the executable
       → C/C++ runtime startup code runs
       → runtime initializes global/static objects
       → runtime calls your wWinMain
       → your application code runs
   ```

   Thus, `wWinMain` is your application-level entry function, not the first machine instruction executed. The ordinary `main` convention also has runtime startup before it. [Microsoft: application entry point](https://learn.microsoft.com/en-us/windows/win32/learnwin32/winmain--the-application-entry-point)

4. **`HINSTANCE instance`: a handle identifying the executable module**

   `HINSTANCE` is the type; `instance` is the variable name chosen by this code.

   A *handle* is a value used to identify something to Windows. Here, it identifies the executable module loaded into this process. Windows APIs can use it when registering a window class or locating resources belonging to the executable.

   It is neither a process ID nor a window handle. A window is identified by a different type, `HWND`.

5. **The unnamed `HINSTANCE`: a historical argument**

   Written with a name, this would commonly be:

   ```cpp
   HINSTANCE previousInstance
   ```

   It exists for historical reasons and is always null on modern Windows. It does **not** tell you whether another copy of the application is running.

   C++ allows unused parameters to have a type without a name. The argument still exists in the signature; this function simply cannot refer to it by name.

6. **The unnamed `PWSTR`: the command-line text**

   `PWSTR` is a Windows alias for `wchar_t*`: a pointer to a wide-character string. With a name, this parameter could be written:

   ```cpp
   PWSTR commandLine
   ```

   For a launch such as:

   ```text
   Veehiicuul.exe --fullscreen --quality high
   ```

   it supplies the arguments after the executable name as one string, rather than an already separated `argc`/`argv` array. Your function leaves the parameter unnamed because it does not use it. [Microsoft: WinMain parameters](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-winmain)

7. **`const int showCommand`: the requested initial display state**

   This carries a value such as “show normally,” “show minimized,” or “show maximized.” The application passes it to window-display code, typically `ShowWindow`.

   `const` only prevents this function from assigning a new value to its local parameter. Because the argument is passed by value, removing that `const` would not change what the caller passes or the calling convention. [Microsoft: entry-point arguments](https://learn.microsoft.com/en-us/windows/win32/learnwin32/winmain--the-application-entry-point)

**When do you use this instead of `main`?**

With the usual Microsoft build conventions:

| Executable setting   | Usual entry function     | Automatic console behavior                 |
| -------------------- | ------------------------ | ------------------------------------------ |
| `/SUBSYSTEM:CONSOLE` | `main` or `wmain`         | Normally inherits or receives a console.   |
| `/SUBSYSTEM:WINDOWS` | `WinMain` or `wWinMain`   | Does not automatically receive a console.  |

These settings influence startup and console handling. They do not restrict whether your application can create graphical windows. A console application can display windows; a Windows-subsystem application can finish without creating any window at all. [Microsoft: subsystem settings](https://learn.microsoft.com/en-us/cpp/build/reference/subsystem-specify-subsystem?view=msvc-170)

For example, a console-target program can display a graphical dialog:

```cpp
#include <Windows.h>

int main()
{
    MessageBoxW(nullptr, L"Hello", L"A graphical window", MB_OK);
    return 0;
}
```

Your [CMakeLists.txt](C:/Users/k/Repository/Veehiicuul/Cpp/CMakeLists.txt:98) declares `add_executable(Veehiicuul WIN32 ...)`, selecting a Windows GUI executable. `wWinMain` fits that setup. Despite its historical name, that `WIN32` option does not mean the executable is 32-bit.

**Where does the GUI execute?**

User mode and kernel mode describe CPU execution privileges, rather than whether code belongs to Microsoft or your application. User-mode code operates with restrictions; kernel-mode code has privileged access to operating-system memory and hardware. [Microsoft: user mode and kernel mode](https://learn.microsoft.com/en-us/windows-hardware/drivers/gettingstarted/user-mode-and-kernel-mode)

Windows divides GUI work among several components:

| Component                                     | Execution mode |
| --------------------------------------------- | -------------- |
| Your application and its window-event handlers | User mode      |
| Desktop Window Manager (`dwm.exe`)             | User mode      |
| Parts of window management and GDI (`win32k`)   | Kernel mode    |
| Graphics runtime and graphics drivers          | Both           |

DWM combines window contents into the desktop image. It is a separate user-mode process. Supporting window-system components and the graphics kernel perform privileged work, while the display-driver model includes both user-mode and kernel-mode components. [Microsoft: composition architecture](https://learn.microsoft.com/en-us/windows/win32/directcomp/architecture-and-components), [Microsoft: WDDM architecture](https://learn.microsoft.com/en-us/windows-hardware/drivers/display/windows-vista-and-later-display-driver-model-architecture)

When your application requests an operation requiring kernel support, a system call transfers execution into Windows’ kernel-mode code. Control then returns to user mode. **Your `wWinMain` and application code remain user-mode code**, regardless of whether you create windows or run the program as administrator.
