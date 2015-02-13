#include "PrtDistHelperFuncs.h"

boolean _ROBOCOPY(string source, string dest)
{
	string copycommand = "robocopy " + source + " " + dest + " > " + "ROBOCOPY_PSERVICE_LOG.txt";
	if (system(copycommand.c_str()) == -1)
	{
		return false;
	}
	else
		return true;
}

char* _CONCAT(char* string1, char* string2)
{
	char ret[100];
	strcat_s(ret, string1);
	strcat_s(ret, string2);
	return ret;
}