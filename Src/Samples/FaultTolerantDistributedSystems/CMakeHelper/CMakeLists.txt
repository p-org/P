SET(CMAKE_BUILD_TYPE Debug)
SET(CMAKE_VERBOSE_MAKEFILE true)

###########################################################################
## P Compilation Related Helper Functions
###########################################################################

## Compile P source
set(P_COMPILER_PATH ${CMAKE_SOURCE_DIR}/Submodule/P/Bld/Drops/Debug/x86/Binaries/Pc.exe)
set(P_BUILT_LIB_PATH ${CMAKE_SOURCE_DIR}/Submodule/P/Bld/Drops/PrtUser)
set(P_LIB_INCLUDE_PATH ${P_BUILT_LIB_PATH}/include)
set(P_LIB_LINK_PATH ${P_BUILT_LIB_PATH}/lib)


function (p_compile pproject p_srcs)
    if (NOT (TARGET Prt))
            add_library(Prt SHARED IMPORTED)
            set_target_properties(Prt PROPERTIES IMPORTED_LOCATION ${P_LIB_LINK_PATH}/libPrt_shared.so)
            add_definitions( -DPRT_PLAT_LINUXUSER ) # currently ros only supports *nix/OSX
    endif()

    set(p_compile_cmd mono ${P_COMPILER_PATH})
    MESSAGE("--------- Compiling ----------------")
    MESSAGE("${${p_srcs}}")
    FOREACH(p_src ${${p_srcs}})

        get_filename_component(p_src_basename ${p_src} NAME_WE)
        set(generated_c_src ${p_src_basename}.c)
        set(output_dir ${CMAKE_CURRENT_SOURCE_DIR}/Generated)
        set(output_c_srcs ${output_dir}/${generated_c_src})
        file(MAKE_DIRECTORY ${output_dir})
        ADD_CUSTOM_TARGET("compile_${pproject}_${p_src_basename}" ALL
                COMMENT "Compiling P ${p_src}..."
                COMMAND ${p_compile_cmd} /generate:C0 ${p_src}
                WORKING_DIRECTORY ${output_dir}
        )
        set(${pproject}_GENERATED_SRCS ${${pproject}_GENERATED_SRCS} ${output_c_srcs} PARENT_SCOPE)
        set(${pproject}_INCLUDE_DIRS ${output_dir} ${P_LIB_INCLUDE_PATH} PARENT_SCOPE)
        set(${pproject}_LINK_LIBS Prt PARENT_SCOPE)
    ENDFOREACH(p_src)

endfunction(p_compile)

function (p_add_linker_dependencies pproject p_srcs)
    FOREACH(infileName ${${p_srcs}})
        SET(allfiles ${allfiles} "${infileName}")
        get_filename_component(temp ${infileName} NAME_WE)
        SET(dependencies ${dependencies} "compile_${pproject}_${temp}")
    ENDFOREACH(infileName)
    add_dependencies(linker_${pproject} ${dependencies})
endfunction(p_add_linker_dependencies)


function (p_link pproject formula_srcs)
    set(p_link_cmd mono ${P_COMPILER_PATH} /link)
    set(output_dir ${CMAKE_CURRENT_SOURCE_DIR}/Generated)
    set(output_c_srcs ${output_dir}/linker.c)

    MESSAGE("--------- linking ----------------")
    MESSAGE("${${formula_srcs}}")

    FOREACH(infileName ${${formula_srcs}})
        SET(allfiles ${allfiles} "${infileName}")
    ENDFOREACH(infileName)

    ADD_CUSTOM_TARGET(
            linker_${pproject} ALL
            COMMAND ${CMAKE_COMMAND} -E make_directory ${output_dir}
            COMMENT "Linking P ${p_src}..."
            COMMAND ${p_link_cmd} /outputDir:${output_dir} ${allfiles}
            WORKING_DIRECTORY ${output_dir}
    )
    set(${pproject}_GENERATED_SRCS ${${pproject}_GENERATED_SRCS} ${output_c_srcs} PARENT_SCOPE)
endfunction(p_link)


##########################################################################
include(CheckCXXCompilerFlag)
CHECK_CXX_COMPILER_FLAG("-std=c++11" COMPILER_SUPPORTS_CXX11)
CHECK_CXX_COMPILER_FLAG("-std=c++0x" COMPILER_SUPPORTS_CXX0X)
if(COMPILER_SUPPORTS_CXX11)
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -std=c++11")
elseif(COMPILER_SUPPORTS_CXX0X)
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -std=c++0x")
else()
        message(STATUS "The compiler ${CMAKE_CXX_COMPILER} has no C++11 support. Please use a different C++ compiler.")
endif()

set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -fPIC")



#############################################
## Compile using mex
#############################################
function (mex_compile src_dir c_srcs inc_dir)
    MESSAGE("--------- Compiling Mex ----------------")

    ADD_CUSTOM_TARGET("compile_mex" ALL
            COMMENT "Compiling Mex..."
            COMMAND mex -v -client engine -I${inc_dir} ${${c_srcs}}
            WORKING_DIRECTORY ${src_dir}
    )
endfunction(mex_compile)
