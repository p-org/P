/*** User defined types ***/
type tReadReplicaReq = (client: KVServer, key: tKey, rTime: tTime, rId: int);
type tReadReplicaResp = (record = tRecord, rTime: tTime, rId: int);

/*** ReadReplica Events ***/
// event:: read request representing the client reads
event eReadReplicaReq: tReadReplicaReq;
// event:: response to read request from the clients
event eReadReplicaResp: tReadReplicaResp;

/***********************************************
ReadReplica machine implements the simple replica
that maintains the current state of the KV store
to service read requests

Actions::
(1) service read requests
(2) subscribe to write requests from the log to get latest writes (in-order).
************************************************/
event eReadFromLog;
machine ReadReplica {
  var storage: map[tKey, tRecord];
  var low_water_mark: tLogTime;
  var log: AppendLog;

  start state ServiceReadRequests {
    on eReadReplicaReq do (req: tReadReplicaReq) {
      send req.client, eReadReplicaResp, (record = storage[req.key], rTime = req.rTime, rId = req.rId);
    }
  }
}