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

void _CONCAT(char* dest, char* string1, char* string2)
{
	strcat(dest, string1);
	strcat(dest, string2);
}