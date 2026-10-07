cmake_minimum_required(VERSION 3.25)
# Embedded debug information avoids launching a persistent compiler PDB server.
set(CMAKE_MSVC_DEBUG_INFORMATION_FORMAT "$<$<CONFIG:Debug,RelWithDebInfo>:Embedded>")
project(InputLatency LANGUAGES CXX)
if(NOT CMAKE_HOST_WIN32 OR CMAKE_HOST_SYSTEM_VERSION VERSION_LESS "10.0.22000"
    OR NOT CMAKE_HOST_SYSTEM_PROCESSOR STREQUAL "AMD64")
    message(FATAL_ERROR "Development requires Windows 11 x64.")
endif()
if(NOT MSVC OR NOT WIN32 OR NOT CMAKE_SIZEOF_VOID_P EQUAL 8
    OR NOT CMAKE_CXX_COMPILER_ARCHITECTURE_ID STREQUAL "x64")
    message(FATAL_ERROR "Build with MSVC for Windows x64.")
endif()
find_program(DXC_EXECUTABLE NAMES dxc.exe HINTS "$ENV{WindowsSdkVerBinPath}/x64" REQUIRED)
set(PRESENTMON_DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}/ThirdParty/PresentMon")
add_library(PresentMonDecoder STATIC
    ${PRESENTMON_DIRECTORY}/PresentData/PresentMonTraceConsumer.cpp
    ${PRESENTMON_DIRECTORY}/PresentData/TraceConsumer.cpp
    ${PRESENTMON_DIRECTORY}/PresentData/TraceLogging.cpp
    ${PRESENTMON_DIRECTORY}/PresentData/GpuTrace.cpp
    ${PRESENTMON_DIRECTORY}/PresentData/NvidiaTraceConsumer.cpp
    ${PRESENTMON_DIRECTORY}/PresentData/InterPresentActivity.cpp
    ${PRESENTMON_DIRECTORY}/PresentData/ProviderConfiguration.cpp
    ${PRESENTMON_DIRECTORY}/PresentData/ConsoleFormatting.cpp
    ${PRESENTMON_DIRECTORY}/IntelPresentMon/CommonUtilities/Hash.cpp)
target_compile_features(PresentMonDecoder PRIVATE cxx_std_20)
# Use the upstream production decoder behavior in both application configs;
# modal CRT assertion dialogs must never block a background ETW consumer.
target_compile_definitions(PresentMonDecoder PRIVATE NOMINMAX WIN32_LEAN_AND_MEAN UNICODE _UNICODE NDEBUG PRESENTMON_ENABLE_DEBUG_TRACE=0)
target_compile_options(PresentMonDecoder PRIVATE /W3 /EHsc /utf-8 $<$<CONFIG:Release>:/O2> $<$<CONFIG:Release>:/GL>)
set_property(TARGET PresentMonDecoder PROPERTY MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>")
set(SHADER_DIRECTORY "${CMAKE_CURRENT_BINARY_DIR}/Generated")
foreach(stage Vertex Pixel)
    if(stage STREQUAL "Vertex")
        set(profile vs_6_0)
    else()
        set(profile ps_6_0)
    endif()
    add_custom_command(OUTPUT "${SHADER_DIRECTORY}/${stage}Shader.h"
        COMMAND "${CMAKE_COMMAND}" -E make_directory "${SHADER_DIRECTORY}"
        COMMAND "${DXC_EXECUTABLE}" -E "${stage}Main" -T ${profile} -O3 -Ges -WX
            -Qstrip_debug -Qstrip_reflect -Fh "${SHADER_DIRECTORY}/${stage}Shader.h"
            -Vn "${stage}Shader" "${CMAKE_CURRENT_SOURCE_DIR}/Source/Dashboard.hlsl"
        DEPENDS Source/Dashboard.hlsl VERBATIM)
endforeach()
add_executable(InputLatency WIN32 Source/Main.cpp Source/Renderer.cpp Source/InputMonitor.cpp Source/DisplayTracker.cpp
    "${SHADER_DIRECTORY}/VertexShader.h" "${SHADER_DIRECTORY}/PixelShader.h")
target_include_directories(InputLatency PRIVATE Source ThirdParty/GameInput/Include "${SHADER_DIRECTORY}")
target_include_directories(InputLatency SYSTEM PRIVATE "${PRESENTMON_DIRECTORY}/PresentData")
target_compile_features(InputLatency PRIVATE cxx_std_20)
target_compile_definitions(InputLatency PRIVATE NOMINMAX WIN32_LEAN_AND_MEAN UNICODE _UNICODE PRESENTMON_ENABLE_DEBUG_TRACE=0)
target_compile_options(InputLatency PRIVATE /W4 /WX /EHsc /permissive- /utf-8
    $<$<CONFIG:Release>:/O2> $<$<CONFIG:Release>:/GL>)
set_property(TARGET InputLatency PROPERTY MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>")
target_link_options(InputLatency PRIVATE $<$<CONFIG:Release>:/LTCG> /DYNAMICBASE /NXCOMPAT)
target_link_libraries(InputLatency PRIVATE PresentMonDecoder d3d12 dxgi dxguid gdi32 user32 shell32 windowscodecs ole32 advapi32 tdh)
add_custom_command(TARGET InputLatency POST_BUILD
    COMMAND "${CMAKE_COMMAND}" -E copy_directory "${CMAKE_CURRENT_SOURCE_DIR}/ThirdParty/GameInput/Runtime" "$<TARGET_FILE_DIR:InputLatency>"
    COMMAND "${CMAKE_COMMAND}" -E copy_if_different "${CMAKE_CURRENT_SOURCE_DIR}/ThirdParty/GameInput/LICENSE.txt" "$<TARGET_FILE_DIR:InputLatency>/GameInputLicense.txt"
    COMMAND "${CMAKE_COMMAND}" -E copy_if_different "${CMAKE_CURRENT_SOURCE_DIR}/ThirdParty/GameInput/NOTICE.txt" "$<TARGET_FILE_DIR:InputLatency>/GameInputNotice.txt"
    COMMAND "${CMAKE_COMMAND}" -E copy_if_different "${PRESENTMON_DIRECTORY}/LICENSE.txt" "$<TARGET_FILE_DIR:InputLatency>/PresentMonLicense.txt")
include(CTest)
if(BUILD_TESTING)
    find_program(POWERSHELL_EXECUTABLE NAMES pwsh REQUIRED)
    foreach(test MeasurementTests ControllerStateTests DisplayTimingTests)
        add_executable(${test} "Tests/${test}.cpp")
        target_include_directories(${test} PRIVATE Source)
        target_compile_features(${test} PRIVATE cxx_std_20)
        target_compile_options(${test} PRIVATE /W4 /WX /EHsc /permissive-)
        set_property(TARGET ${test} PROPERTY MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>")
        add_test(NAME ${test} COMMAND ${test})
    endforeach()
    add_test(NAME SessionValidationTests COMMAND "${POWERSHELL_EXECUTABLE}" -NoProfile -File "${CMAKE_CURRENT_SOURCE_DIR}/Tests/SessionValidationTests.ps1")
    add_test(NAME SessionDirectoryTests COMMAND "${POWERSHELL_EXECUTABLE}" -NoProfile -File "${CMAKE_CURRENT_SOURCE_DIR}/Tests/SessionDirectoryTests.ps1" -Executable "$<TARGET_FILE:InputLatency>")
    set_tests_properties(SessionDirectoryTests PROPERTIES TIMEOUT 60)
    add_test(NAME ApplicationSmoke COMMAND InputLatency --duration 3 --hidden --software-adapter --capture-frame)
    set_tests_properties(ApplicationSmoke PROPERTIES TIMEOUT 30)
endif()
