#include<string>
#include<iostream>
#include <windows.h>
#include <thread>
#include<fstream>
#include "..\CommonFiles\PrtDistMachines.h"
#include "..\CommonFiles\PrtDistPorts.h"
#include"PrtDistNodeManagerIDL\PrtDistNodeManager_h.h"
#include"PrtDistNodeManagerIDL\PrtDistNodeManager_s.c"
#include "..\..\PrtDistHelper\PrtDistHelper.h"
using namespace std;

#define _CRT_SECURE_NO_WARNINGS
void PrtDistServiceCreateLogFile();
void PrtDistServiceCloseLogFile();
void PrtDistServiceLog(char* log);

string PrtDistServiceNextNodeManagerPort();
void PrtDistServiceCreateRPCServer();
