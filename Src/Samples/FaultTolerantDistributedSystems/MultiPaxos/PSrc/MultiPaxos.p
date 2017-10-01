machine MultiPaxosNodeMachine
sends eSMRLeaderUpdated, eSMRReplicatedMachineOperation, eStartTimer, eCancelTimer;
{

	var client: SMRClientInterface;
	var FT : FaultTolerance;
	var commitId : int;

// leader election
	var currentLeader : (rank:int, server : SMRServerInterface);
	var leaderElectionService : LeaderElectionInterface;

// Proposer 
	var acceptors : seq[MultiPaxosNodeInterface];
	var commitVal : SMROperationType;
	var proposeVal : SMROperationType;
	var majority : int;
	var roundNum : int;
	var myRank : int;
	var nextProposal: ProposalIdType;
	var receivedAgree : (proposal : ProposalIdType, smrop : SMROperationType);
	var maxRound : int;
	var countAccept : int;
	var countAgree : int;
	var timer : TimerPtr;
	var nextSlotForProposer : int;
//Acceptor 
	var acceptorSlots : map[int, (proposal : ProposalIdType, smrop : SMROperationType)];
	
//Learner 
	var learnerSlots : map[int, (proposal : ProposalIdType, smrop : SMROperationType)];
	var lastExecutedSlot:int;
	var learner : SMRReplicatedMachineInterface;

	start state Init {
		defer ePing;
		entry (payload: SMRServerConstrutorType) {
			var repSMConstArg : data;
			client = payload.client;
			if(payload.isRoot)
			{
				myRank = 0;
				//update the client about leader
				SendSMRServerUpdate(client, (0, this to SMRServerInterface));
				repSMConstArg = payload.val;
				//create the rest of nodes
				SetUp(repSMConstArg);
			}
			else
			{
				myRank = (payload.val as (int, data)).0;
				repSMConstArg = (payload.val as (int, data)).1;
			}

			currentLeader = (rank = myRank, server = this to SMRServerInterface);
			roundNum = 0;
			maxRound = 0;
			timer = CreateTimer(this to ITimerClient);
			lastExecutedSlot = -1;
			nextSlotForProposer = 0;
			learner = new SMRReplicatedMachineInterface((client = payload.client, val = repSMConstArg));

			receive {
				case eAllNodes : (nodes: seq[MultiPaxosNodeInterface]) {
					acceptors = nodes;
					majority = (sizeof(acceptors))/2 + 1;
					//Also start the leader election service;
					leaderElectionService = new LeaderElectionInterface((servers = acceptors, parentServer = this, rank = myRank));
				}
			}
			
			raise local;
		}
		on local push PerformOperation;

		on eSMROperation do (payload: SMROperationType){
			//all operations are considered to update operations.
			raise eUpdate, payload;
		}
	}
	
	fun SetUp (arg : data)
	{
		var nodes : seq[MultiPaxosNodeInterface];
		var iter : int;
		var temp: MultiPaxosNodeInterface; 
		iter = 1;

		//create all the nodes and then send eAllNodes.
		nodes += (0, this to MultiPaxosNodeInterface);
		while(iter < 2*(FT to int) + 1)
		{
			temp = new MultiPaxosNodeInterface((client = client, reorder = false, isRoot = false, ft = FT, val = (iter, arg)));
		
			nodes += (iter, temp);
			iter = iter + 1;
		}
		
		//send to all
		iter = 0;
		while(iter < sizeof(nodes))
		{
			send nodes[iter], eAllNodes,  nodes;
			iter = iter + 1;
		}
	}

	state PerformOperation {
		ignore eAgree, eAccepted, eTimeOut, eReject;
		
		// proposer
		on eUpdate do (payload: SMROperationType) {
			if(currentLeader.rank == myRank) {
				// I am the leader 
				commitVal = payload;
				proposeVal = commitVal;
				goto ProposeValuePhase1;
			}
			else
			{
				//forward it to the leader
				SEND(currentLeader.server, eSMROperation, payload);
			}
		}
		
		//acceptor
		on ePrepare do (payload: (proposer: MultiPaxosNodeInterface, slot : int, proposal : ProposalIdType)) 
		{ PrepareAction(payload); }
		on eAccept do (payload: (proposer: MultiPaxosNodeInterface, slot: int, proposal : ProposalIdType, smrop : SMROperationType)) { AcceptAction(payload); }
		
		// learner
		on eChosen push RunLearner;
		
		//leader election
		on eFwdPing do (payload: (rank:int, server : any<MultiPaxosLEEvents>)){ 
			// forward to LE machine
			send leaderElectionService, ePing, payload;
		}
		on eNewLeader do (payload: (rank:int, server : any<MultiPaxosLEEvents>)){
			currentLeader = payload as (rank:int, server : SMRServerInterface);
			if(currentLeader.server == this to SMRServerInterface)
			{
				//tell the replicated machine that it is the leader now
				send learner, eSMRReplicatedLeader;
				//update the client about leader
				SendSMRServerUpdate(client, (0, currentLeader.server to SMRServerInterface));
			}
			
		}
	}
	
	fun PrepareAction(payload: (proposer: MultiPaxosNodeInterface, slot : int, proposal : ProposalIdType)) {
		
		if(!(payload.slot in acceptorSlots))
		{
			SEND(payload.proposer, eAgree, (slot = payload.slot, proposal = default(ProposalIdType), smrop = default(SMROperationType)));
			acceptorSlots[payload.slot] = (proposal = payload.proposal, smrop = default(SMROperationType));
			return;
		}

		if(LessThan(payload.proposal, acceptorSlots[payload.slot].proposal))
		{
			SEND(payload.proposer, eReject, (slot = payload.slot, proposal = acceptorSlots[payload.slot].proposal));
		}
		else 
		{
			SEND(payload.proposer, eAgree, (slot = payload.slot, proposal = acceptorSlots[payload.slot].proposal, smrop = acceptorSlots[payload.slot].smrop));
			acceptorSlots[payload.slot] = (proposal = payload.proposal, smrop = default(SMROperationType));
		}
	}
	
	fun AcceptAction (payload: (proposer: MultiPaxosNodeInterface, slot: int, proposal : ProposalIdType, smrop : SMROperationType)){
		if(payload.slot in acceptorSlots)
		{
			if(!Equal(payload.proposal, acceptorSlots[payload.slot].proposal))
			{
				SEND(payload.proposer, eReject, (slot = payload.slot, proposal = acceptorSlots[payload.slot].proposal));
			}
			else
			{
				acceptorSlots[payload.slot] = (proposal = payload.proposal, smrop = payload.smrop);
				SEND(payload.proposer, eAccepted, (slot = payload.slot, proposal = payload.proposal, smrop = payload.smrop));
			}
		}
	}
	
	
	
	
	fun GetNextProposal(maxRound : int) : ProposalIdType {
		return (roundId = maxRound + 1, serverId = myRank);
	}
	
	fun Equal (p1 : ProposalIdType, p2 : ProposalIdType) : bool {
		if(p1.roundId == p2.roundId && p1.serverId == p2.serverId)
			return true;
		else
			return false;
	}
	
	fun LessThan (p1 : ProposalIdType, p2 : ProposalIdType) : bool {
		if(p1.roundId < p2.roundId)
		{
			return true;
		}
		else if(p1.roundId == p2.roundId)
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
	

	
	fun BroadCastToAcceptors(mess: event, pay : any) {
		var iter: int;
		iter = 0;
		while(iter < sizeof(acceptors))
		{
			SEND(acceptors[iter], mess, pay);
			iter = iter + 1;
		}
	}
	
	state ProposeValuePhase1 {
		ignore eAccepted;
		entry {
			countAgree = 0;
			nextProposal = GetNextProposal(maxRound);
			receivedAgree = (proposal = default(ProposalIdType), smrop = default(SMROperationType));
			BroadCastToAcceptors(ePrepare, (proposer = this, slot = nextSlotForProposer, proposal = (roundId = nextProposal.roundId, serverId = myRank)));
			StartTimer(timer, 100);
		}
		
		on eAgree do (payload: (slot:int, proposal : ProposalIdType, smrop : SMROperationType)){
			if(payload.slot == nextSlotForProposer)
			{
				countAgree = countAgree + 1;
				if(LessThan(receivedAgree.proposal, payload.proposal))
				{
					receivedAgree.proposal = payload.proposal;
					receivedAgree.smrop = payload.smrop;
				}
				if(countAgree == majority)
					raise(eSuccess);
			}
		}
		on eReject goto ProposeValuePhase1 with (payload:(slot: int, proposal : ProposalIdType)){
			if(nextProposal.roundId <= payload.proposal.roundId)
				maxRound = payload.proposal.roundId;
				
			CancelTimer(timer);
		}
		on eSuccess goto ProposeValuePhase2 with 
		{
			CancelTimer(timer);
		}
		on eTimeOut goto ProposeValuePhase1;
	}
	
	fun GetHighestProposedValue() : SMROperationType {
		if(receivedAgree.smrop != default(SMROperationType))
		{
			return receivedAgree.smrop;
		}
		else
		{
			return commitVal;
		}
	}
	
	state ProposeValuePhase2 {
		ignore eAgree;
		entry {
			countAccept = 0;
			proposeVal = GetHighestProposedValue();
			BroadCastToAcceptors(eAccept, (proposer = this, slot = nextSlotForProposer, proposal = nextProposal, smrop = proposeVal));
			StartTimer(timer, 100);
		}
		
		
		on eAccepted do (payload: (slot:int, proposal : ProposalIdType, smrop : SMROperationType)){
			if(payload.slot == nextSlotForProposer)
			{
				if(Equal(payload.proposal, nextProposal))
				{
					countAccept = countAccept + 1;
				}
				if(countAccept == majority)
				{
					CancelTimer(timer);
					//increment the nextSlotForProposer
					nextSlotForProposer = nextSlotForProposer + 1;
					raise eChosen, payload;
				}
			}
		}

		on eReject goto ProposeValuePhase1 with (payload: (slot: int, proposal : ProposalIdType)){
			if(nextProposal.roundId <= payload.proposal.roundId)
				maxRound = payload.proposal.roundId;
				
			CancelTimer(timer);
		}
		on eTimeOut goto ProposeValuePhase1;
		
	}
	
	
	fun RunReplicatedMachine() {
		while(true)
		{
			if((lastExecutedSlot + 1) in learnerSlots)
			{
				//run the machine
				if(currentLeader.rank == myRank)
				{
					SendSMRRepMachineOperation(learner, learnerSlots[(lastExecutedSlot + 1)].smrop, commitId);
					commitId = commitId + 1;
					lastExecutedSlot = lastExecutedSlot + 1;
				}
			}
			else
			{
				return;
			}
		}
	
	}
	
	state RunLearner {
		ignore eAgree, eAccepted, eTimeOut, ePrepare, eReject, eAccept;
		entry (payload: (slot:int, proposal : ProposalIdType, smrop : SMROperationType)) {
			learnerSlots[payload.slot] = (proposal = payload.proposal, smrop = payload.smrop);
			RunReplicatedMachine();
			if(commitVal == payload.smrop)
			{
				pop;
			}
			else
			{
				//try proposing again
				proposeVal = commitVal;
				goto ProposeValuePhase1;
			}
		}
	
	}
}
