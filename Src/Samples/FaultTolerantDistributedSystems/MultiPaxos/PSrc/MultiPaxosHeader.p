/*******************************************************************************
* Description: 
* This file declares all the events and types used by the multi paxos protocol.
********************************************************************************/
// Events exchanged between multipaxos nodes and leader election node

//event sent by multipaxos to leader election nodes
event ePing  assume 3 : (rank:int, server : MultiPaxosNodeInterface);

event eNewLeader : (rank:int, server : MultiPaxosNodeInterface);

/*********************************************
The multi-paxos events. 
*********************************************/
event ePrepare assume 3: (proposer: MultiPaxosNodeInterface, slot : int, proposal : ProposalIdType);
event eAccept  assume 3: (proposer: MultiPaxosNodeInterface, slot: int, proposal : ProposalIdType, smrop : SMROperationType);
event eAgree assume 6: (slot:int, proposal : ProposalIdType, smrop : SMROperationType) ;
event eReject  assume 6: (slot: int, proposal : ProposalIdType);
event eAccepted  assume 6: (slot:int, proposal : ProposalIdType, smrop : SMROperationType);
event eSuccess;
event eGoPropose;
event eChosen : (slot:int, proposal : (round: int, servermachine : int), smrop : SMROperationType);

/*********************************************
Types
*********************************************/
type ProposalIdType = (roundId: int, serverId : int);
type LEContructorType = (servers: seq[MultiPaxosNodeInterface], parentServer:MultiPaxosNodeInterface, rank : int);

/*********************************************
Interface types
**********************************************/
type LeaderElectionInterface(LEContructorType) = { ePing };
type MultiPaxosNodeInterface(SMRServerConstrutorType) = { eChosen, eGoPropose, eAccepted, eSuccess, eReject, eAgree, eAccept, ePrepare, eNewLeader };