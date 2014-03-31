// Manager.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include "stdlib.h"
#include <string>
#include <windows.h>
#include "Manager.h"

using namespace std;
int nextPortNum;

int _tmain(int argc, _TCHAR* argv[])
{
	RPC_STATUS status;

    status = RpcServerUseProtseqEp((RPC_WSTR)"ncacn_ip_tcp",
                                   RPC_C_PROTSEQ_MAX_REQS_DEFAULT,
                                   (RPC_WSTR)"9000",
                                   NULL); 
 
    if (status) exit(status);
 
    status = RpcServerRegisterIfEx(Manager_v1_0_s_ifspec, NULL, NULL, RPC_IF_ALLOW_CALLBACKS_WITH_NO_AUTH, RPC_C_LISTEN_MAX_CALLS_DEFAULT, NULL);

    if (status) exit(status);
 
    status = RpcServerListen(1,
                             RPC_C_LISTEN_MAX_CALLS_DEFAULT,
                             FALSE);
 
    if (status) exit(status);
	nextPortNum = 9001;

	return 0;
}

unsigned char* NewMachineId()
{
	string s = "localhost:" + nextPortNum;
	nextPortNum++;
	return (unsigned char*)s.c_str();
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

void Shutdown(void)
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
