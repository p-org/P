#pragma once
#include "PrtWinUser.h"
#include "PrtExecution.h"

handle_t
PrtDistCreateRPCClient(
PRT_VALUE* target
);

DWORD WINAPI PrtDistCreateRPCServerForEnqueueAndWait
(LPVOID portNumber);