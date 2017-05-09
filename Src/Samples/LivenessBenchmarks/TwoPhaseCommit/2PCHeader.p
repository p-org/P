/****************************
Declaring all common types used in 2PC
*****************************/

//operations
enum AccountOperations {
    ADD_AMOUNT,
    SUBS_AMOUNT
}

//type of operation
type OperationType = (
    op: AccountOperations, 
    val: int
);

//Defines the transaction type 
type TransactionType =
(
    source: machine,
    op1: OperationType,
    op2: OperationType
);

//Account status ac: acount id and val: value in the account.
type ParticipantStatusType = 
(
    part: int,
    val: int
);


/*************************************
Declaring all the events used in 2PC
*************************************/

//Events used for interaction between client and coordinator
event eTransaction : TransactionType;
event eTransactionFailed;
event eTransactionSuccess;
event eReadPartStatus: (source: machine, part:int);
event eRespPartStatus: ParticipantStatusType;

//Events used for interaction between coordinator and participants
event ePrepare : (tid: int, op: OperationType);
event ePrepared : (tid: int);
event eNotPrepared : (tid: int);
event eCommit : (tid: int);
event eAbort : (tid: int);
event eStatusQuery;
event eStatusResp : ParticipantStatusType;


//events used by specification.
event eParticipantCommitted: (part: int, tid:int);
event eParticipantAborted: (part: int, tid: int);


//local events
event local;