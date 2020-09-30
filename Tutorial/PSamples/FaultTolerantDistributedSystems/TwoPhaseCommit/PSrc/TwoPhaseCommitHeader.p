/*******************************************************************************
* Description: 
* This file declares all the events and types used by two phase commit protocol.
********************************************************************************/


/* 
* Operation Type 
* op: represents the operation to be performed by the participants.
* val: payload value associated with the operation.
*/
type OperationType = (
    op: data, 
    val: int
);

/* 
* Transaction Type
* source: represents the source of transaction (client of the two phase commit).
* ops: operation to be performed by each participant
*/  
type TransactionType =
(
    source: ClientInterface,
    op: OperationType
);

/*
* Participant Status
* part: represents the participants unique id
* val: represents the current status of the value stored at participant
*/
type ParticipantStatusType = 
(
    part: int,
    val: data
);

/* Interface types */
// Interface implemented by the client of the 2PC.
interface ClientInterface((CoorClientInterface, int)) receives eRespPartStatus, eTransactionFailed, eTransactionSuccess;
// Interface exported by the Coordinator to the client of 2PC
interface CoorClientInterface((isfaultTolerant: bool)) receives eTransaction, eReadPartStatus;
// Interface exported by the Coordinator to the Participants in 2PC
interface CoorParticipantInterface() receives ePrepared, eNotPrepared, eStatusResp;
// Interface implemented by the Participant machines in 2PC
interface ParticipantInterface((client: CoorParticipantInterface, val : data)) receives ePrepare, eCommit, eAbort, eStatusQuery;

/* Declaring all the events */

/* Events used for interaction between client and coordinator in 2PC */
// Event sent by client to the coordinator
event eTransaction : TransactionType;
// Event sent by coordinator to client if transaction failed
event eTransactionFailed;
// Event sent by coordinator to client if transaction succeeded
event eTransactionSuccess;
// Event sent by client to coordinator for reading the status of participant with id = part
event eReadPartStatus: (source: ClientInterface, part:int);
// Event sent by coordinator to client representing the status of the participant (requested using eReadPartStatus)
event eRespPartStatus: ParticipantStatusType;

/* Events used for interaction between coordinator and participants */
// Event sent from Coor to Participant requesting to prepare for a transaction.
event ePrepare : (tid: int, op: OperationType);
// Event sent from Participant to Coor if it is ready for the transaction.
event ePrepared : (tid: int, part: int);
// Event sent from Participant to Coor if it is not ready for the transaction.
event eNotPrepared : (tid: int);
// Event sent from Coor to Participant requesting to commit the transaction.
event eCommit : (tid: int);
// Event sent from Coor to Participant requesting to abort the transaction.
event eAbort : (tid: int);
// Event sent from Coor to Participant requesting status of the participant.
event eStatusQuery;
// Event sent from Participant to Coor with its status.
event eStatusResp : ParticipantStatusType;


//local event
event local;

interface ITimer1(ITimerClient) receives eStartTimer, eCancelTimer;
interface ITimer2(ITimerClient) receives eStartTimer, eCancelTimer;