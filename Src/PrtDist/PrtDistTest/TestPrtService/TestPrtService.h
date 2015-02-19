#pragma once
#include<iostream>
#include<stdlib.h>
#include "../../PrtDistManager/PrtDistServiceIDL/PrtDistService_h.h"
#include "../../PrtDistManager/PrtDistServiceIDL/PrtDistService_c.c"
#include "../../PrtDistUtilities/PrtDistHelper/PrtDistConfigParser.h"
#include "../../PrtDistUtilities/PrtDistHelper/PrtDistHelperFuncs.h"
#include "PrtDistGlobals.h"

using namespace std;


void Test_PServicePing();
void Test_ServiceCreateNode();