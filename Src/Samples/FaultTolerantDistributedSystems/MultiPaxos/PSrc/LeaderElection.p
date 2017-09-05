/*
The leader election protocol for multi-paxos, the protocol is based on broadcast based approach. 
*/
machine LeaderElectionMachine
sends eNewLeader, eStartTimer, eCancelTimer, eFwdPing;
{
	var servers : seq[any<MultiPaxosLEEvents>];
	var parentServer : any<MultiPaxosLEEvents>;
	var currentLeader : (rank:int, server : any<MultiPaxosLEEvents>);
	var myRank : int;
	var CommunicateLeaderTimeout : TimerPtr;
	var BroadCastTimeout : TimerPtr;
	
	start state Init {
		entry (payload: LEContructorType){
			servers = payload.servers;
			parentServer = payload.parentServer;
			myRank = payload.rank;
			currentLeader = (rank = myRank, server = parentServer);
			CommunicateLeaderTimeout = CreateTimer(this to ITimerClient);
			BroadCastTimeout = CreateTimer(this to ITimerClient);
			goto ProcessPings;
		}
	}
	
	fun BroadCast(ev : event, pd : any) {
		var iter: int;
		iter = 0;
		while(iter < sizeof(servers))
		{
			send servers[iter] to LeaderElectionClientInterface, ev, pd;
			iter = iter + 1;
		}
	}

	state ProcessPings {
		entry {
			BroadCast(eFwdPing, (rank = myRank, server = parentServer));
			StartTimer(BroadCastTimeout, 100);
		}
		on ePing do (payload : (rank:int, server : any<MultiPaxosLEEvents>))
		{
			if(payload.rank < myRank)
			{
				currentLeader = payload;
			}
		}
		on eTimeOut do (payload: TimerPtr)
		{
			if(payload == CommunicateLeaderTimeout)
			{
				assert(currentLeader.rank <= myRank);
				send parentServer to LeaderElectionClientInterface, eNewLeader, currentLeader;
				//reset
				currentLeader = (rank = myRank, server = parentServer);
				StartTimer(CommunicateLeaderTimeout, 100);
			}
			else {
				goto ProcessPings;
			}
			
		}
	}
}

machine LeaderElectionAbsMachine
sends eNewLeader;
{
	var servers : seq[any<MultiPaxosLEEvents>];
	var parentServer : any<MultiPaxosLEEvents>;
	var myRank : int;
	
	start state Init {
		entry (payload: LEContructorType) {
			servers = payload.servers;
			parentServer = payload.parentServer;
			goto SendLeader;
		}
	}
	
	state SendLeader {
		entry {
			var currentLeader : (rank:int, server : any<MultiPaxosLEEvents>);
			currentLeader = GetNewLeader();
			send parentServer to LeaderElectionClientInterface , eNewLeader, currentLeader;
		}
		on null goto SendLeader;
	}
	
	fun ChooseInt(min: int, max: int) : int {
		var iter: int;
		iter = min;
		while (iter < max)
		{
			if($)
				return iter;
			else
				iter = iter + 1;
		}
		return max;
	}

	fun GetNewLeader() : (rank:int, server : any<MultiPaxosLEEvents>) {
		var chooseLeader : int;
		chooseLeader = ChooseInt(1, sizeof(servers));
		return (rank = chooseLeader, server = servers[chooseLeader - 1]);
	}
}

machine MultiPaxosLEAbsMachine
sends ePing;
{
	var allLE : seq[LeaderElectionInterface];
	var numOfNodes : int;
	start state Init {
		entry (payload: SMRServerConstrutorType)
		{
			
			var iter: int;
			var allNodes: seq[LeaderElectionClientInterface];
			var temp : LeaderElectionInterface;
			
			numOfNodes = 3;

			while(iter < numOfNodes)
			{
				allNodes += (iter, this to LeaderElectionClientInterface);
				iter = iter + 1;
			}

			iter = 0;
			while(iter < numOfNodes)
			{
				temp = new LeaderElectionInterface((servers = allNodes, parentServer = this, rank = iter + 1));
				allLE += (iter, temp);
				iter = iter + 1;
			}
		}

		on eFwdPing do (payload: (rank:int, server : any<MultiPaxosLEEvents>)){
			var iter : int;
			iter = 0;
			while(iter < numOfNodes)
			{
				send allLE[iter], ePing, payload;
				iter = iter + 1;
			}
		}

		ignore eNewLeader;
	}

}