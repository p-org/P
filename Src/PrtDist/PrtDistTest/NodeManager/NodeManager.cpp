#include<stdio.h>
#include<Windows.h>
#include<iostream>
#include<string>
#include <ctime>
#include<fstream>
using namespace std;

/* GLobal Variables */
string configurationFile = "PrtDMConfiguration.xml";
char* logFileName = "PRTDIST_NODEMANAGER.txt";
FILE* logFile;

///
///PrtDist Deployer Logging
///
void PrtNodeManagerCreateLogFile()
{
	fopen_s(&logFile, logFileName, "w+");
	fputs("Starting PrtNodeManager ..... \n", logFile);
	fflush(logFile);
}

void PrtNodeManagerCloseLogFile()
{
	fputs("Done with PrtNodeManager ...... \n", logFile);
	fflush(logFile);
	fclose(logFile);
}

void PrtNodeManagerLog(char* log)
{
	fputs(log, logFile);
	fputs("\n", logFile);
	fflush(logFile);
}

int main(int argc, char * argv[])
{
	char* log = (char*)calloc(100, sizeof(char));
	//create the Log File
	PrtNodeManagerCreateLogFile();
	PrtNodeManagerLog("Ok this is working");
	PrtNodeManagerCloseLogFile();

}

