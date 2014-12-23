// RTC compiles way better than random

/*
The basic paxos algorithm. 
*/
event prepare assume 3: (proposer: machine, slot : int, proposal : (round: int, servermachine : int)) ;
event accept  assume 3: (proposer: machine, slot: int, proposal : (round: int, servermachine : int), value : int);
event agree assume 6: (slot:int, proposal : (round: int, servermachine : int), value : int) ;
event reject  assume 6: (slot: int, proposal : (round: int, servermachine : int));
event accepted  assume 6: (slot:int, proposal : (round: int, servermachine : int), value : int);
event local;
event success;
event allNodes: (nodes: seq[machine]);
event goPropose;
event chosen : (slot:int, proposal : (round: int, servermachine : int), value : int);
/**** client events ********/
event update : (seqmachine: int, command : int);

machine PaxosNode {

	var currentLeader : (rank:int, server : machine);
	var leaderElectionService : machine;
/********************** Proposer **************************************************/
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
	var returnVal : bool;
	var timer: machine;
	var receivedMess_1 : (slot:int, proposal : (round: int, servermachine : int), value : int);
	var nextSlotForProposer : int;
	var currCommitOperation : bool;
/*************************** Acceptor **********************************************/
	var acceptorSlots : map[int, (proposal : (round: int, servermachine : int), value : int)];
	var receivedMess_2 : (proposer: machine, slot : int, proposal : (round: int, servermachine : int), value : int);
/**************************** Learner **************************************/
	var learnerSlots : map[int, (proposal : (round: int, servermachine : int), value : int)];
	var lastExecutedSlot:int;
	
	start state Init {
		defer Ping;
		entry {
			myRank = (payload as (rank:int)).rank;
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
	
	fun UpdateAcceptors() {
		acceptors = payload.nodes;
		majority = (sizeof(acceptors))/2 + 1;
		assert(majority == 2);
		//Also start the leader election service;
		leaderElectionService = new LeaderElection((servers = acceptors, parentServer = this, rank = myRank));
		
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
			send currentLeader.server, update, payload;
		}
	}
	state PerformOperation {
		ignore agree, accepted, timeout;
		
		/***** proposer ******/
		on update do CheckIfLeader;
		on goPropose push ProposeValuePhase1;
		
		/***** acceptor ****/
		on prepare do preparefun;
		on accept do acceptfun;
		
		/**** leaner ****/
		on chosen push RunLearner;
		
		/*****leader election ****/
		on Ping do ForwardToLE;
		on newLeader do UpdateLeader;
	}
	
	fun ForwardToLE() {
		send leaderElectionService, Ping, payload;
	}
	
	fun UpdateLeader() {
		currentLeader = payload;
	}
	
	fun preparefun(){
		receivedMess_2.proposal = (payload as (proposer: machine, slot : int, proposal : (round: int, servermachine : int))).proposal;
		receivedMess_2.proposer = (payload as (proposer: machine, slot : int, proposal : (round: int, servermachine : int))).proposer;
		receivedMess_2.slot = (payload as (proposer: machine, slot : int, proposal : (round: int, servermachine : int))).slot;
		
		if(!(receivedMess_2.slot in acceptorSlots))
		{
			send receivedMess_2.proposer, agree, (slot = receivedMess_2.slot, proposal = (round = -1, servermachine = -1), value = -1);
			acceptorSlots[receivedMess_2.slot] = (proposal = receivedMess_2.proposal, value = -1);
			return;
		}
		returnVal = lessThan(receivedMess_2.proposal, acceptorSlots[receivedMess_2.slot].proposal);
		if(returnVal)
		{
			send receivedMess_2.proposer, reject, (slot = receivedMess_2.slot, proposal = acceptorSlots[receivedMess_2.slot].proposal);
		}
		else 
		{
			send receivedMess_2.proposer, agree, (slot = receivedMess_2.slot, proposal = acceptorSlots[receivedMess_2.slot].proposal, value = acceptorSlots[receivedMess_2.slot].value);
			acceptorSlots[receivedMess_2.slot] = (proposal = receivedMess_2.proposal, value = -1);
		}
	}
	
	fun acceptfun (){
		receivedMess_2 = (payload as (proposer: machine, slot:int, proposal : (round: int, servermachine : int), value : int));
		if(receivedMess_2.slot in acceptorSlots)
		{
			returnVal = equal(receivedMess_2.proposal, acceptorSlots[receivedMess_2.slot].proposal);
			if(!returnVal)
			{
				send receivedMess_2.proposer, reject, (slot = receivedMess_2.slot, proposal = acceptorSlots[receivedMess_2.slot].proposal);
			}
			else
			{
				acceptorSlots[receivedMess_2.slot] = (proposal = receivedMess_2.proposal, value = receivedMess_2.value);
				send receivedMess_2.proposer, accepted, (slot = receivedMess_2.slot, proposal = receivedMess_2.proposal, value = receivedMess_2.value);
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
	
	/**************************** Proposer **********************************************************/
	
	fun BroadCastAcceptors(mess: event, pay : any) {
		iter = 0;
		while(iter < sizeof(acceptors))
		{
			send acceptors[iter], mess, pay;
			iter = iter + 1;
		}
	}
	
	fun CountAgree (){
		receivedMess_1 = (payload as (slot : int, proposal : (round: int, servermachine : int), value : int));
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
			receivedAgree = (proposal = (round = -1, servermachine = -1), value = -1);
			BroadCastAcceptors(prepare, (proposer = this, slot = nextSlotForProposer, proposal = (round = nextProposal.round, servermachine = myRank)));
			monitor ValmachineityCheck, monitor_proposer_sent, proposeVal;
			send timer, startTimer;
		}
		
		on agree do CountAgree;
		on reject goto ProposeValuePhase1 with {
			if(nextProposal.round <= payload.proposal.round)
				maxRound = payload.proposal.round;
				
			send timer, cancelTimer;
		};
		on success goto ProposeValuePhase2 with
		{
			send timer, cancelTimer;
		};
		on timeout goto ProposeValuePhase1;
	}
	
	fun CountAccepted (){
		receivedMess_1 = (payload as (slot:int, proposal : (round: int, servermachine : int), value : int));
		if(receivedMess_1.slot == nextSlotForProposer)
		{
			returnVal = equal(receivedMess_1.proposal, nextProposal);
			if(returnVal)
			{
				countAccept = countAccept + 1;
			}
			if(countAccept == majority)
			{
				raise chosen, receivedMess_1;
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
			//monitor the monitor on proposal event
			monitor BasicPaxosInvariant_P2b, monitor_valueProposed, (proposer = this, slot = nextSlotForProposer, proposal = nextProposal, value = proposeVal);
			monitor ValmachineityCheck, monitor_proposer_sent, proposeVal;
			
			BroadCastAcceptors(accept, (proposer = this, slot = nextSlotForProposer, proposal = nextProposal, value = proposeVal));
			send timer, startTimer;
		}
		
		exit {
			if(trigger == chosen)
			{
				//the value is chosen, hence monitor the monitor on chosen event
				monitor BasicPaxosInvariant_P2b, monitor_valueChosen, (proposer = this, slot = nextSlotForProposer, proposal = nextProposal, value = proposeVal);
			
				send timer, cancelTimer;
				
				monitor ValmachineityCheck, monitor_proposer_chosen, proposeVal;

				//increment the nextSlotForProposer
				nextSlotForProposer = nextSlotForProposer + 1;
			}
		}
		on accepted do CountAccepted;
		on reject goto ProposeValuePhase1 with {
			if(nextProposal.round <= payload.proposal.round)
				maxRound = payload.proposal.round;
				
			send timer, cancelTimer;
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
			receivedMess_1 = (payload  as (slot:int, proposal : (round: int, servermachine : int), value : int));
			learnerSlots[receivedMess_1.slot] = (proposal = receivedMess_1.proposal, value = receivedMess_1.value);
			RunReplicatedMachine();
			if(currCommitOperation && commitValue == receivedMess_1.value)
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


/*
Properties :
The property we check is that 
P2b : If a proposal is chosen with value v , then every higher numbered proposal issued by any proposer has value v.

*/

event monitor_valueChosen : (proposer: machine, slot: int, proposal : (round: int, servermachine : int), value : int);
event monitor_valueProposed : (proposer: machine, slot:int, proposal : (round: int, servermachine : int), value : int);

monitor BasicPaxosInvariant_P2b {
	var lastValueChosen : map[int, (proposal : (round: int, servermachine : int), value : int)];
	var returnVal : bool;
	var receivedValue : (proposer: machine, slot: int, proposal : (round: int, servermachine : int), value : int);
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
		on monitor_valueChosen goto CheckValueProposed with 
		{
			receivedValue = (payload as (proposer: machine, slot:int, proposal : (round: int, servermachine : int), value : int));
			lastValueChosen[receivedValue.slot] = (proposal = receivedValue.proposal, value = receivedValue.value);
		};
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
	
	state CheckValueProposed {
		on monitor_valueChosen goto CheckValueProposed with {
			receivedValue = (payload as (proposer: machine, slot: int, proposal : (round: int, servermachine : int), value : int));
			assert(lastValueChosen[receivedValue.slot].value == receivedValue.value);
		};
		on monitor_valueProposed goto CheckValueProposed with {
			receivedValue = (payload as (proposer: machine, slot : int, proposal : (round: int, servermachine : int), value : int));
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

monitor ValmachineityCheck {
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
		on monitor_proposer_chosen do checkChosenValmachineity;
	}
	
	fun addClientSet() {
		clientSet[payload] = 0;
	}
	
	fun addProposerSet() {
		assert(payload in clientSet);
		ProposedSet[payload as int] = 0;
	}
	
	fun checkChosenValmachineity() {
		assert(payload in ProposedSet);
	}
}


/*
The leader election protocol for multi-paxos, the protocol is based on broadcast based approach. 

*/
event Ping assume 4 : (rank:int, server : machine) ;
event newLeader : (rank:int, server : machine);
event timeout : (mymachine : machine);
event startTimer;
event cancelTimer;
event cancelTimerSuccess;

machine LeaderElection {
	var servers : seq[machine];
	var parentServer : machine;
	var currentLeader : (rank:int, server : machine);
	var myRank : int;
	var iter : int;
	
	start state Init {
		entry {
			servers = (payload as (servers: seq[machine], parentServer:machine, rank : int)).servers;
			parentServer = (payload as (servers: seq[machine], parentServer:machine, rank : int)).parentServer;
			myRank = (payload as (servers: seq[machine], parentServer:machine, rank : int)).rank;
			currentLeader = (rank = myRank, server = this);
			raise(local);
		}
		on local goto SendLeader;
		
	}
	
	state SendLeader {
		entry {
			currentLeader = GetNewLeader();
			assert(currentLeader.rank <= myRank);
			send parentServer, newLeader, currentLeader;
		}
	}
	model fun GetNewLeader() : (rank:int, server : machine) {
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

model Timer {
	var target: machine;
	var timeoutvalue : int;
	start state Init {
		entry {
			target = (payload as (machine, int)).0;
			timeoutvalue = (payload as (machine, int)).1;
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
			if ($) {
				//send target, timeout, (mymachine = this));
				raise(local);
			}
		}
		on local goto Loop;
		on cancelTimer goto Loop;
	}
}

event response;

main machine GodMachine {
	var paxosnodes : seq[machine];
	var temp : machine;
	var iter : int;
	start state Init {
		entry {
			temp = new PaxosNode((rank = 3,));
			paxosnodes += (0, temp);
			temp = new PaxosNode((rank = 2,));
			paxosnodes += (0, temp);
			temp = new PaxosNode((rank = 1,));
			paxosnodes += (0, temp);
			//send all nodes the other machines
			iter = 0;
			while(iter < sizeof(paxosnodes))
			{
				send paxosnodes[iter], allNodes, (nodes = paxosnodes,);
				iter = iter + 1;
			}
			//create the client nodes
			new Client(paxosnodes);
		}
	}
}

model Client {
	var servers :seq[machine];
	start state Init {
		entry {
			new ValmachineityCheck();
			servers = payload as seq[machine];
			raise(local);
		}
		on local goto PumpRequestOne;
	}
	
	state PumpRequestOne {
		entry {
			
			monitor ValmachineityCheck, monitor_client_sent, 1;
			if($)
				send servers[0], update, (seqmachine  = 0, command = 1);
			else
				send servers[sizeof(servers) - 1], update, (seqmachine  = 0, command = 1);
				
			raise(response);
		}
		on response goto PumpRequestTwo;
	}
	
	state PumpRequestTwo {
		entry {
			
			monitor ValmachineityCheck, monitor_client_sent, 2;
			if($)
				send servers[0], update, (seqmachine  = 0, command = 2);
			else
				send servers[sizeof(servers) - 1], update, (seqmachine  = 0, command = 2);
				
			raise(response);
		}
		on response goto Done;
	}

	state Done {
	
	}
}
