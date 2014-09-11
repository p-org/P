#include "PrtDDeployer.h"

/* GLobal Variables */
string configurationFile = "../PrtDistManagement/Config/PrtDMConfiguration.xml";
string allBinaries = ".\\";
string localDeploymentFolder = "..\\DeploymentFolder\\";
string pThreadsFolder = "..\\..\\..\\Resources\\PThreads\\";
string vsdllsFolder = "..\\..\\..\\Resources\\VS2013\\";

char* PrtDGetPathToPHome(char* nodeAddress)
{
	char* path = (char*)PrtMalloc(sizeof(char) * 1000);
	strcpy_s(path, 1000, "");
	strcat_s(path, 1000, "\\\\");
	strcat_s(path, 1000, nodeAddress);
	strcat_s(path, 1000, "\\Plang_Shared\\");
	return path;
}

string PrtDDeployPProgram()
{
	string remoteDeploymentFolder = PrtDGetDeploymentFolder();
	string copycommand;
	//create the folder to be deployed in 
	DWORD ftyp = GetFileAttributesA(localDeploymentFolder.c_str());
	if (ftyp == INVALID_FILE_ATTRIBUTES)
		CreateDirectory(localDeploymentFolder.c_str(), NULL);


	//copy the configuration file
	string configFile = "PrtDMConfiguration.xml";
	string newFilePath = localDeploymentFolder + configFile;
	CopyFile(configurationFile.c_str(), newFilePath.c_str(), FALSE);
	//copy all resources into the deployment package
	copycommand = "robocopy " + pThreadsFolder + " " + localDeploymentFolder + " > " + localDeploymentFolder + "ROBOCOPY_LOG.txt";
	if (system(copycommand.c_str()) == -1)
	{
		cerr << "Failed to Copy Phreads in " << localDeploymentFolder << endl;
		exit(-1);
	}

	copycommand = "robocopy " + vsdllsFolder + " " + localDeploymentFolder + " > " + localDeploymentFolder + "ROBOCOPY_LOG.txt";
	if (system(copycommand.c_str()) == -1)
	{
		cerr << "Failed to Copy VS2013 in " << localDeploymentFolder << endl;
		exit(-1);
	}

	
	copycommand = "robocopy " + allBinaries + " " + localDeploymentFolder + " >> " + localDeploymentFolder + "ROBOCOPY_LOG.txt";
	if (system(copycommand.c_str()) == -1)
	{
		cerr << "Failed to Copy All Binaries in " << localDeploymentFolder << endl;
		exit(-1);
	}


	SYSTEMTIME time;
	GetLocalTime(&time);
	string jobName = to_string(time.wMonth) + "-" + to_string(time.wDay) + "-" + to_string(time.wYear) + "--" + to_string(time.wHour) + "-" + to_string(time.wMinute) + "-" + to_string(time.wSecond);

	string jobFolder = (remoteDeploymentFolder + jobName);
	//dump the jobname and folder in job.txt
	ofstream tempout;
	tempout.open(localDeploymentFolder + "job.txt");
	tempout << jobName << endl;
	tempout << jobFolder << endl;
	tempout.close();

	//copy all files

	CreateDirectory(jobFolder.c_str(), NULL);
	copycommand = "robocopy " + localDeploymentFolder + " " + jobFolder + " >> " + localDeploymentFolder + "ROBOCOPY_LOG.txt";
	if (system(copycommand.c_str()) == -1)
	{
		cerr << "Failed to Copy " << localDeploymentFolder << " to the destination folder " << jobFolder << endl;
		exit(-1);
	}
	return jobFolder;
}

string PrtDGetDeploymentFolder() {
	int i = 0;
	char DM[200];
	XMLNODE** listofNodes;
	XMLNODE* currNode;
	string DeploymentFolder;
	strcpy_s(DM, 200, "DeploymentFolder");
	listofNodes = XMLDOMParsingNodes(configurationFile.c_str());
	currNode = listofNodes[i];
	while (currNode != NULL)
	{
		if (strcmp(currNode->NodeName, DM) == 0)
		{
			DeploymentFolder = currNode->NodeValue;
		}
		currNode = listofNodes[i];
		i++;
	}
	PrtDLog(PRTD_DEPLOYER, (char*)("Deployment Folder = " + DeploymentFolder).c_str());
	return DeploymentFolder;
}

int main(int argc, char * argv[])
{
	char* log = (char*)PrtCalloc(100, sizeof(char));
	//create the Log File
	PrtDCreateLogFile(PRTD_DEPLOYER);
	sprintf_s(log, 100,"Starting the Deployment Operation for P Program \n");
	PrtDLog(PRTD_DEPLOYER, log);
	string jobFolder = PrtDDeployPProgram();
	cout << "Deployed the P program at :" << endl << jobFolder << endl;
	PrtDCloseLogFile();
	cout << "Press Key to Continue ....";
	getchar();

}