#pragma once
#include<stdio.h>
#include<Windows.h>
#include<iostream>
#include<string>
#include <ctime>
#include<fstream>
#include"PrtDist.h"
#include "../../PrtDistUtilities/XMLParser/XMLParser.h"
using namespace std;

//Helper Functions
string PrtDistGetNetworkShare();
void PrtDistDeployerCreateLogFile();
void PrtDistDeployerCloseLogFile();
void PrtDistDeployerLog(char* log);

//Deploy
string PrtDistDeployPProgram();