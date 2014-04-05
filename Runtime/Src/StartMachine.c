#ifdef DISTRIBUTED_RUNTIME
#include <stdlib.h>
#include <windows.h>
#include "SmfProtectedTypes.h"
#include "SmfPrivate.h"
#include "StartMachine.h"

PSMF_DRIVERDECL DriverDecl;
SMF_SMCONTEXT_REMOTE Manager;
HANDLE ChildUpEvent;
SMF_MACHINE_HANDLE This;

extern void NewMachineId(long *addressSize, unsigned char **address, long *portSize, unsigned char **port);
extern VOID AddToRemoteIdTable(PSMF_SMCONTEXT_REMOTE remoteContext);
extern VOID CloseRemoteHandles();

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

	status = RpcStringFree(&pszStringBinding);  
    if (status) exit(status);

	return ifHandle;
}

int StartMachine(long InstanceOf, int argc, wchar_t* argv[])
{
	RPC_WSTR myAddress = NULL;
	RPC_WSTR myPort = NULL;
	char *execString;
	long execStringSize;
	long addressSize;
	unsigned char **address;
	long portSize;
	unsigned char **port;
	RPC_STATUS status;

	Manager.Address = (RPC_WSTR)argv[0];
	Manager.Port = (RPC_WSTR)argv[1];
	Manager.RPCHandle = OpenHandle(Manager.Address, Manager.Port);

	myAddress = (RPC_WSTR)argv[2];
	myPort = (RPC_WSTR)argv[3];

	address = (unsigned char**) midl_user_allocate(sizeof(unsigned char*));
	*address = NULL;
	port = (unsigned char**) midl_user_allocate(sizeof(unsigned char*));
	*port = NULL;
    NewMachineId(&addressSize, address, &portSize, port);
		execStringSize = 
				strlen("StartOrNew.exe") + 1 + 
				strlen("New") + 1 + 
				strlen((char *)Manager.Address) + 1 + 
				strlen((char *)Manager.Port) + 1 + 
				strlen((char *)myAddress) + 1 +
				strlen((char *)myPort) + 1 +
				addressSize + 1 +
				portSize + 1;
	execString = (char *) malloc(execStringSize);
	sprintf_s(execString, execStringSize, "StartOrNew.exe New %s %s %s %s %s %s", address, Manager.Address, Manager.Port, *address, *port, myAddress, myPort);
	system(execString);
	free(execString);
	WaitForSingleObject(ChildUpEvent, INFINITE);
	Client_WrapSmfNew(OpenHandle((RPC_WSTR)*address, (RPC_WSTR)*port), InstanceOf);
	midl_user_free(address);
	midl_user_free(port);

    status = RpcMgmtStopServerListening(NULL);
    if (status) exit(status);
 
    status = RpcServerUnregisterIf(NULL, NULL, FALSE);
    if (status) exit(status);
 
	status = RpcBindingFree(&(Manager.RPCHandle));
	if (status) exit(status);

	CloseHandle(ChildUpEvent);
	return 0;
}

int NewMachine(PSMF_DRIVERDECL decl, int argc, wchar_t* argv[])
{
	RPC_STATUS status;
	unsigned long ulCode;
	RPC_WSTR myAddress = NULL;
	RPC_WSTR myPort = NULL;
	RPC_WSTR parentAddress = NULL;
	RPC_WSTR parentPort = NULL;

	DriverDecl = decl;
	
	Manager.Address = (RPC_WSTR)argv[0];
	Manager.Port = (RPC_WSTR)argv[1];
	Manager.RPCHandle = OpenHandle(Manager.Address, Manager.Port);
	
	myAddress = (RPC_WSTR)argv[2];
	myPort = (RPC_WSTR)argv[3];

	parentAddress = (RPC_WSTR)argv[4];
	parentPort = (RPC_WSTR)argv[5];
	
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
	
	RpcTryExcept  
	{
		Client_ChildUp(OpenHandle(parentAddress, parentPort));
	}
	RpcExcept(1) 
	{
		ulCode = RpcExceptionCode();
		printf("Runtime reported exception 0x%lx = %ld\n", ulCode, ulCode);
	}
	RpcEndExcept

	return 0;
}

SMF_MACHINE_HANDLE 
SmfNewRemote(
__in PSMF_DRIVERDECL			PDriverDecl, 
__inout PSMF_SMCONTEXT			Context, 
__in SMF_MACHINEDECL_INDEX		InstanceOf,
__in PSMF_PACKED_VALUE			Arg)
{
    PSMF_SMCONTEXT_REMOTE remoteContext;
	char *execString;
	long execStringSize;
	long addressSize;
	unsigned char **address;
	long portSize;
	unsigned char **port;
	address = (unsigned char**) midl_user_allocate(sizeof(unsigned char*));
	*address = NULL;
	port = (unsigned char**) midl_user_allocate(sizeof(unsigned char*));
	*port = NULL;
    NewMachineId(&addressSize, address, &portSize, port);
	execStringSize = 
				strlen("StartOrNew.exe") + 1 + 
				strlen("New") + 1 + 
				strlen((char *)Manager.Address) + 1 + 
				strlen((char *)Manager.Port) + 1 + 
				strlen((char *)Context->Address) + 1 +
				strlen((char *)Context->Port) + 1 +
				addressSize + 1 +
				portSize + 1;
	execString = (char *) malloc(execStringSize);
	sprintf_s(execString, execStringSize, "StartOrNew.exe New %s %s %s %s %s %s", address, Manager.Address, Manager.Port, *address, *port, Context->Address, Context->Port);
	system(execString);
	free(execString);
	WaitForSingleObject(ChildUpEvent, INFINITE);
	remoteContext = (PSMF_SMCONTEXT_REMOTE) malloc(sizeof(SMF_SMCONTEXT_REMOTE));
	remoteContext->Address = (RPC_WSTR)*address;
	remoteContext->Port = (RPC_WSTR)*port;
	remoteContext->RPCHandle = OpenHandle(remoteContext->Address, remoteContext->Port);
	AddToRemoteIdTable(remoteContext);
	midl_user_free(address);
	midl_user_free(port);
	Client_WrapSmfNew(remoteContext->RPCHandle, InstanceOf);
	return SmfGetStateMachineHandleRemote(remoteContext);
}

VOID 
SmfEnqueueEventRemote(
__in SMF_MACHINE_HANDLE			Machine, 
__in SMF_EVENTDECL_INDEX		EventIndex, 
__in PSMF_PACKED_VALUE			Arg,
__in BOOLEAN					UseWorkerItem
)
{
	PSMF_SMCONTEXT_REMOTE remoteContext;
	if (Machine == This) {
		SmfEnqueueEventInternal(Machine, EventIndex, Arg, UseWorkerItem);
		return;
	}
	remoteContext = SmfGetStateMachinePointerRemote(Machine);
	if (remoteContext->RPCHandle == NULL) {
		remoteContext->RPCHandle = OpenHandle(remoteContext->Address, remoteContext->Port);
	}
	Client_WrapSmfEnqueueEvent(remoteContext->RPCHandle, EventIndex);
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
    if (status) exit(status);
 
    status = RpcServerUnregisterIf(NULL, NULL, FALSE);
    if (status) exit(status);
 
	status = RpcBindingFree(&(Manager.RPCHandle));
	if (status) exit(status);

	CloseHandle(ChildUpEvent);

	CloseRemoteHandles();
	SmfRemoveMachine(SmfGetStateMachinePointer(This));
} //end Shutdown
#endif