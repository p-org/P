set ( P_Prt_Src_Path ${CMAKE_CURRENT_SOURCE_DIR} )
set ( API_PATH ${P_Prt_Src_Path}/API/ )
set ( Core_PATH ${P_Prt_Src_Path}/Core/ )

if(SGX)
    set ( User_PATH ${P_Prt_Src_Path}/SGXUser/ )
elseif(Win32)
	set ( User_PATH ${P_Prt_Src_Path}/WinUser/ )
elseif(LINUX OR MACOSX)
	set ( User_PATH ${P_Prt_Src_Path}/LinuxUser/ )
endif()

set( PrtUser_Output_PATH ${PROJECT_SOURCE_DIR}/../Bld/Drops/PrtUser/)
set( LIBRARY_OUTPUT_PATH ${PrtUser_Output_PATH}/lib/ )
set( LIBRARY_OUTPUT_INCLUDE_PATH ${PrtUser_Output_PATH}/include/ )

file ( GLOB API_SRC ${API_PATH}/*.c ${API_PATH}/*.h )
file ( GLOB Core_SRC ${Core_PATH}/*.c ${Core_PATH}/*.h )
file ( GLOB User_SRC ${User_PATH}/*.c ${User_PATH}/*.h )

set ( PrtUserSrc ${API_SRC} ${Core_SRC} ${User_SRC} )

add_library(Prt OBJECT ${PrtUserSrc})

if (NOT Win32 AND NOT DEFINED SGX)
	find_package(Threads REQUIRED)
	if(THREADS_HAVE_PTHREAD_ARG)
	  set_property(TARGET Prt PROPERTY COMPILE_OPTIONS "-pthread")
	  set_property(TARGET Prt PROPERTY INTERFACE_COMPILE_OPTIONS "-pthread")
	endif()
	if(${CMAKE_THREAD_LIBS_INIT})
	  target_link_libraries(Prt ${CMAKE_THREAD_LIBS_INIT})
	endif()
endif()

set_property(TARGET Prt PROPERTY C_STANDARD 99)

IF( CMAKE_SYSTEM_PROCESSOR STREQUAL "x86_64" )
  SET_TARGET_PROPERTIES(Prt PROPERTIES COMPILE_FLAGS "-fPIC")
ENDIF( CMAKE_SYSTEM_PROCESSOR STREQUAL "x86_64" )

# add_library(Prt_test OBJECT ${PrtUserSrc})
add_library(Prt_shared SHARED $<TARGET_OBJECTS:Prt>)
add_library(Prt_static STATIC $<TARGET_OBJECTS:Prt>)
# target_compile_definitions( Prt_test PRIVATE -DPRT_TEST )

target_include_directories(Prt PUBLIC ${API_PATH})
target_include_directories(Prt PUBLIC ${Core_PATH})
target_include_directories(Prt PUBLIC ${User_PATH})
get_property(Prt_Published_Headers_PATHS TARGET Prt PROPERTY INTERFACE_INCLUDE_DIRECTORIES)
# need to manually set includes for static and shared libraries, from object library include path
target_include_directories(Prt_shared PUBLIC ${Prt_Published_Headers_PATHS})
target_include_directories(Prt_static PUBLIC ${Prt_Published_Headers_PATHS})
# target_include_directories(Prt_test PUBLIC ${Prt_Published_Headers_PATHS})

Publish_Library_Header(Prt_shared)
