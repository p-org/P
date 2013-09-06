/*********************************************************************************

Copyright (c) Microsoft Corporation

File Name:

    SmfDepends.h

Abstract:
    This header file contains list of all the header files on which P Runtime Depends.

Environment:

    Kernel mode only.		

***********************************************************************************/

#pragma once 
#ifdef KERNEL_MODE


#include <wdm.h>
#include <initguid.h>
#include <wdf.h>
#include <wdmguid.h>
#include <ntstrsafe.h>
#include <ntintsafe.h>
#include <driverspecs.h>
#include <devguid.h>
#include <ntddstor.h>

#else

#include <Windows.h>
#include <ntintsafe.h>
#include <stdio.h>
#include <stdlib.h>
#include <assert.h>
#endif