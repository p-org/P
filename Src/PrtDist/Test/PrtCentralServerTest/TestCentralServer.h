#pragma once
#include<iostream>
#include<stdlib.h>
#include "../../ClusterManagement/NodeManager/NodeManagerIDL/NodeManager_c.c"
#include "../../ClusterManagement/NodeManager/NodeManagerIDL/NodeManager_h.h"
#include "../../ClusterManagement/Helper/Helper.h"
#include "../../Core/CommonFiles/PrtDistClusterInformation.h"
using namespace std;


void Test_CentralServerPing();
void Test_CentralServerGetNodeId();
