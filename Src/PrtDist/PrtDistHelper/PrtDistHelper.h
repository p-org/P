


#ifndef PRTDISTHELPER_H
#define PRTDISTHELPER_H

#include<string>
#include<stdio.h>
#include<fstream>
#import <msxml3.dll>

using namespace std;

typedef	struct _XMLNODE {
	char NodeType[100];
	char NodeName[100];
	char NodeValue[100];
	char NodeParent[100];
} XMLNODE;

XMLNODE** XMLDOMParseNodes(const char*);

//enum for the fields in cluster configuration file
enum ClusterConfiguration
{
	MainExe = 0,
	NetworkShare,
	localFolder,
	CentralServer,
	TotalNodes
};
#ifdef __cplusplus
extern "C"{
#endif

//Helper functions used across PrtDistManager projects.
boolean _ROBOCOPY(string source, string dest);

//For concatenating two strings
void _CONCAT(char* dest, char* string1, char* string2);

//Helper functions used for parsing information from the XML.
char* PrtDistClusterConfigGet(ClusterConfiguration field);

#ifdef __cplusplus
}
#endif

#endif


