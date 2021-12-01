type tProposal = (proposerId: int, round: int, value: int);

event ePrepare: (proposer: Proposer, proposal: tProposal);
event eAccept : (proposer: Proposer, proposal: tProposal);
event eAgree: (acceptor: Acceptor, agreed: tProposal);
event eReject : tProposal;
event eAccepted: (acceptor: Acceptor, accepted: tProposal);

fun DefaultProposal() : tProposal {
    return (proposerId = -1, round = -1, value = -1);
}
machine Proposer
{
  var acceptors: set[Acceptor];
  var majority: int;
  var proposeValue: int;
  var currRound : int;
  var proposerId: int;
  var agreeReceivedFrom : set[Acceptor];
  var agreedProposal: tProposal;

  start state InitProposer {
    entry {
      agreedProposal = (proposerId = proposerId, round = nextRoundId, value = proposeValue);
      majority = sizeof(acceptors)/2 + 1;
      goto ProposerPhaseOne;
    }
  }

  state ProposerPhaseOne {
    entry {
      agreeReceivedFrom = default(set[Acceptor]);
      UnReliableBroadCast(acceptors, ePrepare, agreedProposal);
    }
    ignore eAccepted;

    on eAgree do (proposal: tAgree) {
      agreeReceivedFrom += (proposal.source);

      // the acceptors have agreed for a value based on a larger proposal
      if(ProposalLessThan(promisedAgree.pid, payload.pid))
      {
        agreedValue = proposal.payload.value;
      }

      if(sizeof(agreeReceivedFrom) == majority)
      {
        goto ProposerPhaseTwo;
      }
    }
    on reject do (payload: reject){
      if(nextProposalId.round <= payload.round)
      {
        nextProposalId.round = payload.round + 1;
      }
      goto ProposerPhaseOne;
    }

    on TIMEOUT goto ProposerPhaseOne;
  }

  state ProposerPhaseTwo {
    ignore eAgree;
    entry {
      numOfAcceptRecv = 0;
      SendToAllAcceptors(eAccept, (proposer = this, proposal = agreedProposal));
      StartTimer(timer;
    }
    on eReject do (proposal : tProposal)
    {
      if(nextProposalId.round <= payload.round)
      {
        nextProposalId.round = payload.round;
      }
      CancelTimer(timer);
      goto ProposerPhaseOne;
    }
    on eAccepted do (req: (acceptor: Acceptor, accepted: tProposal)) {
      if(IsProposalIdEqual(req.proposal, agreedProposal)){
        numOfAcceptRecv = numOfAcceptRecv + 1;
      }
      if(numOfAcceptRecv == majority)
      {
        CancelTimer(timer);
        // done proposing lets halt
        raise halt;
      }
    }
    on eTimeOut goto ProposerPhaseOne;
  }
}

fun IsProposalIdEqual(id1: tProposal, id2: tProposal) : bool {
  return id1.proposerId == id2.proposerId && id1.round == id2.round;
}

fun ProposalIdLessThan(id1 : tProposal, id2: tProposal) : bool {
  if(id1.round > id2.round) {
    return false;
  } else {
    if(id1.round == id2.round) {
      return id1.proposerId < id2.proposerId;
    } else {
      return true;
    }
  }
}

machine Acceptor {
  var lastRecvProposal : tProposal;

  start state InitAcceptor {
    entry {
      lastRecvProposal = DefaultProposal();
      goto WaitForRequests;
    }
  }

  state WaitForRequests {
    on ePrepare do (req: (proposer: Proposer, proposal: tProposal)) {
      // have not seen any proposal till now
      if(lastRecvProposal == DefaultProposal() || !ProposalIdLessThan(req.proposal, lastRecvProposal))
      {
        lastRecvProposal = proposal;
        send req.proposer, eAgree, (acceptor = this, agreed = lastRecvProposal);
      }
      else
      {
        send req.proposer, eReject, lastRecvProposal;
      }
    }

    on eAccept do (req: (proposer: Proposer, proposal: tProposal)) {
      if(!IsProposalIdEqual(payload.proposal, lastRecvProposal)
      {
        send req.proposer, eReject, lastRecvProposal;
      }
      else
      {
        send req.proposer, eAccepted, (acceptor = this, accepted = req.proposal);
      }
    }
  }
}