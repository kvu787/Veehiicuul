# Use a fresh directory each time: stale files must never supply an accidentally
# missing package dependency. All outputs stay in the calling build directory.
string(RANDOM LENGTH 12 ALPHABET 0123456789abcdef run_id)
set(project "${SMOKE_ROOT}/${run_id}/Project")
set(build "${SMOKE_ROOT}/${run_id}/Build")
file(MAKE_DIRECTORY "${project}")
file(COPY "${MODULE_DIR}" DESTINATION "${project}")
file(COPY "${CONSUMER_DIR}/CMakeLists.txt" "${CONSUMER_DIR}/Main.cpp" DESTINATION "${project}")

function(run_checked)
    execute_process(COMMAND ${ARGV} RESULT_VARIABLE result)
    if(NOT result EQUAL 0)
        message(FATAL_ERROR "Copied SimplePaint check failed (${result}): ${ARGV}")
    endif()
endfunction()

run_checked("${CMAKE_COMMAND}" -S "${project}" -B "${build}" -G "${GENERATOR}"
    "-DCMAKE_CXX_COMPILER=${CXX_COMPILER}" "-DCMAKE_MAKE_PROGRAM=${MAKE_PROGRAM}"
    "-DCMAKE_BUILD_TYPE=${CONFIGURATION}" "-DDXC_EXECUTABLE=${DXC_EXECUTABLE}")
run_checked("${CMAKE_COMMAND}" --build "${build}" --config "${CONFIGURATION}" --parallel)
run_checked("${CTEST_COMMAND}" --test-dir "${build}" -C "${CONFIGURATION}" --output-on-failure)

# The configurable array must reject a count that cannot describe a material.
execute_process(COMMAND "${DXC_EXECUTABLE}" -E PSMain -T ps_6_0 -D SIMPLE_PAINT_MATERIAL_COUNT=0
    -Fo "${build}/invalid-count.dxil" "${project}/SimplePaint/SimplePaint.hlsl"
    RESULT_VARIABLE invalid_count OUTPUT_QUIET ERROR_VARIABLE diagnostic)
if(invalid_count EQUAL 0 OR NOT diagnostic MATCHES "SIMPLE_PAINT_MATERIAL_COUNT must be positive")
    message(FATAL_ERROR "Copied shader did not reject an invalid material count: ${diagnostic}")
endif()
