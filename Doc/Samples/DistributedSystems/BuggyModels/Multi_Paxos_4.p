/*
The basic paxos algorithm. 
*/
event prepare : (proposer: id, slot : int, proposal : (round: int, serverId : int)) assume 3;
event accept : (proposer: id, slot: int, proposal : (round: int, serverId : int), value : int) assume 3;
event agree : (slot:int, proposal : (round: int, serverId : int), value : int) assume 6;
event reject : (slot: int, proposal : (round: int, serverId : int)) assume 6;
event accepted : (slot:int, proposal : (round: int, serverId : int), value : int) assume 6;
event local;
event success;
event allNodes: (nodes: seq[id]);
event goPropose;
event chosen : (slot:int, proposal : (round: int, serverId : int), value : int);
/**** client events ********/
event update : (seqId: int, command : int);

machine PaxosNode {

	var currentLeader : (rank:int, server : id);
	var leaderElectionService : id;
/********************** Proposer **************************************************/
	var acceptors : seq[id];
	var commitValue : int;
	var proposeVal : int;
	var majority : int;
	var roundNum : int;
	var myRank : int;
	var nextProposal: (round: int, serverId : int);
	var receivedAgree : (proposal : (round: int, serverId : int), value : int);
	var iter : int ;
	var maxRound : int;
	var countAccept : int;
	var countAgree : int;
	var tempVal : int;
	var returnVal : bool;
	var timer: mid;
	var receivedMess_1 : (slot:int, proposal : (round: int, serverId : int), value : int);
	var nextSlotForProposer : int;
	var currCommitOperation : bool;
/*************************** Acceptor **********************************************/
	var acceptorSlots : map[int, (proposal : (round: int, serverId : int), value : int)];
	var receivedMess_2 : (proposer: id, slot : int, proposal : (round: int, serverId : int), value : int);
/**************************** Learner **************************************/
	var learnerSlots : map[int, (proposal : (round: int, serverId : int), value : int)];
	var lastExecutedSlot:int;
	
	start state Init {
		defer Ping;
		entry {
			myRank = ((rank:int))payload.rank;
			currentLeader = (rank = myRank, server = this);
			roundNum = 0;
			maxRound = 0;
			timer = new Timer((this, 10));
			lastExecutedSlot = -1;
			nextSlotForProposer = 0;
		}
		on allNodes do UpdateAcceptors;
		on local goto PerformOperation;
	}
	
	action UpdateAcceptors {
		acceptors = payload.nodes;
		majority = (sizeof(acceptors))/2 + 1;
		assert(majority == 2);
		//Also start the leader election service;
		leaderElectionService = new LeaderElection((servers = acceptors, parentServer = this, rank = myRank));
		
		raise(local);
	}
	
	action CheckIfLeader {
		if(currentLeader.rank == myRank) {
			// I am the leader 
			commitValue = payload.command;
			proposeVal = commitValue;
			raise(goPropose);
		}
		else
		{
			//forward it to the leader
			send(currentLeader.server, update, payload);
		}
	}
	state PerformOperation {
		ignore agree, accepted, timeout;
		
		/***** proposer ******/
		on update do CheckIfLeader;
		on goPropose push ProposeValuePhase1;
		
		/***** acceptor ****/
		on prepare do prepareAction;
		on accept do acceptAction;
		
		/**** leaner ****/
		on chosen push RunLearner;
		
		/*****leader election ****/
		on Ping do ForwardToLE;
		on newLeader do UpdateLeader;
	}
	
	action ForwardToLE {
		send(leaderElectionService, Ping, payload);
	}
	
	action UpdateLeader {
		currentLeader = payload;
	}
	
	action prepareAction {
		receivedMess_2.proposal = ((proposer: id, slot : int, proposal : (round: int, serverId : int)))payload.proposal;
		receivedMess_2.proposer = ((proposer: id, slot : int, proposal : (round: int, serverId : int)))payload.proposer;
		receivedMess_2.slot = ((proposer: id, slot : int, proposal : (round: int, serverId : int)))payload.slot;
		
		if(!(receivedMess_2.slot in acceptorSlots))
		{
			send(receivedMess_2.proposer, agree, (slot = receivedMess_2.slot, proposal = (round = -1, serverId = -1), value = -1));
			acceptorSlots.update(receivedMess_2.slot, (proposal = receivedMess_2.proposal, value = -1));
			leave;
		}
		returnVal = lessThan(receivedMess_2.proposal, acceptorSlots[receivedMess_2.slot].proposal);
		if(returnVal)
		{
			send(receivedMess_2.proposer, reject, (slot = receivedMess_2.slot, proposal = acceptorSlots[receivedMess_2.slot].proposal));
		}
		else 
		{
			send(receivedMess_2.proposer, agree, (slot = receivedMess_2.slot, proposal = acceptorSlots[receivedMess_2.slot].proposal, value = acceptorSlots[receivedMess_2.slot].value));
			acceptorSlots.update(receivedMess_2.slot, (proposal = receivedMess_2.proposal, value = -1));
		}
	}
	
	action acceptAction {
		receivedMess_2 = ((proposer: id, slot:int, proposal : (round: int, serverId : int), value : int))payload;
		if(receivedMess_2.slot in acceptorSlots)
		{
			returnVal = equal(receivedMess_2.proposal, acceptorSlots[receivedMess_2.slot].proposal);
			if(!returnVal)
			{
				send(receivedMess_2.proposer, reject, (slot = receivedMess_2.slot, proposal = acceptorSlots[receivedMess_2.slot].proposal));
			}
			else
			{
				acceptorSlots.update(receivedMess_2.slot, (proposal = receivedMess_2.proposal, value = receivedMess_2.value));
				send(receivedMess_2.proposer, accepted, (slot = receivedMess_2.slot, proposal = receivedMess_2.proposal, value = receivedMess_2.value));
			}
		}
	}
	
	
	
	
	fun GetNextProposal(maxRound : int) : (round: int, serverId : int) {
		return (round = maxRound + 1, serverId = myRank);
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
	
	/**************************** Proposer **********************************************************/
	
	fun BroadCastAcceptors(mess: eid, pay : any) {
		iter = 0;
		while(iter < sizeof(acceptors))
		{
			send(acceptors[iter], mess, pay);
			iter = iter + 1;
		}
	}
	
	action CountAgree {
		receivedMess_1 = ((slot : int, proposal : (round: int, serverId : int), value : int))payload;
		if(receivedMess_1.slot == nextSlotForProposer)
		{
			countAgree = countAgree + 1;
			returnVal = lessThan(receivedAgree.proposal, receivedMess_1.proposal);
			if(returnVal)
			{
				receivedAgree.proposal = receivedMess_1.proposal;
				receivedAgree.value = receivedMess_1.value;
			}
			if(countAgree == majority)
				raise(success);
		}
		
	}
	state ProposeValuePhase1 {
		ignore accepted;
		entry {
			countAgree = 0;
			nextProposal = GetNextProposal(maxRound);
			receivedAgree = (proposal = (round = -1, serverId = -1), value = -1);
			BroadCastAcceptors(prepare, (proposer = this, slot = nextSlotForProposer, proposal = (round = nextProposal.round, serverId = myRank)));
			invoke ValidityCheck(monitor_proposer_sent, proposeVal);
			send(timer, startTimer);
		}
		
		on agree do CountAgree;
		on reject goto ProposeValuePhase1 {
			if(nextProposal.round <= ((slot:int, proposal : (round: int, serverId : int)))payload.proposal.round)
				maxRound = ((slot:int, proposal : (round: int, serverId : int)))payload.proposal.round;
				
			send(timer, cancelTimer);
		};
		on success goto ProposeValuePhase2
		{
			send(timer, cancelTimer);
		};
		on timeout goto ProposeValuePhase1;
	}
	
	action CountAccepted {
		receivedMess_1 = ((slot:int, proposal : (round: int, serverId : int), value : int))payload;
		if(receivedMess_1.slot == nextSlotForProposer)
		{
			returnVal = equal(receivedMess_1.proposal, nextProposal);
			if(returnVal)
			{
				countAccept = countAccept + 1;
			}
			if(countAccept == majority)
			{
				raise(chosen, receivedMess_1);
			}
		}
	
	}
	
	fun getHighestProposedValue() : int {
		if(receivedAgree.value != -1)
		{
			currCommitOperation = false;
			return receivedAgree.value;
		}
		else
		{
			currCommitOperation = true;
			return commitValue;
		}
	}
	
	state ProposeValuePhase2 {
		ignore agree;
		entry {
		
			countAccept = 0;
			proposeVal = getHighestProposedValue();
			//invoke the monitor on proposal event
			invoke BasicPaxosInvariant_P2b(monitor_valueProposed, (proposer = this, proposal = nextProposal, value = proposeVal));
			invoke ValidityCheck(monitor_proposer_sent, proposeVal);
			
			BroadCastAcceptors(accept, (slot = nextSlotForProposer, proposer = this, proposal = nextProposal, value = proposeVal));
			send(timer, startTimer);
		}
		
		exit {
			if(trigger == chosen)
			{
				//the value is chosen, hence invoke the monitor on chosen event
				invoke BasicPaxosInvariant_P2b(monitor_valueChosen, (proposer = this, proposal = nextProposal, value = proposeVal));
			
				send(timer, cancelTimer);
				
				invoke ValidityCheck(monitor_proposer_chosen, proposeVal);

				//increment the nextSlotForProposer
				nextSlotForProposer = nextSlotForProposer + 1;
			}
		}
		on accepted do CountAccepted;
		on reject goto ProposeValuePhase1 {
			if(nextProposal.round <= ((slot:int, proposal : (round: int, serverId : int)))payload.proposal.round)
				maxRound = ((slot:int, proposal : (round: int, serverId : int)))payload.proposal.round;
				
			send(timer, cancelTimer);
		};
		on timeout goto ProposeValuePhase1;
		
	}
	
	/**************************** Learner *******************************************/
	fun RunReplicatedMachine() {
		while(true)
		{
			if((lastExecutedSlot + 1) in learnerSlots)
			{
				//run the machine
				lastExecutedSlot = lastExecutedSlot + 1;
			}
			else
			{
				return;
			}
		}
	
	}
	

	state RunLearner {
		ignore agree, accepted, timeout, prepare, reject, accept;
		defer newLeader;
		entry {
			receivedMess_1 = ((slot:int, proposal : (round: int, serverId : int), value : int)) payload;
			learnerSlots.update(receivedMess_1.slot, (proposal = receivedMess_1.proposal, value = receivedMess_1.value));
			RunReplicatedMachine();
			if(currCommitOperation && commitValue == receivedMess_1.value)
			{
				return;
			}
			else
			{
				proposeVal = commitValue;
				raise(goPropose);
			}
		}
	
	}
}


/*
Properties :
The property we check is that 
P2b : If a proposal is chosen with value v , then every higher numbered proposal issued by any proposer has value v.

*/

event monitor_valueChosen : (proposer: id, slot: int, proposal : (round: int, serverId : int), value : int);
event monitor_valueProposed : (proposer: id, slot:int, proposal : (round: int, serverId : int), value : int);

monitor BasicPaxosInvariant_P2b {
	var lastValueChosen : map[int, (proposal : (round: int, serverId : int), value : int)];
	var returnVal : bool;
	var receivedValue : (proposer: id, slot: int, proposal : (round: int, serverId : int), value : int);
	start state Init {
		entry {
			raise(local);
		
		}
		on local goto WaitForValueChosen;
	}
	
	state WaitForValueChosen {
		ignore monitor_valueProposed;
		entry {
			
		}
		on monitor_valueChosen goto CheckValueProposed
		{
			receivedValue = ((proposer: id, slot:int, proposal : (round: int, serverId : int), value : int))payload;
			lastValueChosen.update(receivedValue.slot, (proposal = receivedValue.proposal, value = receivedValue.value));
		};
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
	
	state CheckValueProposed {
		on monitor_valueChosen goto CheckValueProposed {
			receivedValue = ((proposer: id, slot: int, proposal : (round: int, serverId : int), value : int)) payload;
			assert(lastValueChosen[receivedValue.slot].value == receivedValue.value);
		};
		on monitor_valueProposed goto CheckValueProposed {
			receivedValue = ((proposer: id, slot : int, proposal : (round: int, serverId : int), value : int)) payload;
			returnVal = lessThan(lastValueChosen[receivedValue.slot].proposal, receivedValue.proposal);
			if(returnVal)
				assert(lastValueChosen[receivedValue.slot].value == receivedValue.value);
		};
	}

}


/*
Monitor to check if 
the proposed value is from the set send by the client (accept)
chosen value is the one proposed by atleast one proposer (chosen).
*/
event monitor_client_sent : int;
event monitor_proposer_sent : int;
event monitor_proposer_chosen : int;

monitor ValidityCheck {
	var clientSet : map[int, int];
	var ProposedSet : map[int, int];
	
	start state Init {
		entry {
			raise(local);
		}
		on local goto Wait;
	}
	
	state Wait {
		on monitor_client_sent do addClientSet;
		on monitor_proposer_sent do addProposerSet;
		on monitor_proposer_chosen do checkChosenValidity;
	}
	
	action addClientSet {
		clientSet.update((int)payload, 0);
	}
	
	action addProposerSet {
		assert((int)payload in clientSet);
		ProposedSet.update((int)payload, 0);
	}
	
	action checkChosenValidity {
		assert((int)payload in ProposedSet);
	}
}


/*
The leader election protocol for multi-paxos, the protocol is based on broadcast based approach. 

*/
event Ping : (rank:int, server : id) assume 4;
event newLeader : (rank:int, server : id);
event timeout : (myId : mid);
event startTimer;
event cancelTimer;
event cancelTimerSuccess;

machine LeaderElection {
	var servers : seq[id];
	var parentServer : id;
	var currentLeader : (rank:int, server : id);
	var myRank : int;
	var iter : int;
	
	start state Init {
		entry {
			servers = ((servers: seq[id], parentServer:id, rank : int)) payload.servers;
			parentServer = ((servers: seq[id], parentServer:id, rank : int))payload.parentServer;
			myRank = ((servers: seq[id], parentServer:id, rank : int))payload.rank;
			currentLeader = (rank = myRank, server = this);
			raise(local);
		}
		on local goto SendLeader;
		
	}
	
	state SendLeader {
		entry {
			currentLeader = GetNewLeader();
			assert(currentLeader.rank <= myRank);
			send(parentServer, newLeader, currentLeader);
		}
	}
	model fun GetNewLeader() : (rank:int, server : id) {
			/*iter = 0;
			while(iter < sizeof(servers))
			{
				if((iter + 1) < myRank) {
					if(*)
					{
						return (rank = iter + 1, server = servers[iter]);
					}
				}
				
				iter = iter + 1;	
			}
			return (rank = myRank, server = parentServer);*/
			
			return (rank = 1, server = servers[0]);
		}

}

model machine Timer {
	var target: id;
	var timeoutvalue : int;
	start state Init {
		entry {
			target = ((id, int))payload[0];
			timeoutvalue = ((id, int))payload[1];
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
				//send(target, timeout, (myId = this));
				raise(local);
			}
		}
		on local goto Loop;
		on cancelTimer goto Loop;
	}
}

event response;

main machine GodMachine {
	var paxosnodes : seq[id];
	var temp : id;
	var iter : int;
	start state Init {
		entry {
			temp = new PaxosNode((rank = 3));
			paxosnodes.insert(0, temp);
			temp = new PaxosNode((rank = 2));
			paxosnodes.insert(0, temp);
			temp = new PaxosNode((rank = 1));
			paxosnodes.insert(0, temp);
			//send all nodes the other machines
			iter = 0;
			while(iter < sizeof(paxosnodes))
			{
				send(paxosnodes[iter], allNodes, (nodes = paxosnodes));
				iter = iter + 1;
			}
			//create the client nodes
			new Client(paxosnodes);
		}
	}
}

model machine Client {
	var servers :seq[id];
	start state Init {
		entry {
			new ValidityCheck();
			servers = (seq[id])payload;
			raise(local);
		}
		on local goto PumpRequestOne;
	}
	
	state PumpRequestOne {
		entry {
			
			invoke ValidityCheck(monitor_client_sent, 1);
			if(*)
				send(servers[0], update, (seqId  = 0, command = 1));
			else
				send(servers[sizeof(servers) - 1], update, (seqId  = 0, command = 1));
				
			raise(response);
		}
		on response goto PumpRequestTwo;
	}
	
	state PumpRequestTwo {
		entry {
			
			invoke ValidityCheck(monitor_client_sent, 2);
			if(*)
				send(servers[0], update, (seqId  = 0, command = 2));
			else
				send(servers[sizeof(servers) - 1], update, (seqId  = 0, command = 2));
				
			raise(response);
		}
		on response goto Done;
	}

	state Done {
	
	}
}
