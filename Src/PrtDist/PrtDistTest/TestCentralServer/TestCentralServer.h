#pragma once
#include<iostream>
#include<stdlib.h>
#include "../../PrtDistManager/PrtDistCentralServerIDL/PrtDistCentralServer_h.h"
#include "../../PrtDistManager/PrtDistCentralServerIDL/PrtDistCentralServer_c.c"
#include "../../PrtDistUtilities/PrtDistHelper/PrtDistConfigParser.h"
#include "../../PrtDistUtilities/PrtDistHelper/PrtDistHelperFuncs.h"
#include "PrtDistGlobals.h"

using namespace std;


void Test_CentralServerPing();
void Test_CentralServerGetNodeId();
