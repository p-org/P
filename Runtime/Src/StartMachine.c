#ifdef DISTRIBUTED_RUNTIME
#include <stdlib.h>
#include <windows.h>
#include "SmfProtectedTypes.h"
#include "StartMachine.h"

extern VOID 
SmfInitAttributes(
__inout PSMF_MACHINE_ATTRIBUTES Attributes, 
__in PSMF_DRIVERDECL			Driver,
__in SMF_MACHINEDECL_INDEX		InstanceOf,
__in PSMF_PACKED_VALUE			Arg,
__in PVOID						ConstructorParam
);

PSMF_DRIVERDECL DriverDecl;

extern NTSTATUS 
SmfCreate(
__in PSMF_MACHINE_ATTRIBUTES	InitAttributes, 
__out PSMF_MACHINE_HANDLE		PSmHandle
);

extern VOID 
SmfEnqueueEvent(
__in SMF_MACHINE_HANDLE			Machine, 
__in SMF_EVENTDECL_INDEX		EventIndex, 
__in PSMF_PACKED_VALUE			Arg,
__in BOOLEAN					UseWorkItem
); 

SMF_SMCONTEXT_REMOTE Manager;
HANDLE ChildUpEvent;
SMF_MACHINE_HANDLE This;

handle_t OpenHandle(RPC_WSTR address, RPC_WSTR port)
{
    RPC_STATUS status;
	RPC_WSTR pszStringBinding    = NULL;
    handle_t ifHandle;

    status = RpcStringBindingCompose(NULL,
									 (RPC_WSTR)L"ncacn_ip_tcp",
                                     address,
                                     port,
                                     NULL,
                                     &pszStringBinding);

    if (status) exit(status);

    status = RpcBindingFromStringBinding(pszStringBinding, &ifHandle);
 
    if (status) exit(status);
	return ifHandle;
}

int Start(PSMF_DRIVERDECL decl, int argc, wchar_t* argv[])
{
	RPC_STATUS status;
	unsigned long ulCode;
	handle_t parentHandle;
	RPC_WSTR parentAddress, parentPort, myAddress, myPort;

	Manager.Address = (RPC_WSTR)argv[1];
	Manager.Port = (RPC_WSTR)argv[2];
	parentAddress = (RPC_WSTR)argv[3];
	parentPort = (RPC_WSTR)argv[4];
	myAddress = (RPC_WSTR)argv[5];
	myPort = (RPC_WSTR)argv[6];

	DriverDecl = decl;

	Manager.RPCHandle = OpenHandle(Manager.Address, Manager.Port);

	ChildUpEvent = CreateEvent(NULL, FALSE, FALSE, NULL);

    status = RpcServerUseProtseqEp((RPC_WSTR)L"ncacn_ip_tcp",
                                   RPC_C_PROTSEQ_MAX_REQS_DEFAULT,
								   myPort,
                                   NULL); 
 
    if (status) exit(status);
 
    status = RpcServerRegisterIfEx(StartMachine_v1_0_s_ifspec, NULL, NULL, RPC_IF_ALLOW_CALLBACKS_WITH_NO_AUTH, RPC_C_LISTEN_MAX_CALLS_DEFAULT, NULL);

    if (status) exit(status);
 
    status = RpcServerListen(1,
                             RPC_C_LISTEN_MAX_CALLS_DEFAULT,
                             FALSE);
 
    if (status) exit(status);

	parentHandle = OpenHandle(parentAddress, parentPort);
	RpcTryExcept  
    {
        Client_ChildUp(parentHandle);
    }
    RpcExcept(1) 
    {
        ulCode = RpcExceptionCode();
        printf("Runtime reported exception 0x%lx = %ld\n", ulCode, ulCode);
    }
    RpcEndExcept

	return 0;
}

void WrapSmfNew(handle_t startMachine_IfHandle, long InstanceOf)
{
	PSMF_MACHINE_ATTRIBUTES mAttributes;
    SMF_MACHINE_HANDLE smHandle;
	mAttributes = (PSMF_MACHINE_ATTRIBUTES) malloc(sizeof(SMF_MACHINE_ATTRIBUTES));
	if (mAttributes == NULL) {
		return; // add some error code
	}
	SmfInitAttributes(mAttributes, DriverDecl, (SMF_MACHINEDECL_INDEX)InstanceOf, NULL, NULL);
    SmfCreate(mAttributes, &smHandle);
    free(mAttributes);
    This = smHandle;
}

void WrapSmfEnqueueEvent(handle_t startMachine_IfHandle, long EventIndex)
{
	SmfEnqueueEvent(This, (SMF_EVENTDECL_INDEX)EventIndex, SmfNull, FALSE);
}

void ChildUp(handle_t startMachine_IfHandle)
{
	SetEvent(ChildUpEvent);
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

void MachineShutdown(handle_t startMachine_IfHandle)
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

	//Close all RPC server handles, ChildUpEvent handle, free all allocated memory
} //end Shutdown
#endif