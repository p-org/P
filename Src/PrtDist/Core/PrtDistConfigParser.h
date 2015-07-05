


#ifndef PRTDISTHELPER_H
#define PRTDISTHELPER_H

#include<stdio.h>

#ifdef __cplusplus
extern "C"{
#endif
	//enum for the fields in cluster configuration file
struct ClusterConfig {
	char* MainExe;
	char* NodeManagerPort;
	char* ContainerPortStart;
	char* NetworkShare;
	char* LocalFolder;
	char* CentralServer;
	char* MainMachineNode;
	int TotalNodes;
	char** ClusterMachines;
	char* configFileName;
};

extern struct ClusterConfig ClusterConfiguration;

//Helper functions used for parsing information from the XML.
void PrtDistClusterConfigInitialize(char* configurationFile);

#ifdef __cplusplus
}
#endif

#endif


