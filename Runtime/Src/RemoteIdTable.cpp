#ifdef DISTRIBUTED_RUNTIME
#include <windows.h>
#include "SmfProtectedTypes.h"
#include "SmfPrivate.h"
#include <map>
using namespace std;

map<pair<RPC_WSTR, RPC_WSTR>, PSMF_SMCONTEXT_REMOTE> remoteIdTable;

extern "C" VOID
AddToRemoteIdTable(PSMF_SMCONTEXT_REMOTE remoteContext)
{
	remoteIdTable[make_pair(remoteContext->Address, remoteContext->Port)] = remoteContext;
}

extern "C" VOID
CloseRemoteHandles()
{
	RPC_STATUS status;
	map<pair<RPC_WSTR, RPC_WSTR>, PSMF_SMCONTEXT_REMOTE>::iterator iter;
	for (iter = remoteIdTable.begin(); iter != remoteIdTable.end(); iter++) {
		status = RpcBindingFree(&(iter->second->RPCHandle));
		if (status) exit(status);
		free(iter->second);
	}
}
#endif