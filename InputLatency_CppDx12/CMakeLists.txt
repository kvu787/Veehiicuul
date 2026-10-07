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
add_executable(InputLatency WIN32 Source/Main.cpp Source/Renderer.cpp Source/InputMonitor.cpp
    "${SHADER_DIRECTORY}/VertexShader.h" "${SHADER_DIRECTORY}/PixelShader.h")
target_include_directories(InputLatency PRIVATE Source ThirdParty/GameInput/Include "${SHADER_DIRECTORY}")
target_compile_features(InputLatency PRIVATE cxx_std_20)
target_compile_definitions(InputLatency PRIVATE NOMINMAX WIN32_LEAN_AND_MEAN UNICODE _UNICODE)
target_compile_options(InputLatency PRIVATE /W4 /WX /EHsc /permissive- /utf-8
    $<$<CONFIG:Release>:/O2> $<$<CONFIG:Release>:/GL>)
set_property(TARGET InputLatency PROPERTY MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>")
target_link_options(InputLatency PRIVATE $<$<CONFIG:Release>:/LTCG> /DYNAMICBASE /NXCOMPAT)
target_link_libraries(InputLatency PRIVATE d3d12 dxgi dxguid gdi32 user32 shell32 windowscodecs ole32)
add_custom_command(TARGET InputLatency POST_BUILD
    COMMAND "${CMAKE_COMMAND}" -E copy_directory "${CMAKE_CURRENT_SOURCE_DIR}/ThirdParty/GameInput/Runtime" "$<TARGET_FILE_DIR:InputLatency>"
    COMMAND "${CMAKE_COMMAND}" -E copy_if_different "${CMAKE_CURRENT_SOURCE_DIR}/ThirdParty/GameInput/LICENSE.txt" "$<TARGET_FILE_DIR:InputLatency>/GameInputLicense.txt"
    COMMAND "${CMAKE_COMMAND}" -E copy_if_different "${CMAKE_CURRENT_SOURCE_DIR}/ThirdParty/GameInput/NOTICE.txt" "$<TARGET_FILE_DIR:InputLatency>/GameInputNotice.txt")
include(CTest)
if(BUILD_TESTING)
    foreach(test MeasurementTests ControllerStateTests)
        add_executable(${test} "Tests/${test}.cpp")
        target_include_directories(${test} PRIVATE Source)
        target_compile_features(${test} PRIVATE cxx_std_20)
        target_compile_options(${test} PRIVATE /W4 /WX /EHsc /permissive-)
        set_property(TARGET ${test} PROPERTY MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>")
        add_test(NAME ${test} COMMAND ${test})
    endforeach()
    add_test(NAME ApplicationSmoke COMMAND InputLatency --duration 3 --hidden --software-adapter --capture-frame)
    set_tests_properties(ApplicationSmoke PROPERTIES TIMEOUT 30)
endif()
