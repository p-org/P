
/********************************************
Check that the leader election abstraction is sound
*********************************************/

module LeaderElectionAbs { LeaderElectionAbsMachine }
module LeaderElection { LeaderElectionMachine, Timer }
module MultiPaxostoLEAbs 
{ MultiPaxosLEAbsMachine }

module LeaderElectionImpClosed = (rename MultiPaxosLEAbsMachine to Main in (compose MultiPaxostoLEAbs, LeaderElection));

module LeaderElectionAbsClosed = (rename MultiPaxosLEAbsMachine to Main in (compose MultiPaxostoLEAbs, LeaderElectionAbs));

test Test0: LeaderElectionImpClosed refines LeaderElectionAbsClosed;

/********************************************
Check that the multi-paxos abstraction used to check Leader election protocol is sound
*********************************************/
test Test1: (rename TestDriver1 to Main in (compose MultiPaxosWithLeaderAbs, TestDriver1)) refines (hide ePing in LeaderElectionAbsClosed);




/********************************************
Check that the multi-paxos protocol refines linearizability abstraction
*********************************************/
module TestDriver1 { TestDriver1 }

module TestDriver2 { TestDriver2 }

module SMRReplicated { SMRReplicatedMachine }

module MultiPaxos { MultiPaxosNodeMachine, Timer }

module MultiPaxosWithLeaderAbs = (compose LeaderElectionAbs, MultiPaxos, SMRReplicated);

//test that the multipaxos implementation is safe
test Test2: (rename TestDriver1 to Main in (compose MultiPaxosWithLeaderAbs, TestDriver1));

//The module that implements the linearizability abstraction for the SMR protocols
module LinearAbs {
    LinearizabilityAbs
}
//test that MultiPaxos composed with leader election abs refines linearizability abs
module LHS1 =
    //rename machine so that it has same machine name
    (rename MultiPaxosNodeMachine to SMRLeader in 
    (rename TestDriver1 to Main in (compose MultiPaxosWithLeaderAbs, TestDriver1)));

module RHS1 = 
    //rename machine so that it has same machine name
    (rename LinearizabilityAbs to SMRLeader in
    //hide events not important for the refinement check
    (hide eSMRReplicatedMachineOperation, eSMRReplicatedLeader in
    (rename TestDriver2 to Main in (compose LinearAbs, TestDriver2, SMRReplicated))));


test Test3:  LHS1 refines RHS1;



