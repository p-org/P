/* User Defined Types */

// a transaction consisting of the key, value, and the unique transaction id.
type tTrans = (key: string, val: int, transId: int);
// payload type associated with the `eWriteTransReq` event where `client`: client sending the
// transaction, `trans`: transaction to be committed.
type tWriteTransReq = (client: Client, trans: tTrans);
// payload type associated with the `eWriteTransResp` event where `transId` is the transaction Id
// and `status` is the return status of the transaction request.
type tWriteTransResp = (transId: int, status: tTransStatus);
// payload type associated with the `eReadTransReq` event where `client` is the Client machine sending
// the read request and `key` is the key whose value the client wants to read.
type tReadTransReq = (client: Client, key: string);
// payload type associated with the `eReadTransResp` event where `val` is the value corresponding to
// the `key` in the read request and `status` is the read status (e.g., success or failure)
type tReadTransResp = (key: string, val: int, status: tTransStatus);

// transaction status
enum tTransStatus {
  SUCCESS,
  ERROR,
  TIMEOUT
}

/* Events used by the 2PC clients to communicate with the 2PC coordinator */
// event: write transaction request (client to coordinator)
event eWriteTransReq : tWriteTransReq;
// event: write transaction response (coordinator to client)
event eWriteTransResp : tWriteTransResp;
// event: read transaction request (client to coordinator)
event eReadTransReq : tReadTransReq;
// event: read transaction response (participant to client)
event eReadTransResp: tReadTransResp;

/* Events used for communication between the coordinator and the participants */
// event: prepare request for a transaction (coordinator to participant)
event ePrepareReq: tPrepareReq;
// event: prepare response for a transaction (participant to coodinator)
event ePrepareResp: tPrepareResp;
// event: commit transaction (coordinator to participant)
event eCommitTrans: int;
// event: abort transaction (coordinator to participant)
event eAbortTrans: int;

/* User Defined Types */
// payload type associated with the `ePrepareReq` event
type tPrepareReq = tTrans;
// payload type assocated with the `ePrepareResp` event where `participant` is the participant machine
// sending the response, `transId` is the transaction id, and `status` is the status of the prepare
// request for that transaction.
type tPrepareResp = (participant: Participant, transId: int, status: tTransStatus);

// event: inform participant about the coordinator
event eInformCoordinator: Coordinator;

// event: initialize the AtomicityInvariant spec monitor
event eMonitor_AtomicityInitialize: int;

/************************************************
Events used to interact with the timer machine
************************************************/
event eStartTimer;
event eCancelTimer;
event eTimeOut;
event eDelayedTimeOut;
