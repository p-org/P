#include"PrtDistConfigParser.h"

string PrtDistConfigGetNetworkShare(string configFilePath) {
	int i = 0;
	char DM[200];
	XMLNODE** listofNodes;
	XMLNODE* currNode;
	string DeploymentFolder = "";
	strcpy_s(DM, 200, "NetworkShare");
	listofNodes = XMLDOMParseNodes(configFilePath.c_str());
	currNode = listofNodes[i];
	while (currNode != NULL)
	{
		if (strcmp(currNode->NodeName, DM) == 0)
		{
			DeploymentFolder = currNode->NodeValue;
		}
		currNode = listofNodes[i];
		i++;
	}
	return DeploymentFolder;
}

void PrtDistConfigGetJobNameAndJobFolder(string configFilePath, string* jobName, string* jobFolder)
{
	ifstream read;
	read.open("job.txt");
	read >> *jobName;
	read >> *jobFolder;
}

int PrtDistConfigGetCentralServerNode(string configFilePath)
{
	int i = 0;
	char DM[200];
	XMLNODE** listofNodes;
	XMLNODE* currNode;
	string centralserver = "";
	strcpy_s(DM, 200, "CentralServer");
	listofNodes = XMLDOMParseNodes(configFilePath.c_str());
	currNode = listofNodes[i];
	while (currNode != NULL)
	{
		if (strcmp(currNode->NodeName, DM) == 0)
		{
			centralserver = currNode->NodeValue;
		}
		currNode = listofNodes[i];
		i++;
	}
	int cs = atoi(centralserver.c_str());
	return cs;
}

int PrtDistConfigGetTotalNodes(string configFilePath)
{
	int i = 0;
	char DM[200];
	XMLNODE** listofNodes;
	XMLNODE* currNode;
	string totalNodes = "";
	strcpy_s(DM, 200, "NNodes");
	listofNodes = XMLDOMParseNodes(configFilePath.c_str());
	currNode = listofNodes[i];
	while (currNode != NULL)
	{
		if (strcmp(currNode->NodeName, DM) == 0)
		{
			totalNodes = currNode->NodeValue;
		}
		currNode = listofNodes[i];
		i++;
	}
	int cs = atoi(totalNodes.c_str());
	return cs;
}

string PrtDistConfigGetLocalJobFolder(string configFilePath)
{
	int i = 0;
	char DM[200];
	XMLNODE** listofNodes;
	XMLNODE* currNode;
	string localFolder = "";
	strcpy_s(DM, 200, "localFolder");
	listofNodes = XMLDOMParseNodes(configFilePath.c_str());
	currNode = listofNodes[i];
	while (currNode != NULL)
	{
		if (strcmp(currNode->NodeName, DM) == 0)
		{
			localFolder = currNode->NodeValue;
		}
		currNode = listofNodes[i];
		i++;
	}
	
	return localFolder;
}



