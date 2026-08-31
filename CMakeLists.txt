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

set(SCENE_SHADER_SOURCE "${CMAKE_CURRENT_SOURCE_DIR}/shaders/Scene.hlsl")
set(SHADER_GENERATED_DIRECTORY "${CMAKE_CURRENT_BINARY_DIR}/generated/shaders")

function(compile_scene_shader output_name entry_point target_profile variable_name)
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
            -Qstrip_debug
            -Qstrip_reflect
            "${SCENE_SHADER_SOURCE}"
        DEPENDS "${SCENE_SHADER_SOURCE}"
        COMMENT "Compiling ${entry_point} as ${target_profile} with DXC"
        VERBATIM
    )

    set(SCENE_SHADER_OUTPUTS
        ${SCENE_SHADER_OUTPUTS}
        "${object_file}"
        "${header_file}"
        PARENT_SCOPE
    )
    set(SCENE_SHADER_HEADERS
        ${SCENE_SHADER_HEADERS}
        "${header_file}"
        PARENT_SCOPE
    )
endfunction()

compile_scene_shader(SceneVS VSMain vs_6_0 g_sceneVertexShader)
compile_scene_shader(ScenePS PSMain ps_6_0 g_scenePixelShader)

add_custom_target(CompiledSceneShaders DEPENDS ${SCENE_SHADER_OUTPUTS})

add_executable(SimpleDirectX12Game WIN32
    ${SCENE_SHADER_HEADERS}
    ${SCENE_SHADER_SOURCE}
    src/Application.cpp
    src/Application.h
    src/Geometry.cpp
    src/Geometry.h
    src/Main.cpp
    src/Renderer.cpp
    src/Renderer.h
)

add_dependencies(SimpleDirectX12Game CompiledSceneShaders)
set_source_files_properties("${SCENE_SHADER_SOURCE}" PROPERTIES HEADER_FILE_ONLY TRUE)
target_include_directories(SimpleDirectX12Game PRIVATE "${SHADER_GENERATED_DIRECTORY}")

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
)

set_property(DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}" PROPERTY VS_STARTUP_PROJECT SimpleDirectX12Game)
