event eWriteLogEntryReq: tWriteLogEntryReq;
event eWriteLogEntryResp: tWriteLogEntryResp;
event eReadNextLogEntryReq: tReadNextLogEntryReq;
event eReadNextLogEntryResp: tReadNextLogEntryResp;

machine AppendLog {
  var log: seq[tRecord];
  var readIndex: int;
  var registeredReplica: ReadReplica;

  start state WaitForRequests {
    on eWriteLogEntryReq do (req: tWriteLogEntryReq) {
      PublishEntriesToReplica();
    }

    on ePublishToSubscribedReplica do () {

    }
  }

  fun PublishEntriesToReplica() {
    if(sizeof(log) > 0)
      send this, ePublishToSubscribedReplica;
  }
}