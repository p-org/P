/*
The basic paxos algorithm. 
*/
event prepare : (proposer: id, proposal : (round: int, serverId : int), value : int) assume 3;
event accept : (proposer: id, proposal : (round: int, serverId : int), value : int) assume 3;
event agree : (proposal : (round: int, serverId : int), value : int) assume 6;
event reject : (proposal : (round: int, serverId : int)) assume 6;
event accepted : (proposal : (round: int, serverId : int), value : int) assume 6;
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
	var receivedAgree : (proposal : (round: int, serverId : int), value : int);
	var iter : int ;
	var maxRound : int;
	var countAccept : int;
	var countAgree : int;
	var tempVal : int;
	var returnVal : bool;
	var timer: mid;
	var receivedMess : (proposal : (round: int, serverId : int), value : int);
	
	
	fun GetNextProposal(maxRound : int) : (round: int, serverId : int) {
		return (round = maxRound + 1, serverId = myId);
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
			assert(majority == 2);
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
		receivedMess = ((proposal : (round: int, serverId : int), value : int))payload;
		countAgree = countAgree + 1;
		returnVal = lessThan(receivedAgree.proposal, receivedMess.proposal);
		if(returnVal)
		{
			receivedAgree.proposal = receivedMess.proposal;
			receivedAgree.value = receivedMess.value;
		}
		if(countAgree == majority)
			raise(success);
		
	}
	state ProposeValuePhase1 {
		ignore accepted;
		entry {
			countAgree = 0;
			nextProposal = GetNextProposal(maxRound);
			BroadCastAcceptors(prepare, (proposer = this, proposal = (round = nextProposal.round, serverId = myId), value = proposeVal));
			send(timer, startTimer);
		}
		
		exit {
			receivedAgree = (proposal = (round = -1, serverId = -1), value = -1);
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
		if(receivedAgree.value != -1)
		{
			return receivedAgree.value;
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
			//invoke the monitor on proposal event
			invoke BasicPaxosInvariant_P2b(monitor_valueProposed, (proposer = this, proposal = nextProposal, value = proposeVal));
			
			BroadCastAcceptors(accept, (proposer = this, proposal = nextProposal, value = proposeVal));
			send(timer, startTimer);
		}
		
		on accepted do CountAccepted;
		on reject goto ProposeValuePhase1 {
			if(nextProposal.round <= ((proposal : (round: int, serverId : int)))payload.proposal.round)
				maxRound = ((proposal : (round: int, serverId : int)))payload.proposal.round;
				
			send(timer, cancelTimer);
		};
		on success goto Done
		{
			//the value is chosen, hence invoke the monitor on chosen event
			invoke BasicPaxosInvariant_P2b(monitor_valueChosen, (proposer = this, proposal = nextProposal, value = proposeVal));
			send(timer, cancelTimer);
		};
		on timeout goto ProposeValuePhase1;
		
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