/*
The basic paxos algorithm. 
*/
event prepare : (proposer: id, proposal : (round: int, serverId : int)) assume 3;
event accept : (proposer: id, proposal : (round: int, serverId : int), value : int) assume 3;
event agree : (proposal : (round: int, serverId : int), value : int) assume 6;
event reject : (proposal : (round: int, serverId : int)) assume 6;
event accepted : (proposal : (round: int, serverId : int), value : int) assume 6;
event local;
event success;
event allNodes: (nodes: seq[id]);
event goPropose;
event Chosen : (command:int);
/**** client events ********/
event update : (seqId: int, command : int);

machine PaxosNode {

	var currentLeader : (rank:int, server : id);
	var leaderElectionService : id;
/********************** Proposer **************************************************/
	var acceptors : seq[id];
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
	var receivedMess_1 : (proposal : (round: int, serverId : int), value : int);
/*************************** Acceptor **********************************************/
	var lastSeenProposal : (proposal : (round: int, serverId : int), value : int);
	var receivedMess_2 : (proposer: id, proposal : (round: int, serverId : int), value : int);
	
	
	start state Init {
		defer Ping;
		entry {
			lastSeenProposal.proposal = (round = -1, serverId = -1);
			lastSeenProposal.value = -1;
			myRank = ((rank:int))payload.rank;
			roundNum = 0;
			maxRound = 0;
			timer = new Timer((this, 10));
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
			proposeVal = payload.command;
			raise(goPropose);
		}
		else
		{
			//forward it to the leader
			send(currentLeader.server, update, payload);
		}
	}
	state PerformOperation {
		ignore agree;
		
		/***** proposer ******/
		on update do CheckIfLeader;
		on goPropose push ProposeValuePhase1;
		
		/***** acceptor ****/
		on prepare do prepareAction;
		on accept do acceptAction;
		
		/**** leaner ****/
		on Chosen goto RunLearner;
		
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
		receivedMess_2.proposal = ((proposer: id, proposal : (round: int, serverId : int)))payload.proposal;
		receivedMess_2.proposer = ((proposer: id, proposal : (round: int, serverId : int)))payload.proposer;
		returnVal = lessThan(receivedMess_2.proposal, lastSeenProposal.proposal);
		if(lastSeenProposal.value ==  -1)
		{
			send(receivedMess_2.proposer, agree, (proposal = (round = -1, serverId = -1), value = -1));
			lastSeenProposal.proposal = receivedMess_2.proposal;
		}
		else if(returnVal)
		{
			send(receivedMess_2.proposer, reject, (proposal = lastSeenProposal.proposal));
		}
		else 
		{
			send(receivedMess_2.proposer, agree, lastSeenProposal);
			lastSeenProposal.proposal = receivedMess_2.proposal;
		}
	}
	
	action acceptAction {
		receivedMess_2 = ((proposer: id, proposal : (round: int, serverId : int), value : int))payload;
		returnVal = equal(receivedMess_2.proposal, lastSeenProposal.proposal);
		if(!returnVal)
		{
			send(receivedMess_2.proposer, reject, (proposal = lastSeenProposal.proposal));
		}
		else
		{
			lastSeenProposal.proposal = receivedMess_2.proposal;
			lastSeenProposal.value = receivedMess_2.value;
			send(receivedMess_2.proposer, accepted, (proposal = receivedMess_2.proposal, value = receivedMess_2.value));
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
		receivedMess_1 = ((proposal : (round: int, serverId : int), value : int))payload;
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
	state ProposeValuePhase1 {
		ignore accepted;
		entry {
			countAgree = 0;
			nextProposal = GetNextProposal(maxRound);
			receivedAgree = (proposal = (round = -1, serverId = -1), value = -1);
			BroadCastAcceptors(prepare, (proposer = this, proposal = (round = nextProposal.round, serverId = myRank)));
			invoke ValidityCheck(monitor_proposer_sent, proposeVal);
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
			invoke ValidityCheck(monitor_proposer_sent, proposeVal);
			
			BroadCastAcceptors(accept, (proposer = this, proposal = nextProposal, value = proposeVal));
			send(timer, startTimer);
		}
		
		on accepted do CountAccepted;
		on reject goto ProposeValuePhase1 {
			if(nextProposal.round <= ((proposal : (round: int, serverId : int)))payload.proposal.round)
				maxRound = ((proposal : (round: int, serverId : int)))payload.proposal.round;
				
			send(timer, cancelTimer);
		};
		on success goto DoneProposal
		{
			//the value is chosen, hence invoke the monitor on chosen event
			invoke BasicPaxosInvariant_P2b(monitor_valueChosen, (proposer = this, proposal = nextProposal, value = proposeVal));
		
			send(timer, cancelTimer);
		};
		on timeout goto ProposeValuePhase1;
		
	}
	
	state DoneProposal {
		entry {
			invoke ValidityCheck(monitor_proposer_chosen, proposeVal);
			raise(Chosen, (command = proposeVal));
		}
	}
	
	/**************************** Learner *******************************************/
	
	state RunLearner {
		ignore agree, accepted, timeout, prepare, reject, accept;
		entry {
		}
	
	}
}

/*
Properties :
The property we check is that 
P2b : If a proposal is chosen with value v , then every higher numbered proposal issued by any proposer has value v.

*/

event monitor_valueChosen : (proposer: id, proposal : (round: int, serverId : int), value : int);
event monitor_valueProposed : (proposer: id, proposal : (round: int, serverId : int), value : int);

monitor BasicPaxosInvariant_P2b {
	var lastValueChosen : (proposer: id, proposal : (round: int, serverId : int), value : int);
	var returnVal : bool;
	var receivedValue : (proposer: id, proposal : (round: int, serverId : int), value : int);
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
			lastValueChosen = ((proposer: id, proposal : (round: int, serverId : int), value : int))payload;
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
			receivedValue = ((proposer: id, proposal : (round: int, serverId : int), value : int)) payload;
			assert(lastValueChosen.value == receivedValue.value);
		};
		on monitor_valueProposed goto CheckValueProposed {
		receivedValue = ((proposer: id, proposal : (round: int, serverId : int), value : int)) payload;
		returnVal = lessThan(lastValueChosen.proposal, receivedValue.proposal);
			if(returnVal)
				assert(lastValueChosen.value == receivedValue.value);
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
				send(target, timeout, (myId = this));
				raise(local);
			}
		}
		on local goto Loop;
		on cancelTimer goto Loop;
	}
}



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
		on local goto PumpOneRequest;
	}
	
	state PumpOneRequest {
		entry {
			
			invoke ValidityCheck(monitor_client_sent, 1);
			if(*)
				send(servers[0], update, (seqId  = 0, command = 1));
			else
				send(servers[sizeof(servers) - 1], update, (seqId  = 0, command = 1));
		}
	}

}
