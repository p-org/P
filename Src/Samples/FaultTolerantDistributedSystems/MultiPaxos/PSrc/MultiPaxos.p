machine MultiPaxosNodeMachine : MultiPaxosNodeInterface, SMRServerInterface
receives eTimeOut, eCancelSuccess, eCancelFailure;
sends eSMRLeaderUpdated, eSMRReplicatedMachineOperation, eStartTimer, eCancelTimer;
{

	var currentLeader : (rank:int, server : machine);
	var leaderElectionService : machine;

// Proposer 
	var acceptors : seq[machine];
	var commitValue : int;
	var proposeVal : int;
	var majority : int;
	var roundNum : int;
	var myRank : int;
	var nextProposal: (round: int, servermachine : int);
	var receivedAgree : (proposal : (round: int, servermachine : int), value : int);
	var iter : int ;
	var maxRound : int;
	var countAccept : int;
	var countAgree : int;
	var tempVal : int;
	var timer: machine;
	var nextSlotForProposer : int;
//Acceptor 
	var acceptorSlots : map[int, (proposal : (round: int, servermachine : int), value : int)];
	
//Learner 
	var learnerSlots : map[int, (proposal : (round: int, servermachine : int), value : int)];
	var lastExecutedSlot:int;
	var learner : SMR_REPLICATED_MACHINE_IN;
	start state Init {
		defer Ping;
		entry {
			myRank = (payload as (rank:int)).rank;
			currentLeader = (rank = myRank, server = this);
			roundNum = 0;
			maxRound = 0;
			timer = new Timer_Machine(this as TIMER_CLIENT_IN);
			lastExecutedSlot = -1;
			nextSlotForProposer = 0;
			learner = new SMR_REPLICATED_MACHINE_IN(myRank);
		}
		on allNodes do UpdateAcceptors;
		on local goto PerformOperation;
	}
	
	fun UpdateAcceptors(nodes : seq[machines]) {
		acceptors = nodes;
		majority = (sizeof(acceptors))/2 + 1;
		assert(majority == 2);
		//Also start the leader election service;
		leaderElectionService = new LeaderElection_Machine((servers = acceptors, parentServer = this, rank = myRank));
		
		raise(local);
	}
	
	fun CheckIfLeader() {
		if(currentLeader.rank == myRank) {
			// I am the leader 
			commitValue = payload.command;
			proposeVal = commitValue;
			raise(goPropose);
		}
		else
		{
			//forward it to the leader
			SEND(currentLeader.server, update, payload);
		}
	}
	state PerformOperation {
		ignore agree, accepted, timeout, reject;
		
		// proposer
		on update do CheckIfLeader;
		on goPropose push ProposeValuePhase1;
		
		//acceptor
		on prepare do prepareAction;
		on accept do acceptAction;
		
		// leaner
		on chosen push RunLearner;
		
		//leader election
		on Ping do ForwardToLE;
		on newLeader do UpdateLeader;
	}
	
	fun ForwardToLE() {
		//send leaderElectionService, Ping, payload;
	}
	
	fun UpdateLeader() {
		currentLeader = payload;
	}
	
	fun prepareAction() {
		
		if(!(payload.slot in acceptorSlots))
		{
			SEND(payload.proposer, agree, (slot = payload.slot, proposal = (round = -1, servermachine = -1), value = -1));
			acceptorSlots[payload.slot] = (proposal = payload.proposal, value = -1);
			return;
		}

		if(lessThan(payload.proposal, acceptorSlots[payload.slot].proposal))
		{
			SEND(payload.proposer, reject, (slot = payload.slot, proposal = acceptorSlots[payload.slot].proposal));
		}
		else 
		{
			SEND(payload.proposer, agree, (slot = payload.slot, proposal = acceptorSlots[payload.slot].proposal, value = acceptorSlots[payload.slot].value));
			acceptorSlots[payload.slot] = (proposal = payload.proposal, value = -1);
		}
	}
	
	fun acceptAction (){
		if(payload.slot in acceptorSlots)
		{
			if(!equal(payload.proposal, acceptorSlots[payload.slot].proposal))
			{
				SEND(payload.proposer, reject, (slot = payload.slot, proposal = acceptorSlots[payload.slot].proposal));
			}
			else
			{
				acceptorSlots[payload.slot] = (proposal = payload.proposal, value = payload.value);
				SEND(payload.proposer, accepted, (slot = payload.slot, proposal = payload.proposal, value = payload.value));
			}
		}
	}
	
	
	
	
	fun GetNextProposal(maxRound : int) : (round: int, servermachine : int) {
		return (round = maxRound + 1, servermachine = myRank);
	}
	
	fun equal (p1 : (round: int, servermachine : int), p2 : (round: int, servermachine : int)) : bool {
		if(p1.round == p2.round && p1.servermachine == p2.servermachine)
			return true;
		else
			return false;
	}
	
	fun lessThan (p1 : (round: int, servermachine : int), p2 : (round: int, servermachine : int)) : bool {
		if(p1.round < p2.round)
		{
			return true;
		}
		else if(p1.round == p2.round)
		{
			if(p1.servermachine < p2.servermachine)
				return true;
			else
				return false;
		}
		else
		{
			return false;
		}
	
	}
	

	
	fun BroadCastAcceptors(mess: event, pay : any) {
		iter = 0;
		while(iter < sizeof(acceptors))
		{
			SEND(acceptors[iter], mess, pay);
			iter = iter + 1;
		}
	}
	
	fun CountAgree() {
		if(payload.slot == nextSlotForProposer)
		{
			countAgree = countAgree + 1;
			if(lessThan(receivedAgree.proposal, payload.proposal))
			{
				receivedAgree.proposal = payload.proposal;
				receivedAgree.value = payload.value;
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
			receivedAgree = (proposal = (round = -1, servermachine = -1), value = -1);
			BroadCastAcceptors(prepare, (proposer = this, slot = nextSlotForProposer, proposal = (round = nextProposal.round, servermachine = myRank)));
			send timer, STARTTIMER;
		}
		
		on agree do CountAgree;
		on reject goto ProposeValuePhase1 with {
			if(nextProposal.round <= payload.proposal.round)
				maxRound = payload.proposal.round;
				
			send timer, CANCELTIMER;
		};
		on success goto ProposeValuePhase2 with 
		{
			send timer, CANCELTIMER;
		};
		on TIMEOUT goto ProposeValuePhase1;
	}
	
	fun CountAccepted (){
		if(payload.slot == nextSlotForProposer)
		{
			if(equal(payload.proposal, nextProposal))
			{
				countAccept = countAccept + 1;
			}
			if(countAccept == majority)
			{
				raise chosen, payload;
			}
		}
	
	}
	
	fun getHighestProposedValue() : int {
		if(receivedAgree.value != -1)
		{
			return receivedAgree.value;
		}
		else
		{
			return commitValue;
		}
	}
	
	state ProposeValuePhase2 {
		ignore agree;
		entry {
		
			countAccept = 0;
			proposeVal = getHighestProposedValue();
			
			BroadCastAcceptors(accept, (proposer = this, slot = nextSlotForProposer, proposal = nextProposal, value = proposeVal));
			send timer, STARTTIMER;
		}
		
		exit {
			if(trigger == chosen)
			{
				send timer, CANCELTIMER;

				//increment the nextSlotForProposer
				nextSlotForProposer = nextSlotForProposer + 1;
			}
		}
		on accepted do CountAccepted;
		on reject goto ProposeValuePhase1 with {
			if(nextProposal.round <= payload.proposal.round)
				maxRound = payload.proposal.round;
				
			send timer, CANCELTIMER;
		};
		on timeout goto ProposeValuePhase1;
		
	}
	
	
	fun RunReplicatedMachine() {
		while(true)
		{
			if((lastExecutedSlot + 1) in learnerSlots)
			{
				//run the machine
				if(currentLeader.rank == myRank)
					send learner, SMR_RM_OPERATION, payload;
				lastExecutedSlot = lastExecutedSlot + 1;
			}
			else
			{
				return;
			}
		}
	
	}
	

	state RunLearner {
		ignore agree, accepted, TIMEOUT, prepare, reject, accept;
		entry {
			learnerSlots[payload.slot] = (proposal = payload.proposal, value = payload.value);
			RunReplicatedMachine();
			if(commitValue == payload.value)
			{
				pop;
			}
			else
			{
				proposeVal = commitValue;
				raise(goPropose);
			}
		}
	
	}
}

