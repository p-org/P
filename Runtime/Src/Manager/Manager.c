// Manager.cpp : Defines the entry point for the console application.
//

#include <stdlib.h>
#include <stdio.h>
#include <math.h>
#include "Manager.h"
#include <windows.h>

int nextPortNum;

void main()
{
	RPC_STATUS status;

	status = RpcServerUseProtseqEp((RPC_WSTR)L"ncacn_ip_tcp",
                                   RPC_C_PROTSEQ_MAX_REQS_DEFAULT,
                                   (RPC_WSTR)L"9000",
                                   NULL); 
 
    if (status) exit(status);
 
    status = RpcServerRegisterIfEx(Manager_v1_0_s_ifspec, NULL, NULL, RPC_IF_ALLOW_CALLBACKS_WITH_NO_AUTH, RPC_C_LISTEN_MAX_CALLS_DEFAULT, NULL);

    if (status) exit(status);
 
	nextPortNum = 9001;

    status = RpcServerListen(1,
                             RPC_C_LISTEN_MAX_CALLS_DEFAULT,
                             FALSE);
 
    if (status) exit(status);
}

void NewMachineId( 
    long *addressSize,
    unsigned char **address,
    long *portSize,
    unsigned char **port)
{
    char *dummy = (char *)L"localhost";
	*addressSize = strlen(dummy) + 1;
	*address = (unsigned char*)midl_user_allocate(*addressSize);
	strncpy((char *)*address, dummy, *addressSize);
	*portSize = floor(log10((double)nextPortNum)) + 1;
	*port = (unsigned char *)midl_user_allocate(*portSize);
	_itoa(nextPortNum, (char *)*port, 10);
	nextPortNum++;
}

/******************************************************/
/*         MIDL allocate and free                     */
/******************************************************/
 
void __RPC_FAR * __RPC_USER midl_user_allocate(size_t len)
{
    return(malloc(len));
}
 
void __RPC_USER midl_user_free(void __RPC_FAR * ptr)
{
    free(ptr);
}

void ManagerShutdown(void)
{
    RPC_STATUS status;
 
    status = RpcMgmtStopServerListening(NULL);
 
    if (status) 
    {
       exit(status);
    }
 
    status = RpcServerUnregisterIf(NULL, NULL, FALSE);
 
    if (status) 
    {
       exit(status);
    }
} //end Shutdown
