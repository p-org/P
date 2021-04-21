cmake_minimum_required (VERSION 3.1)
project (P)
set(projectName ForeignTypes)
find_package (Threads)
add_definitions(-DPRT_PLAT_LINUXUSER)
include_directories(${CMAKE_SOURCE_DIR}/../../../../P/Bld/Drops/Prt/include)
add_executable(foreign_types
	ForeignStringType.c
	ForeignStringType.h
	ForeignTypesImpl.c
	Main.c)

target_link_libraries(foreign_types ${CMAKE_SOURCE_DIR}/../../../Bld/Drops/Prt/lib/libPrt_static.a ${CMAKE_SOURCE_DIR}/../../../Ext/libhandler/out/gcc-amd64-linux-gnu/debug/libhandler.a ${CMAKE_THREAD_LIBS_INIT})
