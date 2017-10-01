module DSClientAndList = 
(compose { SMRClientInterface -> DSClientMachine }, { SMRReplicatedMachineInterface -> ListMachine }, { SMRServerInterface -> LinearizabilityAbs });

module DSClientAndHashSet = 
(compose { SMRClientInterface -> DSClientMachine }, { SMRReplicatedMachineInterface -> HashSetMachine }, { SMRServerInterface -> LinearizabilityAbs });

// Test 0: Test that the DSClientAndList is safe
test Test0: main TestDriver1 in (compose { TestDriver1 }, DSClientAndList);

// Test 1: Test that the DSClientAndHashSet is safe
test Test1: main TestDriver1 in (compose { TestDriver1 }, DSClientAndHashSet);
