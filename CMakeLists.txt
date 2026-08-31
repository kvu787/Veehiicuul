cmake_minimum_required(VERSION 3.21)

project(SimpleDirectX12Game LANGUAGES CXX)

add_executable(SimpleDirectX12Game WIN32
    src/Application.cpp
    src/Application.h
    src/Geometry.cpp
    src/Geometry.h
    src/Main.cpp
    src/Renderer.cpp
    src/Renderer.h
    src/SceneShader.h
)

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
    d3dcompiler
    dxgi
)

set_property(DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}" PROPERTY VS_STARTUP_PROJECT SimpleDirectX12Game)
