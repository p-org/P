// Enums
enum tTransStatus { SUCCESS, ERROR, TIMEOUT }
enum tReadStatus { READ_SUCCESS, READ_ERROR }

// Types
type tWriteTransaction = (key: string, value: int, transId: int);
type tReadTransaction = (key: string);

type tWriteTransReq = (client: machine, trans: tWriteTransaction);
type tWriteTransResp = (transId: int, status: tTransStatus);

type tReadTransReq = (client: machine, key: string);
type tReadTransResp = (key: string, value: int, status: tReadStatus);

type tPrepareReq = (key: string, value: int, transId: int);
type tPrepareResp = (participant: machine, transId: int, status: tTransStatus);

type tCommitTrans = (transId: int);
type tAbortTrans = (transId: int);

type tInformCoordinator = (coordinator: machine);

// Events
event eWriteTransReq: tWriteTransReq;
event eWriteTransResp: tWriteTransResp;
event eReadTransReq: tReadTransReq;
event eReadTransResp: tReadTransResp;
event ePrepareReq: tPrepareReq;
event ePrepareResp: tPrepareResp;
event eCommitTrans: tCommitTrans;
event eAbortTrans: tAbortTrans;
event eInformCoordinator: tInformCoordinator;

// Timer events
event eStartTimer;
event eCancelTimer;
event eTimeOut;