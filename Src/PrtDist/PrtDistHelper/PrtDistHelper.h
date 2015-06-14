


#ifndef PRTDISTHELPER_H
#define PRTDISTHELPER_H


#include<stdio.h>


typedef	struct _XMLNODE {
	char NodeType[100];
	char NodeName[100];
	char NodeValue[100];
	char NodeParent[100];
} XMLNODE;

XMLNODE** XMLDOMParseNodes(const char*);

//enum for the fields in cluster configuration file
typedef enum ClusterConfiguration
{
	MainExe = 0,
	NetworkShare,
	localFolder,
	CentralServer,
	TotalNodes
} PRT_ClusterConfiguration;



//Helper functions used across PrtDistManager projects.
void _ROBOCOPY(char* source, char* dest);

//For concatenating two strings
void _CONCAT(char* dest, char* string1, char* string2);

//Helper functions used for parsing information from the XML.
char* PrtDistClusterConfigGet(PRT_ClusterConfiguration field);

//get the job name and job folder from which to fetch the binaries on the network share
void PrtDistConfigGetJobNameAndJobFolder(char* jobName, char* jobFolder);

//copies all the files locally from the network share.
void PrtDistConfigGetJobFilesLocally(char* configFilePath, char* jobName, char* jobFolder);

#endif


