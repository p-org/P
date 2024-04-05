cmake_minimum_required (VERSION 3.1)

project (P)

if(${CMAKE_SYSTEM_NAME} MATCHES "Linux")
	set(LINUX ON)
endif()

if(${CMAKE_SYSTEM_NAME} MATCHES "Darwin")
	set(MACOSX ON)
endif()

if (NOT Win32)
	if ("${CMAKE_CXX_COMPILER_ID}" STREQUAL "Clang")
	  add_definitions( -DPRT_USE_CLANG)
	elseif ("${CMAKE_CXX_COMPILER_ID}" STREQUAL "GNU")
	  add_definitions( -DPRT_USE_GCC )
	endif()
endif()

if(SGX)
    add_definitions( -DPRT_PLAT_SGXUSER )
elseif(Win32)
    add_definitions( -DPRT_PLAT_WINUSER )
elseif(LINUX OR MACOSX)
    add_definitions( -DPRT_PLAT_LINUXUSER )
endif()

macro ( Publish_Library_Header target )
	set (extra_macro_args ${ARGN})
	list(LENGTH extra_macro_args num_extra_args)
	if(${num_extra_args} EQUAL 0)
		get_property(Published_Headers_PATHS TARGET ${target} PROPERTY INTERFACE_INCLUDE_DIRECTORIES)
	else()
		set(Published_Headers_PATHS "${ARGN}")
	endif()
	add_custom_command(TARGET ${target} POST_BUILD
	  COMMENT "Moving header files to Bld/include/"
	  COMMAND ${CMAKE_COMMAND} ARGS -E
	    make_directory ${LIBRARY_OUTPUT_INCLUDE_PATH}
	)

	foreach(incl_file_path ${Published_Headers_PATHS})
		file ( GLOB incl_files ${incl_file_path}/*.h )
		foreach(incl_file ${incl_files})
			add_custom_command(TARGET ${target} POST_BUILD
				COMMAND ${CMAKE_COMMAND} -E copy_if_different
					${incl_file}
			       	${LIBRARY_OUTPUT_INCLUDE_PATH}
			)
		endforeach()
	endforeach()
endmacro()

add_subdirectory ( Prt )

