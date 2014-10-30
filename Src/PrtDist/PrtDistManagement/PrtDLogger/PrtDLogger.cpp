#include "PrtDLogger.h"
#include<iostream>
using namespace std;

ofstream PrtDLogStream;
void PrtDCreateLogFile(PRTD_COMPONENT pComponent)
{

	switch (pComponent)
	{
	case PRTD_DEPLOYER:
		PrtDLogStream.open("PRTD_DEPLOYER_LOG.txt");
		break;
	case PRTD_SERVICE:
		PrtDLogStream.open("PRTD_SERVICE_LOG.txt");
		break;
	case PRTD_NODEMANAGER:
		PrtDLogStream.open("PRTD_NODEMANAGER_LOG.txt");
		break;
	case PRTD_CENTRALSERVER:
		PrtDLogStream.open("PRTD_CENTRALSERVER_LOG.txt");
		break;
	case PRTD_MAINMACHINE:
		PrtDLogStream.open("PRTD_MAINMACHINE_LOG.txt");
		break;
	default:
		cerr << "Log File Cannot be created" << endl;
		exit(1);
	}

}

void PrtDCloseLogFile()
{
	PrtDLogStream << "<EndLog> " << "GOOD BYE" << endl;
	PrtDLogStream.close();
}

void PrtDLog(PRTD_COMPONENT op, char* log)
{
	switch (op)
	{
	case PRTD_DEPLOYER:
		PrtDLogStream << "<PRTD_DEPLOYER> " << log << endl;
		break;
	case PRTD_SERVICE:
		PrtDLogStream << "<PRTD_SERVICE> " << log << endl;
		break;
	case PRTD_NODEMANAGER:
		PrtDLogStream << "<PRTD_NODEMANAGER> " << log << endl;
		break;
	case PRTD_CENTRALSERVER:
		PrtDLogStream << "<PRTD_CENTRALSERVER> " << log << endl;
		break;
	case PRTD_MAINMACHINE:
		PrtDLogStream << "<PRTD_MAINMACHINE> " << log << endl;
		break;
	default:
		break;
	}
	PrtDLogStream.flush();
	return;
}