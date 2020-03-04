cmake_minimum_required (VERSION 3.1)
project (P)
set(projectName ForeignTypes)
find_package (Threads)
add_definitions(-DPRT_PLAT_LINUXUSER)
include_directories(~/Documents/P/Bld/Drops/Prt/include)
add_executable(foreign_types
	ForeignStringType.c
	ForeignStringType.h
	ForeignTypesImpl.c
	Main.c
	${CMAKE_SOURCE_DIR}/../../../../P/Bld/Drops/Prt/include/ext_compat.h
	${CMAKE_SOURCE_DIR}/../../../../P/Bld/Drops/Prt/include/libhandler.h
	${CMAKE_SOURCE_DIR}/../../../../P/Bld/Drops/Prt/include/libhandler-internal.h
	${CMAKE_SOURCE_DIR}/../../../../P/Bld/Drops/Prt/include/Prt.h
	${CMAKE_SOURCE_DIR}/../../../../P/Bld/Drops/Prt/include/PrtConfig.h
	${CMAKE_SOURCE_DIR}/../../../../P/Bld/Drops/Prt/include/PrtExecution.h
	${CMAKE_SOURCE_DIR}/../../../../P/Bld/Drops/Prt/include/PrtLinuxUserConfig.h
	${CMAKE_SOURCE_DIR}/../../../../P/Bld/Drops/Prt/include/PrtProgram.h
	${CMAKE_SOURCE_DIR}/../../../../P/Bld/Drops/Prt/include/PrtTypes.h
	${CMAKE_SOURCE_DIR}/../../../../P/Bld/Drops/Prt/include/PrtValues.h
	${CMAKE_SOURCE_DIR}/../../../../P/Bld/Drops/Prt/include/sal.h)

target_link_libraries(foreign_types ${CMAKE_SOURCE_DIR}/../../../Bld/Drops/Prt/lib/libPrt_static.a ${CMAKE_SOURCE_DIR}/../../../Ext/libhandler/out/gcc-amd64-linux-gnu/debug/libhandler.a ${CMAKE_THREAD_LIBS_INIT})
