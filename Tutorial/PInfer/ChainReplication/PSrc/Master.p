// event: new head and tail notification to the client (from master to client)
event eNotifyNewHeadTail: (master: Master, h: Replicate, t: Replicate);
// // event: no replicate alive notification to the client (from master to client)
// event eNotifyNoAlivereplicate: (h: replicate, t: replicate);
// event: new chain shape notification to the replicate (from master to replicatce)
event eNotifyChainShape: (master: Master, replicates: seq[Replicate], position: tPos);
event eNotifyReplicateCrash: (replicate: Replicate, position: tPos);
event eNotifyRecoverReq;
event eNotifyRecoverResp;
event eAck: machine;
event eLogReq;
event eLogResp: (source: Replicate, log: seq[tKVMsg]);

/***************************************************
Master machine monitors whether a set of replicates in the chain are alive (responsive).
It periodically sends ping message to each replicate and waits for a pong message from the replicates.
The replicates that do not send a pong message after multiple attempts are marked as failed
and rerange the chain, then notify to the client replicates so that they can update their view of the system.
***************************************************/
machine Master {
  var failureInjector: FailureInjector;
  // the chain of replicates to be monitored
  var replicates: seq[Replicate];
  // set of registered clients
  var clients: set[Client];
  // set of alive replicates, as a chain
  var alive: seq[Replicate];
  var curHeadAndTail: (h: Replicate, t: Replicate);
  var crashNode: Replicate;
  var ackSet: set[machine];

  start state Init {
    entry (config: (failureInjector: FailureInjector, replicates: seq[Replicate], clients: set[Client])) {
      failureInjector = config.failureInjector;
      replicates = config.replicates;
      clients = config.clients;
      ackSet = default(set[machine]);
      alive = default(seq[Replicate]); // start as empty
      NoticeReplicateAboutChain(config.replicates); // updates alive to all the replicates and notifies them
      EnsureAtLeastOneAliveNode(alive);
      NoticeClientAboutChain();
    }

    on eAck do (source: machine) {
      if (handleAck(source)) {
        send failureInjector, eStartInjectFailure;
        goto WaitingForFailure;
      }
    }
  }

  state WaitingForFailure {
    entry {
      EnsureAtLeastOneAliveNode(alive);
    }

    on eNotifyReplicateCrash do (input: (replicate: Replicate, position: tPos)) {
      crashNode = input.replicate;
      goto RecoverChain;
    }

  }

  state RecoverChain {
    entry {
      var i: int;
      var preNodeLog: seq[tKVMsg];
      var postNodeLog: seq[tKVMsg];
      var msg: tKVMsg;
      ackSet = default(set[machine]);
      if (crashNode == alive[0]) {
        // is head
        alive -= 0;
        NoticeReplicateAboutChain(alive);
        EnsureAtLeastOneAliveNode(alive);
        NoticeClientAboutChain();
      } else if (crashNode == alive[sizeof(alive) - 1]) {
        // is tail
        alive -= sizeof(alive) - 1;
        send alive[sizeof(alive) - 1], eNotifyRecoverReq;
      } else if (crashNode in alive) {
        i = seqGetPosition(alive, crashNode);
        print format("Failure id {0}!!", i);
        send alive[i - 1], eLogReq;
        receive {
          case eLogResp: (input: (source: Replicate, log: seq[tKVMsg])) { 
            preNodeLog = input.log;
          }
        }
        send alive[i + 1], eLogReq;
        receive {
          case eLogResp:(input: (source: Replicate, log: seq[tKVMsg])) { 
            postNodeLog = input.log;
          }
        }
        foreach (msg in DiffLog(preNodeLog, postNodeLog)) {
          send alive[i + 1], eWriteRequest, msg;
        }
        alive -= i;
        NoticeReplicateAboutChain(alive);
        EnsureAtLeastOneAliveNode(alive);
      } else {
        assert false, "replicate had already failed!";
        raise halt;
      }
    }

    on eAck do (source: machine) {
      if (handleAck(source)) {
        goto WaitingForFailure;
      }
    }

    on eNotifyRecoverResp do {
      NoticeReplicateAboutChain(alive);
      EnsureAtLeastOneAliveNode(alive);
      NoticeClientAboutChain();      
    }

    defer eNotifyReplicateCrash;
  }

  // fun RearrangeChain(): seq[Replicate] {
  //   var i: int;
  //   var newAlive: seq[Replicate];
  //   while(i < sizeof(alive)) {
  //     if (alive[i] in respInCurrRound) {
  //       newAlive = seqAppend2Tail(newAlive, alive[i]) as seq[Replicate];
  //     }
  //     i = i + 1;
  //   }
  //   return newAlive;
  // }

  fun NoticeReplicateAboutChain(newAlive: seq[Replicate]) {
    var i: int;
    if (alive != newAlive) {
      alive = newAlive;
      i = 0;
      while(i < sizeof(alive)) {
        send alive[i], eNotifyChainShape, (master = this, replicates = alive, position = i);
        ackSet += (alive[i]); 
        i = i + 1;
      }
    }
  }

  fun NoticeClientAboutChain() {
    var i: int;
    var headAndTail: (master: Master, h: Replicate, t: Replicate);
    headAndTail = (master = this, h = alive[0], t = alive[sizeof(alive) - 1]);
    if (curHeadAndTail != (h = headAndTail.h, t = headAndTail.t)) {
      // notification to the client is assumed to be a reliable send so that the client gets an updated view
      while (i < sizeof(clients)) {
        send clients[i], eNotifyNewHeadTail, headAndTail;
        ackSet += (clients[i]); 
        i = i + 1;
      }
    }
  }

  fun EnsureAtLeastOneAliveNode(alive: seq[Replicate]) {
    if (sizeof(alive) == 0) {
      raise halt; 
    }
  }

  fun handleAck (source: machine): bool {
    if (source in ackSet) {
      ackSet -= (source); 
    }
    if (sizeof(ackSet) == 0) {
      return true;
    }
    return false;
  }

  fun DiffLog(a: seq[tKVMsg], b: seq[tKVMsg]): seq[tKVMsg] {
    var result: seq[tKVMsg];
    var msg: tKVMsg;
    var i: int;
    result = b;
    foreach (msg in a) {
      i = seqGetPosition(result, msg);
      if(!(i == -1)) {
        result -= i;
      }
    }
    return result;
  }
}