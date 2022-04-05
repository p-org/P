/**************************************
We implemented the basic paxos protocol as described 
in the "Paxos Made Simple" paper by Leslie Lamport
****************************************/

machine Main {
  var GC_NumOfAccptNodes : int;
  var GC_NumOfProposerNodes : int;
  //var GC_Default_Value: int;
  start state Init {
    entry {
      var proposers: seq[machine];
      var acceptors: seq[machine];
      var temp: machine;
      var index: int;
      GC_NumOfAccptNodes = 3;
      GC_NumOfProposerNodes = 2;
      //GC_Default_Value = 0;
      index = 0;
      //create acceptors
      while(index < GC_NumOfAccptNodes)
      {
        temp =  new AcceptorMachine();
        acceptors += (index, temp);
        index = index + 1;
      }
      //create proposers - one client per proposer
      index = 0;
      while(index < GC_NumOfProposerNodes)
      {
          temp = new ProposerMachine(acceptors, index + 1);
          proposers += (index, temp);
          new TestClient(temp);
          index = index + 1;
      }

//      raise halt;
    }
  }
}

fun ProposalIdEqual(id1: ProposalIdType, id2: ProposalIdType) : bool {
  if(id1.serverid == id2.serverid && id1.round == id2.round)
  {
    return true;
  }
  else
  {
    return false;
  }
}

fun ProposalLessThan(id1: ProposalIdType, id2: ProposalIdType) : bool {
  if(id1.round < id2.round)
  {
    return true;
  }
  else if(id1.round == id2.round)
  {
    if(id1.serverid < id2.serverid)
    {
      return true;
    }
    else
    {
      return false;
    }
  }
  else
  {
    return false;
  }
}

machine AcceptorMachine {
  var lastRecvProposal : ProposalType;
  var GC_NumOfAccptNodes : int;
  var GC_NumOfProposerNodes : int;
  //var GC_Default_Value: int;
  var store: map[tPreds,tPreds];
  start state Init {
    entry {
      lastRecvProposal = default(ProposalType);
      goto WaitForRequests;
    }
  }

  state WaitForRequests {
    on read do (req: readRequest) {
      send req.client, readResp, (key = req.key, val = store[req.key]);
    }

    on prepare do (payload: (proposer: machine, proposal: ProposalType)) {
      var readRec : tRecord;
      var emptyPreds : seq[tPreds];
      if(lastRecvProposal.value == default(tRecord))
      {
        send payload.proposer, agree, default(ProposalType);
        if (payload.proposal.ty == WRITE) {
          lastRecvProposal = payload.proposal;
        } else {
            readRec = payload.proposal.value;
            if (payload.proposal.value.key in store) {
              readRec = (key = payload.proposal.value.val, val = store[payload.proposal.value.key]);
            }
            lastRecvProposal = (ty = READ, pid = payload.proposal.pid, value = readRec);
         }
      }
      else if(ProposalLessThan(payload.proposal.pid, lastRecvProposal.pid))
      {
        send payload.proposer, reject, lastRecvProposal.pid;
      }
      else 
      {
        send payload.proposer, agree, lastRecvProposal;
        if (payload.proposal.ty == WRITE)
          lastRecvProposal = payload.proposal;
        else {
          if (payload.proposal.value.key in store)
            readRec = (key = payload.proposal.value.val, val = store[payload.proposal.value.key]);
          else
            readRec = (key = payload.proposal.value.val, val = choose(emptyPreds));
          lastRecvProposal = (ty = READ, pid = payload.proposal.pid, value = readRec);
        }
      }
    }
    on accept do (payload: (proposer: machine, proposal: ProposalType)) {
      if(!ProposalIdEqual(payload.proposal.pid, lastRecvProposal.pid))
      {
        send payload.proposer, reject, lastRecvProposal.pid;
      }
      else
      {
        if (payload.proposal.ty == WRITE) {
          store[payload.proposal.value.key] = payload.proposal.value.val;
          send payload.proposer, accepted, payload.proposal;
        } else {
          send payload.proposer, accepted, payload.proposal;
        }
      }
      lastRecvProposal = default(ProposalType);
    }
  }
}

machine ProposerMachine {
  var acceptors: seq[machine];
  var majority: int;
  var serverid: int;
  var proposeOp: op;
  var proposeValue: tRecord;
  var nextProposalId : ProposalIdType;
  var GC_NumOfAccptNodes : int;
  var GC_NumOfProposerNodes : int;
  //var GC_Default_Value: int;
  var client: machine;

  start state Init {
    entry (payload: (seq[machine], int)){
      GC_NumOfAccptNodes = 3;
      GC_NumOfProposerNodes = 2;
      //GC_Default_Value = 0;
      acceptors = payload.0;
      serverid = payload.1;
      goto WaitForClient;
    }
  }

  state WaitForClient {
      on write do (req : writeRequest) {
        client = req.client;
        proposeValue = req.rec;
        proposeOp = WRITE;
        nextProposalId = (serverid = serverid, round = 1);
        majority = GC_NumOfAccptNodes/2 + 1;
        goto ProposerPhaseOne;
      }

      on read do (req: readRequest) {
        var emptyPreds : seq[tPreds];
        client = req.client;
        proposeValue = (key = req.key, val = choose(emptyPreds));
        proposeOp = READ;
        nextProposalId = (serverid = serverid, round = 1);
        majority = GC_NumOfAccptNodes/2 + 1;
        goto ProposerPhaseOne;
      }
  }

  fun SendToAllAcceptors(e: event, v: any) {
    var index: int;
    index = 0;
    while(index < sizeof(acceptors))
    {
      send acceptors[index], e, v;  
      index = index + 1;
    }
  }

  var numOfAgreeRecv: int;
  var numOfAgreeRead: map[tPreds, int];
  var numOfAcceptRecv: int;
  var promisedAgree: ProposalType;

  state ProposerPhaseOne {
    defer write;
    defer read;
    ignore accepted;
    entry {
      numOfAgreeRecv = 0;
      SendToAllAcceptors(prepare, (proposer = this, proposal = (ty = proposeOp, pid = nextProposalId, value = proposeValue)));
    }

    on agree do (payload: ProposalType) {
      if (proposeOp == WRITE) {
        numOfAgreeRecv =numOfAgreeRecv + 1;
      }
      else if (proposeOp == READ) {
        if(promisedAgree.pid == payload.pid) {
          if (payload.value.val in numOfAgreeRead) {
            numOfAgreeRead[payload.value.val] = numOfAgreeRead[payload.value.val] + 1;
            if (numOfAgreeRead[payload.value.val] > numOfAgreeRecv) {
              numOfAgreeRecv = numOfAgreeRead[payload.value.val];
              proposeValue = payload.value;
            }
          }
        }
      }
      if(ProposalLessThan(promisedAgree.pid, payload.pid))
      {
        promisedAgree = payload;
      }
      if(numOfAgreeRecv == majority)
      {
        goto ProposerPhaseTwo;
      }
    }

    on reject do (payload: ProposalIdType){
      if(nextProposalId.round <= payload.round)
      {
        nextProposalId.round = payload.round + 1;
      }
      goto ProposerPhaseOne;
    }

  }

  fun GetValueToBeProposed() : tRecord {
    if(promisedAgree.value == default(tRecord))
    {
      return proposeValue;
    }
    else
    {
      return promisedAgree.value;
    }
  }

  state ProposerPhaseTwo {
    ignore agree;
    defer write;
    defer read;
    entry {
      numOfAcceptRecv = 0;
      proposeValue = GetValueToBeProposed();
      SendToAllAcceptors(accept, (proposer = this, proposal = (ty = proposeOp, pid = nextProposalId, value = proposeValue)));
    }

    on reject do (payload : ProposalIdType)
    {
      if(nextProposalId.round <= payload.round)
      {
        nextProposalId.round = payload.round;
      }
      goto ProposerPhaseOne;
    }
    
    on accepted do (payload: ProposalType) {
      if(ProposalIdEqual(payload.pid, nextProposalId)){
        numOfAcceptRecv = numOfAcceptRecv + 1;
      }

      if(numOfAcceptRecv == majority)
      {
        if (payload.ty == WRITE)
          send client, writeResp;
        else
          send client, readResp, payload.value;
        goto WaitForClient; //raise halt;
      }
    }
  }
}
