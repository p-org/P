#ifndef LINUX_EXT_COMPAT
#define LINUX_EXT_COMPAT

#include <stdio.h>
#include <string.h>
#include <stdarg.h>

/* 	
	This file is only a temporary solution to getting Linux Prt to Compile.
	The implementations are NOT COMPLETE !!!
	TODO: move to a safer cleaner solution
*/

#define fprintf_s(stream, format, args...) fprintf(stream, format, ##args)

#define printf_s(format, args...) printf(format, ##args)

#define strcpy_s(d, n, s) snprintf(d, n, "%s", s)

#define sprintf_s(buffer, size, format, args...) snprintf(buffer, size, format, ##args )

#endif