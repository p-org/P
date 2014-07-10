#include<string>
#include<iostream>
#include <windows.h>
#include <thread>
#include "../Commons/PrtDGlobals.h"
#include"../Utilities/ParsingXML/ParsingXML.h"
#include"../PrtDistLogger/PrtDistLogger.h"
#include<fstream>
#include"../PrtDServiceIDL/PrtDService_h.h"
#include"../PrtDServiceIDL/PrtDService_s.c"
using namespace std;


//functions
void PrtDGetJobNameAndJobFolder(string* jobName, string* jobFolder);
void PrtDGetJobFilesLocally(string jobName, string jobFolder);
string PrtDGetDeploymentFolder();
string PrtDNextNodeManagerPort();

//rpc functions
void PrtDCreatePServiceRPCServer();
