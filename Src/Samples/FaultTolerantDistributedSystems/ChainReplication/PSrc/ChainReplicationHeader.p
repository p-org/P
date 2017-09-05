
/*******************************************************************************
* Description: 
* This file declares all the events and types used by the chain replication protocol.
********************************************************************************/

//event sent by the creator to initialize the predecessor and the successor nodes
event ePredSucc : (pred: ChainReplicationNodeInterface, succ: ChainReplicationNodeInterface);

//event update operation received from the client
event eUpdate : SMROperationType;

//event query operation received from the client
event eQuery : SMROperationType;

//event response to the client for a query operation
event eResponseToQuery: (val: data);

//event response to the client for an update operation
event eResponseToUpdate;

//event to send backward acknowledgement to the predecessor after responding to the client.
event eBackwardAck: (seqId: int);

//event to forward the update operation towards the tail of the chain.
event eForwardUpdate: (msg: (seqId: int, smrop: SMROperationType), pred: ChainReplicationNodeInterface);


//// Events used by Fault Detector and the Master Node

//event sent by Fault detector to all nodes periodically
event eCRPing assume 1 : ChainReplicationFaultDetectorInterface;

//event sent by all nodes in response to Ping
event eCRPong assume 1;

//event sent by Master to Fault detector
event eFaultCorrected: (newconfig: seq[ChainReplicationNodeInterface]);

//event sent ny fault detector to Master
event eFaultDetected: ChainReplicationNodeInterface;

event eBecomeHead : ChainReplicationMasterInterface;
event eBecomeTail : ChainReplicationMasterInterface;
event eNewPredecessor : (pred : ChainReplicationNodeInterface, master : ChainReplicationMasterInterface);
event eNewSuccessor : (succ : ChainReplicationNodeInterface, master : ChainReplicationMasterInterface, lastUpdateRec: int, lastAckSent: int);
event eNewSuccInfo : (lastUpdateRec : int, lastAckSent : int);
event eSuccess;
event eHeadChanged;
event eTailChanged;
//local events
event local;


// Types
enum NodeType {
	HEAD,
	TAIL,
	INTERNAL
}

// All the interfaces
interface ChainReplicationNodeInterface((client: SMRClientInterface, reorder: bool, isRoot : bool, ft : FaultTolerance, val: data)) receives eBackwardAck, eForwardUpdate, ePredSucc, eCRPing, eBecomeTail, eBecomeHead, eNewPredecessor, eNewSuccessor, halt;

interface ChainReplicationFaultDetectorInterface ((master: ChainReplicationMasterInterface, nodes: seq[ChainReplicationNodeInterface])) receives eCRPong, eFaultCorrected;

interface ChainReplicationMasterInterface((client: SMRClientInterface, nodes: seq[ChainReplicationNodeInterface])) receives eFaultDetected, eNewSuccInfo, eTailChanged, eHeadChanged, eSuccess;
