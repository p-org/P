#pragma once
#include<stdio.h>
#include<Windows.h>
#include<iostream>
#include<string>
#include <ctime>
#include"../Utilities/ParsingXML/ParsingXML.h"
#include<fstream>
#include"../PrtDLogger/PrtDLogger.h"
extern "C"
{
	#include "PrtHeaders.h"
}
using namespace std;

string PrtDGetDeploymentFolder();
string PrtDDeployPProgram();