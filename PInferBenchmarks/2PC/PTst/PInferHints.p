hint Atomicity (e0: eWriteTransResp, e1: ePrepareResp) {
    config_event = eMonitor_AtomicityInitialize;
}

hint AbortCommit (e0: eCommitTrans, e1: eAbortTrans) {}

// hint exact Atomicity_known (e1: eWriteTransResp, e2: ePrepareResp) {
//     num_guards = 2;
//     exists = 0;
//     arity = 2;
//     include_guards = e1.status == SUCCESS && e1.transId == e2.transId;
// }