/*******************************************************************************
* Description: 
* This file declares all the events and types used by the multi paxos protocol.
********************************************************************************/
// Events exchanged between multipaxos nodes and leader election node

//event sent by multipaxos to leader election nodes
event ePing  assume 3 : (rank:int, server : any<MultiPaxosLEEvents>);
event eFwdPing  assume 3 : (rank:int, server : any<MultiPaxosLEEvents>);
event eNewLeader : (rank:int, server : any<MultiPaxosLEEvents>);

/*********************************************
The multi-paxos events. 
*********************************************/
event eUpdate : SMROperationType;
event ePrepare assume 3: (proposer: MultiPaxosNodeInterface, slot : int, proposal : ProposalIdType);
event eAccept  assume 3: (proposer: MultiPaxosNodeInterface, slot: int, proposal : ProposalIdType, smrop : SMROperationType);
event eAgree assume 6: (slot:int, proposal : ProposalIdType, smrop : SMROperationType) ;
event eReject  assume 6: (slot: int, proposal : ProposalIdType);
event eAccepted  assume 6: (slot:int, proposal : ProposalIdType, smrop : SMROperationType);
event eSuccess;
event eGoPropose;
event eChosen : (slot:int, proposal : ProposalIdType, smrop : SMROperationType);
event eAllNodes : seq[MultiPaxosNodeInterface];

event local;
/*********************************************
Types
*********************************************/
type ProposalIdType = (roundId: int, serverId : int);
type LEContructorType = (servers: seq[any<MultiPaxosLEEvents>], parentServer:any<MultiPaxosLEEvents>, rank : int);

/*********************************************
Interface types
**********************************************/
eventset MultiPaxosLEEvents = { eSMROperation,  eNewLeader, eFwdPing };
interface LeaderElectionClientInterface(SMRServerConstrutorType) receives eNewLeader, eFwdPing;
interface LeaderElectionInterface(LEContructorType) receives ePing;
interface MultiPaxosNodeInterface(SMRServerConstrutorType) receives eChosen, eGoPropose, eAccepted, eSuccess, eReject, eAgree, eAccept, ePrepare, eNewLeader, eFwdPing;