


#ifndef PRTDISTHELPER_H
#define PRTDISTHELPER_H

#include<stdio.h>

//enum for the fields in cluster configuration file
typedef struct _ClusterConfig {
	char* MainExe;
	char* NodeManagerPort;
	char* CentralServerPort;
	char* ContainerPortStart;
	char* NetworkShare;
	char* LocalFolder;
	char* CentralServer;
	char* MainMachineNode;
	int TotalNodes;
	char** ClusterMachines;
} ClusterConfig;

extern ClusterConfig ClusterConfiguration;

#ifdef __cplusplus
extern "C"{

#endif
//Helper functions used for parsing information from the XML.
void PrtDistClusterConfigInitialize();

#ifdef __cplusplus
}
#endif

#endif


