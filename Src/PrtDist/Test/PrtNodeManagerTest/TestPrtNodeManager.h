#pragma once
#include<iostream>
#include<stdlib.h>
#include "NodeManager_h.h"
#include "NodeManager_c.c"
#include "PrtDist.h"

using namespace std;


struct ClusterConfig ClusterConfiguration;

void Test_PServicePing();
void Test_ServiceCreateNode();