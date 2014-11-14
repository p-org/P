#include<string>
#include<iostream>
#include <windows.h>
#include <thread>
#include "PrtDistGlobals.h"
#include "../../PrtDistUtilities/XMLParser/XMLParser.h"
#include<fstream>
#include"../PrtDistServiceIDL/PrtDistService_h.h"
#include"../PrtDistServiceIDL/PrtDistService_s.c"
using namespace std;

#define _CRT_SECURE_NO_WARNINGS
//logging functions
void PrtDistServiceCreateLogFile();
void PrtDistServiceCloseLogFile();
void PrtDistServiceLog(char* log);

//functions
string PrtDistServiceGetNetworkShare();
void PrtDistServiceGetJobNameAndJobFolder(string* jobName, string* jobFolder);
void PrtDistServiceGetJobFilesLocally(string jobName, string jobFolder);
string PrtDistServiceNextNodeManagerPort();

//rpc functions
void PrtDistServiceCreateRPCServer();
