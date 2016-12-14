/**************************************
We implemented the basic paxos protocol as described 
in the "Paxos Made Simple" paper by Leslie Lamport
****************************************/

include "TimerHeader.p"
include "PaxosHeader.p"

machine Main {
  start state Init {
    entry {
      var proposers: seq[machine];
      var acceptors: seq[machine];
      var temp: machine;
      var index: int;
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

      raise halt;
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
