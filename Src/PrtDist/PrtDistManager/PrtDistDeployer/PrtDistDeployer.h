#pragma once
#include<stdio.h>
#include<Windows.h>
#include<iostream>
#include<string>
#include <ctime>
#include<fstream>
#include"PrtDist.h"
#include "../../PrtDistUtilities/XMLParser/XMLParser.h"
#include"../../PrtDistUtilities/PrtDistHelper/PrtDistConfigParser.h"

using namespace std;

//Logger Functions
void PrtDistDeployerCreateLogFile();
void PrtDistDeployerCloseLogFile();
void PrtDistDeployerLog(char* log);

//Deploy
string PrtDistDeployPProgram();