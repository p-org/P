#include<iostream>
#include<stdio.h>
#include<string.h>
#include<stdlib.h>
#include"PrtDGlobals.h"
#include"PrtDService_h.h"
#include"PrtDService_c.c"
#include"../Utilities/ParsingXML/ParsingXML.h"


using namespace std;

string configurationFile = "PrtDMConfiguration.xml";
char** Machines;
int GetNodes();

int __cdecl main()
{

	RPC_STATUS status;
	unsigned char* szStringBinding = NULL;
	int num = GetNodes();
	int i = 0;
	
	char buffPort[100];
	_itoa_s(PRTD_SERVICE_PORT, buffPort, 10);

	for (i = 0; i < num; i++)
	{
		// Creates a string binding handle.
		// This function is nothing more than a printf.
		// Connection is not done here.
		status = RpcStringBindingCompose(
			NULL, // UUID to bind to.
			(unsigned char*)("ncacn_ip_tcp"), // Use TCP/IP
			// protocol.
			(unsigned char*)(Machines[i]), // TCP/IP network
			// address to use.
			(unsigned char*)buffPort, // TCP/IP port to use.
			NULL, // Protocol dependent network options to use.
			&szStringBinding); // String binding output.

		if (status)
			exit(status);

		handle_t testme_handle = NULL;

		// Validates the format of the string binding handle and converts
		// it to a binding handle.
		// Connection is not done here either.
		status = RpcBindingFromStringBinding(
			szStringBinding, // The string binding to validate.
			&testme_handle); // Put the result in the implicit binding
		// handle defined in the IDL file.

		if (status)
			exit(status);
		boolean amIAlive = false;
		RpcTryExcept
		{
			c_PrtDPingService(testme_handle, &amIAlive);

		}
			RpcExcept(1)
		{
			unsigned long ulCode;
			ulCode = RpcExceptionCode();
			printf("Runtime reported exception 0x%lx = %ld\n", ulCode, ulCode);
		}
		RpcEndExcept
		if (amIAlive)
			cout << Machines[i] << " is Alive" << endl;
	}

	printf("Press any Key to Continue\n");
	getchar();


}


void* __RPC_API
MIDL_user_allocate(size_t size)
{
	unsigned char* ptr;
	ptr = (unsigned char*)malloc(size);
	return (void*)ptr;
}

void __RPC_API
MIDL_user_free(void* object)

{
	free(object);
}

int GetNodes() {
	int i = 0, j = 0;
	char DM[200];
	XMLNODE** listofNodes;
	XMLNODE* currNode;
	string DeploymentFolder;
	int numberOfNode = 0;
	//get number of nodes
	strcpy_s(DM, 200, "NNodes");
	listofNodes = XMLDOMParsingNodes(configurationFile.c_str());
	currNode = listofNodes[0];
	while (currNode != NULL)
	{
		if (strcmp(currNode->NodeName, DM) == 0)
		{
			 numberOfNode = atoi(currNode->NodeValue);
			 break;
		}
		i++;
		currNode = listofNodes[i];
	}

	i = 0;
	j = 0;
	Machines = (char**)malloc(sizeof(char*)*numberOfNode);
	strcpy_s(DM, 200, "Node");
	listofNodes = XMLDOMParsingNodes(configurationFile.c_str());
	currNode = listofNodes[0];
	while (currNode != NULL)
	{
		
		if (strcmp(currNode->NodeName, DM) == 0)
		{
			Machines[j] = currNode->NodeValue;
			printf("Machine %d : %s\n", j, Machines[j]);
			j++;
		}
		
		i++;
		currNode = listofNodes[i];
	}

	return numberOfNode;

}