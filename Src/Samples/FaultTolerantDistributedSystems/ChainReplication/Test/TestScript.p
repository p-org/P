module ChainReplicationBasedSMR  = 
{ SMRServerInterface -> ChainReplicationNodeMachine,
  ChainReplicationNodeInterface -> ChainReplicationNodeMachine,
  ChainReplicationMasterInterface -> ChainReplicationMasterMachine, 
  ChainReplicationFaultDetectorInterface -> ChainReplicationFaultDetectionMachine, 
  ITimer -> Timer 
};

module TestDriver1  = { SMRClientInterface-> TestDriver1 };

module TestDriver2 = { SMRClientInterface -> TestDriver2 };

module SMRReplicated = { SMRReplicatedMachineInterface -> SMRReplicatedMachine };


//test 0: check that the chain replication protocol is safe for fault tolerance = 1
test Test0: (rename SMRClientInterface to Main in (compose ChainReplicationBasedSMR, TestDriver1, SMRReplicated));

//test 1: check that the chain replication protocol is safe for fault tolerance = 2
test Test1: (rename SMRClientInterface to Main in (compose ChainReplicationBasedSMR, TestDriver2, SMRReplicated));

module CRWithSafetyInvariants = (assert UpdatePropagationInvariants in (compose ChainReplicationBasedSMR, SMRReplicated));
module CRWithProgressSpec = (assert ProgressUpdateHasResponse in (compose ChainReplicationBasedSMR, SMRReplicated));

//test 2: check that the chain replication protocol satisfy safety invariants for fault tolerance = 1
test Test2: (rename SMRClientInterface to Main in (compose CRWithSafetyInvariants, TestDriver1));

//test 3: check that the chain replication protocol satisfy safety invariants for fault tolerance = 2
test Test3: (rename SMRClientInterface to Main in (compose CRWithSafetyInvariants, TestDriver2));

//test 4: check that the chain replication protocol satisfy liveness for fault tolerance = 1
test Test4: (rename SMRClientInterface to Main in (compose CRWithProgressSpec, TestDriver1));

//test 5: check that the chain replication protocol satisfy liveness for fault tolerance = 2
test Test5: (rename SMRClientInterface to Main in (compose CRWithProgressSpec, TestDriver2));


//Refinement based testing

//The module that implements the linearizability abstraction for the SMR protocols
module LinearAbs = { SMRServerInterface -> LinearizabilityAbs };

//test 6: check that the chain replication protocol refines linearizability abstraction
module LHS1 =
    (rename SMRClientInterface to Main in (compose ChainReplicationBasedSMR, TestDriver2, SMRReplicated));

module RHS1 = 
    // hide SMR Server creation and Replicated Machine creation operation
    (hidei SMRServerInterface, SMRReplicatedMachineInterface in
    //hide events not important for the refinement check
    (hidee eSMRReplicatedMachineOperation, eSMRReplicatedLeader in
    (rename SMRClientInterface to Main in (compose LinearAbs, TestDriver2, SMRReplicated))));

test Test6:  LHS1 refines RHS1;

