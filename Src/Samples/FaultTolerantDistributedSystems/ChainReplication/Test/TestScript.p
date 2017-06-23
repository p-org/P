module ChainReplicationBasedSMR 
{ ChainReplicationNodeMachine, ChainReplicationMasterMachine , ChainReplicationFaultDetectionMachine, Timer }

module TestDriver1 { TestDriver1 }

module TestDriver2 { TestDriver2 }

module SMRReplicated { SMRReplicatedMachine }


//test 0: check that the chain replication protocol is safe for fault tolerance = 1
test Test0: (rename TestDriver1 to Main in (compose ChainReplicationBasedSMR, TestDriver1, SMRReplicated));

//test 1: check that the chain replication protocol is safe for fault tolerance = 2
test Test1: (rename TestDriver2 to Main in (compose ChainReplicationBasedSMR, TestDriver2, SMRReplicated));

module CRWithSafetyInvariants = (assert UpdatePropagationInvariants in (compose ChainReplicationBasedSMR, SMRReplicated));
module CRWithProgressSpec = (assert ProgressUpdateHasResponse in (compose ChainReplicationBasedSMR, SMRReplicated));

//test 2: check that the chain replication protocol satisfy safety invariants for fault tolerance = 1
test Test2: (rename TestDriver1 to Main in (compose CRWithSafetyInvariants, TestDriver1));

//test 3: check that the chain replication protocol satisfy safety invariants for fault tolerance = 2
test Test3: (rename TestDriver2 to Main in (compose CRWithSafetyInvariants, TestDriver2));

//test 4: check that the chain replication protocol satisfy liveness for fault tolerance = 1
test Test4: (rename TestDriver1 to Main in (compose CRWithProgressSpec, TestDriver1));

//test 5: check that the chain replication protocol satisfy liveness for fault tolerance = 2
test Test5: (rename TestDriver2 to Main in (compose CRWithProgressSpec, TestDriver2));