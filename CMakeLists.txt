cmake_minimum_required(VERSION 3.21)

project(SimpleDirectX12Game LANGUAGES CXX)

find_program(DXC_EXECUTABLE NAMES dxc.exe dxc)
if(NOT DXC_EXECUTABLE AND DEFINED ENV{WindowsSdkVerBinPath})
    find_program(DXC_EXECUTABLE
        NAMES dxc.exe
        PATHS "$ENV{WindowsSdkVerBinPath}/x64"
        NO_DEFAULT_PATH
    )
endif()
if(NOT DXC_EXECUTABLE)
    message(FATAL_ERROR
        "DirectX Shader Compiler (dxc.exe) was not found. "
        "Install a current Windows SDK and configure from a Visual Studio developer environment."
    )
endif()

set(SHADER_GENERATED_DIRECTORY "${CMAKE_CURRENT_BINARY_DIR}/generated/shaders")
set(BACKGROUND_SHADER_SOURCE "${CMAKE_CURRENT_SOURCE_DIR}/Source/Shaders/Background.hlsl")
set(SIMPLE_PAINT_SHADER_SOURCE "${CMAKE_CURRENT_SOURCE_DIR}/Source/SimplePaint/SimplePaint.hlsl")
set(SIMPLE_PAINT_CORE "${CMAKE_CURRENT_SOURCE_DIR}/Source/SimplePaint/SimplePaintCore.hlsli")
set(SIMPLE_PAINT_MATERIAL_COUNT 6)

function(compile_shader source_file output_name entry_point target_profile variable_name)
    set(object_file "${SHADER_GENERATED_DIRECTORY}/${output_name}.dxil")
    set(header_file "${SHADER_GENERATED_DIRECTORY}/${output_name}.h")
    set(shader_definitions)
    if(source_file STREQUAL SIMPLE_PAINT_SHADER_SOURCE)
        set(shader_definitions -D "SIMPLE_PAINT_MATERIAL_COUNT=${SIMPLE_PAINT_MATERIAL_COUNT}")
    endif()

    add_custom_command(
        OUTPUT "${object_file}" "${header_file}"
        COMMAND "${CMAKE_COMMAND}" -E make_directory "${SHADER_GENERATED_DIRECTORY}"
        COMMAND "${DXC_EXECUTABLE}"
            -E "${entry_point}"
            -T "${target_profile}"
            -Fo "${object_file}"
            -Fh "${header_file}"
            -Vn "${variable_name}"
            -O3
            -Ges
            -WX
            -all_resources_bound
            ${shader_definitions}
            -Qstrip_debug
            -Qstrip_reflect
            "${source_file}"
        DEPENDS "${source_file}" ${ARGN}
        COMMENT "Compiling ${output_name} as ${target_profile} with DXC"
        VERBATIM
    )

    set(SHADER_OUTPUTS
        ${SHADER_OUTPUTS}
        "${object_file}"
        "${header_file}"
        PARENT_SCOPE
    )
    set(SHADER_HEADERS
        ${SHADER_HEADERS}
        "${header_file}"
        PARENT_SCOPE
    )
endfunction()

compile_shader("${BACKGROUND_SHADER_SOURCE}" BackgroundVS VSMain vs_6_0 g_backgroundVertexShader)
compile_shader("${BACKGROUND_SHADER_SOURCE}" BackgroundPS PSMain ps_6_0 g_backgroundPixelShader)
compile_shader("${SIMPLE_PAINT_SHADER_SOURCE}" SimplePaintVS VSMain vs_6_0 g_simplePaintVertexShader "${SIMPLE_PAINT_CORE}")
compile_shader("${SIMPLE_PAINT_SHADER_SOURCE}" SimplePaintPS PSMain ps_6_0 g_simplePaintPixelShader "${SIMPLE_PAINT_CORE}")

add_custom_target(CompiledShaders DEPENDS ${SHADER_OUTPUTS})

add_subdirectory(Source/SimplePaint)
add_subdirectory(ThirdParty/nlohmann_json)

add_library(SettingsValidation Source/SettingsValidation.cpp Source/ApplicationSettings.h Source/SettingsValidation.h Source/PaintParameters.h)
target_link_libraries(SettingsValidation PUBLIC SimplePaint)
add_library(RenderPreparation Source/RenderPreparation.cpp Source/RenderPreparation.h)
target_link_libraries(RenderPreparation PUBLIC SettingsValidation)
add_library(PlatformPaths Source/PlatformPaths.cpp Source/PlatformPaths.h)
add_library(ApplicationSettings Source/ApplicationSettings.cpp Source/ApplicationSettingsIO.h Source/ApplicationSettingsJson.h Source/JsonSerialization.h)
target_link_libraries(ApplicationSettings PUBLIC SettingsValidation PRIVATE nlohmann_json::nlohmann_json)
foreach(target SettingsValidation RenderPreparation PlatformPaths ApplicationSettings)
    target_include_directories(${target} PUBLIC Source)
    target_compile_features(${target} PUBLIC cxx_std_20)
    target_compile_definitions(${target} PRIVATE NOMINMAX WIN32_LEAN_AND_MEAN UNICODE _UNICODE)
    if(MSVC)
        target_compile_options(${target} PRIVATE /W4 /WX /EHsc /permissive-)
    endif()
endforeach()

set(CAR_MESH_HEADER "${CMAKE_CURRENT_SOURCE_DIR}/Source/generated/CarMesh.generated.h")
add_executable(SimpleDirectX12Game WIN32
    ${SHADER_HEADERS}
    ${BACKGROUND_SHADER_SOURCE}
    ${SIMPLE_PAINT_SHADER_SOURCE}
    ${CAR_MESH_HEADER}
    Source/Application.cpp
    Source/Application.h
    Source/Main.cpp
    Source/Renderer.cpp
    Source/Renderer.h
    Source/UVSphere.h
)

add_dependencies(SimpleDirectX12Game CompiledShaders)
set_source_files_properties(
    "${BACKGROUND_SHADER_SOURCE}"
    "${SIMPLE_PAINT_SHADER_SOURCE}"
    PROPERTIES HEADER_FILE_ONLY TRUE
)
target_include_directories(SimpleDirectX12Game PRIVATE "${SHADER_GENERATED_DIRECTORY}")

set(BACKGROUND_IMAGE "${CMAKE_CURRENT_SOURCE_DIR}/assets/SceneBackground.png")
set(GAME_SETTINGS "${CMAKE_CURRENT_SOURCE_DIR}/assets/Settings.json")
add_custom_target(RuntimeAssets ALL
    COMMAND "${CMAKE_COMMAND}" -E make_directory "$<TARGET_FILE_DIR:SimpleDirectX12Game>/assets"
    COMMAND "${CMAKE_COMMAND}" -E copy_if_different
        "${BACKGROUND_IMAGE}"
        "$<TARGET_FILE_DIR:SimpleDirectX12Game>/assets/SceneBackground.png"
    COMMAND "${CMAKE_COMMAND}" -E copy_if_different
        "${GAME_SETTINGS}"
        "$<TARGET_FILE_DIR:SimpleDirectX12Game>/assets/Settings.json"
    DEPENDS "${BACKGROUND_IMAGE}" "${GAME_SETTINGS}"
    COMMENT "Staging runtime image and paint settings"
    VERBATIM
)
add_dependencies(SimpleDirectX12Game RuntimeAssets)

find_program(BLENDER_EXECUTABLE
    NAMES blender.exe
    PATHS
        "$ENV{USERPROFILE}/Program/blender-4.5.12-windows-x64"
        "$ENV{USERPROFILE}/Program/blender-5.2.0-windows-x64"
    NO_DEFAULT_PATH
)
if(BLENDER_EXECUTABLE)
    add_custom_target(RegenerateAssets
        COMMAND "${BLENDER_EXECUTABLE}"
            --background
            --factory-startup
            --disable-autoexec
            "${CMAKE_CURRENT_SOURCE_DIR}/Blender/Car.blend"
            --python-exit-code 1
            --python "${CMAKE_CURRENT_SOURCE_DIR}/tools/GenerateAssets.py"
            --
            --car-output "${CAR_MESH_HEADER}"
            --background-output "${BACKGROUND_IMAGE}"
        WORKING_DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}"
        COMMENT "Regenerating the car mesh and flattened background with Blender"
        VERBATIM
    )
endif()

target_compile_features(SimpleDirectX12Game PRIVATE cxx_std_20)
target_compile_definitions(SimpleDirectX12Game PRIVATE
    NOMINMAX
    UNICODE
    _UNICODE
    WIN32_LEAN_AND_MEAN
    SIMPLE_PAINT_MATERIAL_COUNT=${SIMPLE_PAINT_MATERIAL_COUNT}
)

if(MSVC)
    target_compile_options(SimpleDirectX12Game PRIVATE
        /W4
        /permissive-
        /EHsc
        /Zc:__cplusplus
    )
endif()

target_link_libraries(SimpleDirectX12Game PRIVATE
    ApplicationSettings RenderPreparation PlatformPaths
    d3d12
    dxgi
    ole32
    windowscodecs
)

set_property(DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}" PROPERTY VS_STARTUP_PROJECT SimpleDirectX12Game)

include(CTest)
if(BUILD_TESTING)
    add_test(NAME SimplePaintStandalone
        COMMAND "${CMAKE_COMMAND}"
            "-DMODULE_DIR=${CMAKE_CURRENT_SOURCE_DIR}/Source/SimplePaint"
            "-DCONSUMER_DIR=${CMAKE_CURRENT_SOURCE_DIR}/tests/SimplePaintStandalone"
            "-DSMOKE_ROOT=${CMAKE_CURRENT_BINARY_DIR}/SimplePaintCopyCheck"
            "-DGENERATOR=${CMAKE_GENERATOR}"
            "-DCXX_COMPILER=${CMAKE_CXX_COMPILER}"
            "-DMAKE_PROGRAM=${CMAKE_MAKE_PROGRAM}"
            "-DCONFIGURATION=$<CONFIG>"
            "-DDXC_EXECUTABLE=${DXC_EXECUTABLE}"
            "-DCTEST_COMMAND=${CMAKE_CTEST_COMMAND}"
            -P "${CMAKE_CURRENT_SOURCE_DIR}/tests/VerifySimplePaintStandalone.cmake")
    set_tests_properties(SimplePaintStandalone PROPERTIES TIMEOUT 120)
    add_executable(SimplePaintTests tests/SimplePaintTests.cpp)
    target_link_libraries(SimplePaintTests PRIVATE SimplePaint)
    add_test(NAME SimplePaintContract COMMAND SimplePaintTests)
    add_executable(SimplePaintGpuTests tests/SimplePaintGpuTests.cpp ${SHADER_HEADERS})
    add_dependencies(SimplePaintGpuTests CompiledShaders)
    target_include_directories(SimplePaintGpuTests PRIVATE "${SHADER_GENERATED_DIRECTORY}")
    target_compile_definitions(SimplePaintGpuTests PRIVATE NOMINMAX WIN32_LEAN_AND_MEAN)
    target_link_libraries(SimplePaintGpuTests PRIVATE SimplePaint d3d12 dxgi)
    if(MSVC)
        target_compile_options(SimplePaintTests PRIVATE /W4 /WX /EHsc /fp:precise)
        target_compile_options(SimplePaintGpuTests PRIVATE /W4 /WX /EHsc /fp:precise)
    endif()
    add_test(NAME SimplePaintGpuHardware COMMAND SimplePaintGpuTests)
    add_test(NAME SimplePaintGpuWarp COMMAND SimplePaintGpuTests --warp)
    set_tests_properties(SimplePaintGpuHardware SimplePaintGpuWarp PROPERTIES TIMEOUT 120 RUN_SERIAL TRUE)
    add_executable(UVSphereTests tests/UVSphereTests.cpp)
    target_include_directories(UVSphereTests PRIVATE Source)
    target_compile_features(UVSphereTests PRIVATE cxx_std_20)
    add_test(NAME UVSphereGeometry COMMAND UVSphereTests)
    add_executable(OrthographicTransformTests tests/OrthographicTransformTests.cpp)
    target_include_directories(OrthographicTransformTests PRIVATE Source)
    target_compile_features(OrthographicTransformTests PRIVATE cxx_std_20)
    add_test(NAME OrthographicTransforms COMMAND OrthographicTransformTests)
endif()

if(BUILD_TESTING)
    add_executable(ApplicationSettingsTests tests/ApplicationSettingsTests.cpp)
    target_link_libraries(ApplicationSettingsTests PRIVATE RenderPreparation)
    add_dependencies(ApplicationSettingsTests RuntimeAssets)
    add_test(NAME ApplicationSettingsContract COMMAND ApplicationSettingsTests)
    add_executable(ApplicationSettingsJsonTests tests/ApplicationSettingsJsonTests.cpp)
    target_link_libraries(ApplicationSettingsJsonTests PRIVATE ApplicationSettings nlohmann_json::nlohmann_json)
    add_dependencies(ApplicationSettingsJsonTests RuntimeAssets)
    add_test(NAME ApplicationSettingsJsonContract COMMAND ApplicationSettingsJsonTests)
    if(MSVC)
        target_compile_options(ApplicationSettingsJsonTests PRIVATE /W4 /WX /EHsc /permissive-)
    endif()
    add_executable(RenderPipelineGpuTests tests/RenderPipelineGpuTests.cpp Source/Renderer.cpp ${SHADER_HEADERS})
    add_dependencies(RenderPipelineGpuTests CompiledShaders RuntimeAssets)
    target_include_directories(RenderPipelineGpuTests PRIVATE "${SHADER_GENERATED_DIRECTORY}")
    target_compile_definitions(RenderPipelineGpuTests PRIVATE NOMINMAX UNICODE _UNICODE WIN32_LEAN_AND_MEAN SIMPLE_PAINT_MATERIAL_COUNT=${SIMPLE_PAINT_MATERIAL_COUNT})
    target_link_libraries(RenderPipelineGpuTests PRIVATE RenderPreparation PlatformPaths d3d12 dxgi ole32 windowscodecs)
    if(MSVC)
        target_compile_options(ApplicationSettingsTests PRIVATE /W4 /WX /EHsc /permissive-)
        target_compile_options(RenderPipelineGpuTests PRIVATE /W4 /WX /EHsc /permissive-)
    endif()
    add_test(NAME RenderPipelineGpuHardware COMMAND RenderPipelineGpuTests)
    add_test(NAME RenderPipelineGpuWarp COMMAND RenderPipelineGpuTests --warp)
    set_tests_properties(RenderPipelineGpuHardware RenderPipelineGpuWarp PROPERTIES TIMEOUT 90 RUN_SERIAL TRUE)
endif()

if(BUILD_TESTING)
    add_executable(ApplicationSmokeTests tests/ApplicationSmokeTests.cpp Source/Application.cpp Source/Renderer.cpp ${SHADER_HEADERS})
    add_dependencies(ApplicationSmokeTests CompiledShaders RuntimeAssets)
    target_include_directories(ApplicationSmokeTests PRIVATE "${SHADER_GENERATED_DIRECTORY}")
    target_compile_definitions(ApplicationSmokeTests PRIVATE NOMINMAX UNICODE _UNICODE WIN32_LEAN_AND_MEAN SIMPLE_PAINT_MATERIAL_COUNT=${SIMPLE_PAINT_MATERIAL_COUNT})
    target_link_libraries(ApplicationSmokeTests PRIVATE RenderPreparation PlatformPaths d3d12 dxgi ole32 windowscodecs)
    if(MSVC)
        target_compile_options(ApplicationSmokeTests PRIVATE /W4 /WX /EHsc /permissive-)
    endif()
    add_test(NAME ApplicationWindowLifecycle COMMAND ApplicationSmokeTests)
    add_test(NAME ApplicationMaximizeFpsWindowLifecycle COMMAND ApplicationSmokeTests --maximize-fps)
    set_tests_properties(ApplicationWindowLifecycle ApplicationMaximizeFpsWindowLifecycle PROPERTIES TIMEOUT 30 RUN_SERIAL TRUE)
endif()
