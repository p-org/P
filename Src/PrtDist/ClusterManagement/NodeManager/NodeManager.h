#include<string>
#include<iostream>
#include <windows.h>
#include <thread>
#include<fstream>
#include <mutex>
#include <chrono>
#include"NodeManagerIDL\NodeManager_h.h"
#include"NodeManagerIDL\NodeManager_s.c"
#include "..\..\ClusterManagement\Helper\Helper.h"
#include "..\..\Core\CommonFiles\PrtDistClusterInformation.h"

using namespace std;

#define _CRT_SECURE_NO_WARNINGS
void PrtDistServiceCreateLogFile();
void PrtDistServiceCloseLogFile();
void PrtDistServiceLog(char* log);

string PrtDistServiceNextNodeManagerPort();
void PrtDistServiceCreateRPCServer();
