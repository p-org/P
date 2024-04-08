/**************************************
We implemented the basic paxos protocol as described
in the "Paxos Made Simple" paper by Leslie Lamport
****************************************/

machine Main {
  var GC_NumOfAccptNodes : int;
  var GC_NumOfProposerNodes : int;
  var GC_Default_Value: int;
  start state Init {
    entry {
      var proposers: seq[machine];
      var acceptors: seq[machine];
      var temp: machine;
      var index: int;
      GC_NumOfAccptNodes = 3;
      GC_NumOfProposerNodes = 2;
      GC_Default_Value = 0;
      index = 0;
      //create acceptors
      while(index < GC_NumOfAccptNodes)
      {
        temp =  new AcceptorMachine();
        acceptors += (index, temp);
        index = index + 1;
      }
      //create proposers
      index = 0;
      while(index < GC_NumOfProposerNodes)
      {
          temp = new ProposerMachine(acceptors, index + 1);
          proposers += (index, temp);
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
  var GC_Default_Value: int;
  start state Init {
    entry {
      lastRecvProposal = default(ProposalType);
      goto WaitForRequests;
    }
  }

  state WaitForRequests {
    on prepare do (payload: (proposer: machine, proposal: ProposalType)) {
      if(lastRecvProposal.value == GC_Default_Value)
      {
        send payload.proposer, agree, default(ProposalType);
        lastRecvProposal = payload.proposal;
      }
      else if(ProposalLessThan(payload.proposal.pid, lastRecvProposal.pid))
      {
        send payload.proposer, reject, lastRecvProposal.pid;
      }
      else
      {
        send payload.proposer, agree, lastRecvProposal;
        lastRecvProposal = payload.proposal;
      }
    }
    on accept do (payload: (proposer: machine, proposal: ProposalType)) {
      if(!ProposalIdEqual(payload.proposal.pid, lastRecvProposal.pid))
      {
        send payload.proposer, reject, lastRecvProposal.pid;
      }
      else
      {
        send payload.proposer, accepted, payload.proposal;
      }
    }
  }
}

machine ProposerMachine {
  var acceptors: seq[machine];
  var majority: int;
  var serverid: int;
  var timer: machine;
  var proposeValue: int;
  var nextProposalId : ProposalIdType;
  var GC_NumOfAccptNodes : int;
  var GC_NumOfProposerNodes : int;
  var GC_Default_Value: int;

  start state Init {
    entry (payload: (seq[machine], int)){
      GC_NumOfAccptNodes = 3;
      GC_NumOfProposerNodes = 2;
      GC_Default_Value = 0;
      acceptors = payload.0;
      serverid = payload.1;
      //propose some random value;
      proposeValue = serverid * 10 + 1;
      nextProposalId = (serverid = serverid, round = 1);
      majority = GC_NumOfAccptNodes/2 + 1;
      timer = CreateTimer(this);
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
  var numOfAcceptRecv: int;
  var promisedAgree: ProposalType;

  state ProposerPhaseOne {
    ignore accepted;
    entry {
      numOfAgreeRecv = 0;
      SendToAllAcceptors(prepare, (proposer = this, proposal = (pid = nextProposalId, value = proposeValue)));
      StartTimer(timer, 100);
    }

    on agree do (payload: ProposalType) {
      numOfAgreeRecv =numOfAgreeRecv + 1;
      if(ProposalLessThan(promisedAgree.pid, payload.pid))
      {
        promisedAgree = payload;
      }
      if(numOfAgreeRecv == majority)
      {
        //cancel the timer and goto next phase
        CancelTimer(timer);
        goto ProposerPhaseTwo;
      }
    }

    on reject do (payload: ProposalIdType){
      if(nextProposalId.round <= payload.round)
      {
        nextProposalId.round = payload.round + 1;
      }
      CancelTimer(timer);
      goto ProposerPhaseOne;
    }

    on TIMEOUT goto ProposerPhaseOne;
  }

  fun GetValueToBeProposed() : int {
    if(promisedAgree.value == 0)
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
    entry {
      numOfAcceptRecv = 0;
      proposeValue = GetValueToBeProposed();
      SendToAllAcceptors(accept, (proposer = this, proposal = (pid = nextProposalId, value = proposeValue)));
      StartTimer(timer, 100);
    }

    on reject do (payload : ProposalIdType)
    {
      if(nextProposalId.round <= payload.round)
      {
        nextProposalId.round = payload.round;
      }
      CancelTimer(timer);
      goto ProposerPhaseOne;
    }

    on accepted do (payload: ProposalType) {
      if(ProposalIdEqual(payload.pid, nextProposalId)){
        numOfAcceptRecv = numOfAcceptRecv + 1;
      }

      if(numOfAcceptRecv == majority)
      {
        CancelTimer(timer);
        // assert(false);
        // done proposing lets halt
        // raise halt;
      }
    }

    on TIMEOUT goto ProposerPhaseOne;
  }
}
