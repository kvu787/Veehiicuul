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
set(BACKGROUND_SHADER_SOURCE "${CMAKE_CURRENT_SOURCE_DIR}/shaders/Background.hlsl")
set(SIMPLE_PAINT_SHADER_SOURCE "${CMAKE_CURRENT_SOURCE_DIR}/shaders/SimplePaint.hlsl")

function(compile_shader source_file output_name entry_point target_profile variable_name)
    set(object_file "${SHADER_GENERATED_DIRECTORY}/${output_name}.dxil")
    set(header_file "${SHADER_GENERATED_DIRECTORY}/${output_name}.h")

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
            -Qstrip_debug
            -Qstrip_reflect
            "${source_file}"
        DEPENDS "${source_file}"
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
compile_shader("${SIMPLE_PAINT_SHADER_SOURCE}" SimplePaintVS VSMain vs_6_0 g_simplePaintVertexShader)
compile_shader("${SIMPLE_PAINT_SHADER_SOURCE}" SimplePaintPS PSMain ps_6_0 g_simplePaintPixelShader)

add_custom_target(CompiledShaders DEPENDS ${SHADER_OUTPUTS})

set(CAR_MESH_HEADER "${CMAKE_CURRENT_SOURCE_DIR}/src/generated/CarMesh.generated.h")
add_executable(SimpleDirectX12Game WIN32
    ${SHADER_HEADERS}
    ${BACKGROUND_SHADER_SOURCE}
    ${SIMPLE_PAINT_SHADER_SOURCE}
    ${CAR_MESH_HEADER}
    src/Application.cpp
    src/Application.h
    src/Main.cpp
    src/Renderer.cpp
    src/Renderer.h
    src/UVSphere.h
)

add_dependencies(SimpleDirectX12Game CompiledShaders)
set_source_files_properties(
    "${BACKGROUND_SHADER_SOURCE}"
    "${SIMPLE_PAINT_SHADER_SOURCE}"
    PROPERTIES HEADER_FILE_ONLY TRUE
)
target_include_directories(SimpleDirectX12Game PRIVATE "${SHADER_GENERATED_DIRECTORY}")

set(BACKGROUND_IMAGE "${CMAKE_CURRENT_SOURCE_DIR}/assets/SceneBackground.png")
set(CAR_PAINT_SETTINGS "${CMAKE_CURRENT_SOURCE_DIR}/assets/CarPaint.ini")
add_custom_target(RuntimeAssets ALL
    COMMAND "${CMAKE_COMMAND}" -E make_directory "$<TARGET_FILE_DIR:SimpleDirectX12Game>/assets"
    COMMAND "${CMAKE_COMMAND}" -E copy_if_different
        "${BACKGROUND_IMAGE}"
        "$<TARGET_FILE_DIR:SimpleDirectX12Game>/assets/SceneBackground.png"
    COMMAND "${CMAKE_COMMAND}" -E copy_if_different
        "${CAR_PAINT_SETTINGS}"
        "$<TARGET_FILE_DIR:SimpleDirectX12Game>/assets/CarPaint.ini"
    DEPENDS "${BACKGROUND_IMAGE}" "${CAR_PAINT_SETTINGS}"
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
    d3d12
    dxgi
    ole32
    windowscodecs
)

set_property(DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}" PROPERTY VS_STARTUP_PROJECT SimpleDirectX12Game)

include(CTest)
if(BUILD_TESTING)
    add_executable(UVSphereTests tests/UVSphereTests.cpp)
    target_include_directories(UVSphereTests PRIVATE src)
    target_compile_features(UVSphereTests PRIVATE cxx_std_20)
    add_test(NAME UVSphereGeometry COMMAND UVSphereTests)
endif()
