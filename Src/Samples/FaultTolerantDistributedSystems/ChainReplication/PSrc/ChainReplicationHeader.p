
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
event eBackwardAck: (SeqId: int);

//event to forward the update operation towards the tail of the chain.
event eForwardUpdate: (msg: (seqId: int, smrop: SMROperationType), pred: ChainReplicationNodeInterface);

//local events
event local;