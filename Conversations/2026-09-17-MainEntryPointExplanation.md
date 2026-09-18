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
