#pragma once
#include<iostream>
#include<stdlib.h>
#include "NodeManager_c.c"
#include "NodeManager_h.h"
#include "PrtDist.h"

using namespace std;

struct ClusterConfig ClusterConfiguration;

void Test_CentralServerPing();
void Test_CentralServerGetNodeId();
