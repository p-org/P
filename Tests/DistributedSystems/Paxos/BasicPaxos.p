/*
The basic paxos algorithm. 
*/
event prepare : (proposer: id, proposal : (round: int, serverId : int), value : int);
event accept : (proposer: id, proposal : (round: int, serverId : int), value : int);
event agree : (proposal : (round: int, serverId : int), value : int) assume 6;
event reject : (proposal : (round: int, serverId : int));
event accepted : (proposal : (round: int, serverId : int), value : int);
event timeout;
event startTimer;
event cancelTimer;
event cancelTimerSuccess;
event local;
event success;

main machine GodMachine {
	var proposers : seq[id];
	var acceptors : seq[id];
	var temp : id;
	start state Init {
		entry {
			temp = new Acceptor();
			acceptors.insert(0, temp);
			temp = new Acceptor();
			acceptors.insert(0, temp);
			temp = new Acceptor();
			acceptors.insert(0, temp);
			
			temp = new Proposer ((acceptors = acceptors, serverId = 1, proposeVal = 1));
			proposers.insert(0, temp);
			temp = new Proposer ((acceptors = acceptors, serverId = 2, proposeVal = 100));
			proposers.insert(0, temp);
			
		}
	}
}


machine Acceptor {
	var lastSeenProposal : (proposal : (round: int, serverId : int), value : int);
	var returnVal : bool;
	var receivedMess : (proposer: id, proposal : (round: int, serverId : int), value : int);
	start state Init {
		entry {
			lastSeenProposal.proposal = (round = -1, serverId = -1);
			lastSeenProposal.value = -1;
			raise(local);
		}
		on local goto Wait;
	}
	
	state Wait {
		on prepare do prepareAction;
		on accept do acceptAction;
	}
	
	fun equal (p1 : (round: int, serverId : int), p2 : (round: int, serverId : int)) : bool {
		if(p1.round == p2.round && p1.serverId == p2.serverId)
			return true;
		else
			return false;
	}
	
	fun lessThan (p1 : (round: int, serverId : int), p2 : (round: int, serverId : int)) : bool {
		if(p1.round < p2.round)
		{
			return true;
		}
		else if(p1.round == p2.round)
		{
			if(p1.serverId < p2.serverId)
				return true;
			else
				return false;
		}
		else
		{
			return false;
		}
	
	}
	action prepareAction {
		receivedMess = ((proposer: id, proposal : (round: int, serverId : int), value : int))payload;
		returnVal = lessThan(receivedMess.proposal, lastSeenProposal.proposal);
		if(lastSeenProposal.value ==  -1)
		{
			send(receivedMess.proposer, agree, (proposal = (round = 0, serverId = 0), value = -1));
			lastSeenProposal.proposal = receivedMess.proposal;
			lastSeenProposal.value = receivedMess.value;
		}
		else if(returnVal)
		{
			send(receivedMess.proposer, reject, (proposal = lastSeenProposal.proposal));
		}
		else 
		{
			send(receivedMess.proposer, agree, lastSeenProposal);
			lastSeenProposal.proposal = receivedMess.proposal;
			lastSeenProposal.value = receivedMess.value;
		}
	}
	
	action acceptAction {
		receivedMess = ((proposer: id, proposal : (round: int, serverId : int), value : int))payload;
		returnVal = equal(receivedMess.proposal, lastSeenProposal.proposal);
		if(!returnVal)
		{
			send(receivedMess.proposer, reject, (proposal = lastSeenProposal.proposal));
		}
		else
		{
			send(receivedMess.proposer, accepted, (proposal = receivedMess.proposal, value = receivedMess.value));
		}
	}
	

}
machine Proposer {
	var acceptors : seq[id];
	var proposeVal : int;
	var majority : int;
	var roundNum : int;
	var myId : int ;
	var nextProposal: (round: int, serverId : int);
	var receivedAgree : seq[(proposal : (round: int, serverId : int), value : int)];
	var iter : int ;
	var maxRound : int;
	var countAccept : int;
	var tempProposal : (round: int, serverId : int);
	var tempVal : int;
	var returnVal : bool;
	var timer: mid;
	
	fun GetNextProposal(maxRound : int) : (round: int, serverId : int) {
		return (round = maxRound + 1, serverId = myId);
	}
	
	fun ClearSeq (s : seq[(proposal : (round: int, serverId : int), value : int)]) {
		assert(sizeof(s) < 10);
		iter = sizeof(s) - 1;
		while(iter >=0)
		{
			s.remove(0);
			iter = iter - 1;
		}
	}
	
	fun equal (p1 : (round: int, serverId : int), p2 : (round: int, serverId : int)) : bool {
		if(p1.round == p2.round && p1.serverId == p2.serverId)
			return true;
		else
			return false;
	}
	
	fun lessThan (p1 : (round: int, serverId : int), p2 : (round: int, serverId : int)) : bool {
		if(p1.round < p2.round)
		{
			return true;
		}
		else if(p1.round == p2.round)
		{
			if(p1.serverId < p2.serverId)
				return true;
			else
				return false;
		}
		else
		{
			return false;
		}
	
	}
	
	start state Init {
		entry {
			acceptors = ((acceptors : seq[id], serverId : int, proposeVal : int))payload.acceptors;
			myId = ((acceptors : seq[id], serverId : int, proposeVal : int))payload.serverId;
			proposeVal = ((acceptors : seq[id], serverId : int, proposeVal : int))payload.proposeVal;
			roundNum = 0;
			maxRound = 0;
			majority = (sizeof(acceptors))/2 + 1;
			timer = new Timer(this);
			raise(local);
		}
		
		on local goto ProposeValuePhase1;
	}
	
	fun BroadCastAcceptors(mess: eid, pay : (proposer: id, proposal : (round: int, serverId : int), value : int)) {
		iter = 0;
		while(iter < sizeof(acceptors))
		{
			send(acceptors[iter], mess, pay);
			iter = iter + 1;
		}
	}
	
	action CountAgree {
		receivedAgree.insert(0, payload);
		if(sizeof(receivedAgree) == majority)
			raise(success);
		
	}
	state ProposeValuePhase1 {
		ignore accepted;
		entry {
			nextProposal = GetNextProposal(maxRound);
			BroadCastAcceptors(prepare, (proposer = this, proposal = (round = nextProposal.round, serverId = myId), value = proposeVal));
			send(timer, startTimer);
		}
		on agree do CountAgree;
		on reject goto ProposeValuePhase1 {
			if(nextProposal.round <= ((proposal : (round: int, serverId : int)))payload.proposal.round)
				maxRound = ((proposal : (round: int, serverId : int)))payload.proposal.round;
			send(timer, cancelTimer);
		};
		on success goto ProposeValuePhase2
		{
			send(timer, cancelTimer);
		};
		on timeout goto ProposeValuePhase1;
	}
	action CountAccepted {
		returnVal = equal(((proposal : (round: int, serverId : int), value : int))payload.proposal, nextProposal);
		if(returnVal)
		{
			countAccept = countAccept + 1;
		}
		if(countAccept == majority)
		{
			raise(success);
		}
	
	}
	
	fun getHighestProposedValue() : int {
		iter = 0;
		tempProposal = (round = -1, serverId = 0);
		while(iter < sizeof(receivedAgree))
		{
			returnVal = lessThan(tempProposal, receivedAgree[iter].proposal);
			if(returnVal)
			{
				tempProposal = receivedAgree[iter].proposal;
				tempVal = receivedAgree[iter].value;
			}
			iter = iter + 1;
		}
		if(tempVal != -1)
		{
			return tempVal;
		}
		else
		{
			return proposeVal;
		}
	}
	
	state ProposeValuePhase2 {
		ignore agree;
		entry {
		
			countAccept = 0;
			proposeVal = getHighestProposedValue();
			BroadCastAcceptors(accept, (proposer = this, proposal = nextProposal, value = proposeVal));
			send(timer, startTimer);
		}
		
		on accepted do CountAccepted;
		on reject goto ProposeValuePhase1 {
			if(nextProposal.round <= ((proposal : (round: int, serverId : int)))payload.proposal.round)
				maxRound = ((proposal : (round: int, serverId : int)))payload.proposal.round;
				
			ClearSeq(receivedAgree);
			send(timer, cancelTimer);
		};
		on success goto Done
		{
			send(timer, cancelTimer);
		};
		on timeout goto ProposeValuePhase1
		{
			ClearSeq(receivedAgree);
		};
		
	}
	
	state Done {
		ignore reject, agree, timeout, accepted;
	}
}

model machine Timer {
	var target: id;
	start state Init {
		entry {
			target = (id)payload;
			raise(local);
		}
		on local goto Loop;
	}

	state Loop {
		ignore cancelTimer;
		on startTimer goto TimerStarted;
	}

	state TimerStarted {
		ignore startTimer;
		entry {
			if (*) {
				send(target, timeout);
				raise(local);
			}
		}
		on local goto Loop;
		on cancelTimer goto Loop;
	}
}