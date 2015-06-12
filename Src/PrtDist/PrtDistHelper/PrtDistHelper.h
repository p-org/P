#pragma once
#include<string>
#include<iostream>
#include <windows.h>
#include <thread>
#include<fstream>

using namespace std;

#pragma once


typedef	struct _XMLNODE {
	char NodeType[100];
	char NodeName[100];
	char NodeValue[100];
	char NodeParent[100];
} XMLNODE;

XMLNODE** XMLDOMParseNodes(const char*);

//Helper functions used across PrtDistManager projects.
boolean _ROBOCOPY(string source, string dest);
void _CONCAT(char* dest, char* string1, char* string2);

//Helper functions used for parsing information from the XML.

//get the network share path from which to fetch the binary files.
string PrtDistConfigGetNetworkShare(string configFilePath);

//get the job name and job folder from which to fetch the binaries on the network share
void PrtDistConfigGetJobNameAndJobFolder(string configFilePath, string* jobName, string* jobFolder);

//copies all the files locally from the network share.
void PrtDistConfigGetJobFilesLocally(string configFilePath, string jobName, string jobFolder);

//get the central server node name/ip from the config file.
int PrtDistConfigGetCentralServerNode(string configFilePath);

//get the total number nodes (0 -> localhost (debug mode))
int PrtDistConfigGetTotalNodes(string configFilePath);

//Get path to the local folder where the files are copied.
string PrtDistConfigGetLocalJobFolder(string configFilePath);


