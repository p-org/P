#include<string>
#include<iostream>
#include <windows.h>
#include <thread>
#include<fstream>
#include <mutex>
#include <chrono>
#include"NodeManager_h.h"
#include"NodeManager_s.c"
#include "Helper.h"
#include "..\..\Core\CommonFiles\PrtDistClusterInformation.h"

using namespace std;

void PrtDistServiceCreateLogFile();
void PrtDistServiceCloseLogFile();
void PrtDistServiceLog(char* log);

string PrtDistServiceNextNodeManagerPort();
void PrtDistServiceCreateRPCServer();
