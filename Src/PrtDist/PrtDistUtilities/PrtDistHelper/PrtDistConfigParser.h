#pragma once
#include<string>
#include<iostream>
#include <windows.h>
#include <thread>
#include<fstream>
#include "../../PrtDistUtilities/XMLParser/XMLParser.h"

using namespace std;

string PrtDistConfigGetNetworkShare(string configFilePath);
void PrtDistConfigGetJobNameAndJobFolder(string configFilePath, string* jobName, string* jobFolder);
void PrtDistConfigGetJobFilesLocally(string configFilePath, string jobName, string jobFolder);
int PrtDistConfigGetCentralServerNode(string configFilePath);
int PrtDistConfigGetTotalNodes(string configFilePath);
string PrtDistConfigGetLocalJobFolder(string configFilePath);


