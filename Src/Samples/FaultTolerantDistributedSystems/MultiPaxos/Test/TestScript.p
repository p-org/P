module TestDriver1  = { SMRClientInterface-> TestDriver1 };

module TestDriver2 = { SMRClientInterface -> TestDriver2 };

/********************************************
Check that the leader election abstraction is sound
*********************************************/
module LeaderElectionImpClosed = (rename LeaderElectionClientInterface to Main in (compose { LeaderElectionClientInterface -> MultiPaxosLEAbsMachine }, { LeaderElectionInterface -> LeaderElectionMachine, ITimer -> Timer }));

module LeaderElectionAbsClosed = (rename LeaderElectionClientInterface to Main in (compose { LeaderElectionClientInterface -> MultiPaxosLEAbsMachine }, { LeaderElectionInterface -> LeaderElectionAbsMachine }));

test Test0: LeaderElectionImpClosed refines LeaderElectionAbsClosed;

/********************************************
Check that the multi-paxos abstraction used to check Leader election protocol is sound
*********************************************/
test Test1: (rename SMRClientInterface to Main in (compose MultiPaxosWithLeaderAbs, TestDriver1)) 
            refines (hidee ePing in LeaderElectionAbsClosed);




/********************************************
Check that the multi-paxos protocol refines linearizability abstraction
*********************************************/
module SMRReplicated = { SMRReplicatedMachineInterface -> SMRReplicatedMachine };

module MultiPaxos = { MultiPaxosNodeInterface -> MultiPaxosNodeMachine, ITimer -> Timer };

module MultiPaxosWithLeaderAbs = (compose LeaderElectionAbs, MultiPaxos, SMRReplicated);

//test that the multipaxos implementation is safe
test Test2: (rename TestDriver1 to Main in (compose MultiPaxosWithLeaderAbs, TestDriver1));

//The module that implements the linearizability abstraction for the SMR protocols
module LinearAbs = { SMRServerInterface -> LinearizabilityAbs };

//test that MultiPaxos composed with leader election abs refines linearizability abs
module LHS1 =
    (rename SMRClientInterface to Main in (compose MultiPaxosWithLeaderAbs, TestDriver1));

module RHS1 = 
    // hide SMR Server creation and Replicated Machine creation operation
    (hidei SMRServerInterface, SMRReplicatedMachineInterface in
    //hide events not important for the refinement check
    (hidee eSMRReplicatedMachineOperation, eSMRReplicatedLeader in
    (rename SMRClientInterface to Main in (compose LinearAbs, TestDriver2, SMRReplicated))));


test Test3:  LHS1 refines RHS1;



