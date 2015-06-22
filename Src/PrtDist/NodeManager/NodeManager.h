#include<string>
#include<iostream>
#include <windows.h>
#include <thread>
#include<fstream>
#include <mutex>
#include <chrono>
#include"..\Core\NodeManager_h.h"
#include"..\Core\NodeManager_s.c"
#include "..\Core\ConfigParser.h"
using namespace std;



/* GLobal Variables */
char* logFileName = "PRTDIST_NODEMANAGER.txt";
int CurrentNodeID = 1;
int myNodeId;
FILE* logFile;
std::mutex g_lock;

ClusterConfig ClusterConfiguration;

void PrtDistServiceCreateLogFile();
void PrtDistServiceCloseLogFile();
void PrtDistServiceLog(char* log);

string PrtDistServiceNextNodeManagerPort();
void PrtDistServiceCreateRPCServer();

//Helper functions used across PrtDistManager projects.
boolean _ROBOCOPY(string source, string dest);

//For concatenating two strings
void _CONCAT(char* dest, char* string1, char* string2);