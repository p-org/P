#include<string>
#include<iostream>
#include <windows.h>
#include <thread>
#include "PrtDistGlobals.h"
#include "../../PrtDistUtilities/XMLParser/XMLParser.h"
#include "../../PrtDistUtilities/PrtDistHelper/PrtDistHelperFuncs.h"
#include "../../PrtDistUtilities/PrtDistHelper/PrtDistConfigParser.h"
#include<fstream>
#include"../PrtDistServiceIDL/PrtDistService_h.h"
#include"../PrtDistServiceIDL/PrtDistService_s.c"
using namespace std;

#define _CRT_SECURE_NO_WARNINGS
void PrtDistServiceCreateLogFile();
void PrtDistServiceCloseLogFile();
void PrtDistServiceLog(char* log);

string PrtDistServiceNextNodeManagerPort();
void PrtDistServiceCreateRPCServer();
