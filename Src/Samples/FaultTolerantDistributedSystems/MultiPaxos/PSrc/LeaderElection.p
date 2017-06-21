/*
The leader election protocol for multi-paxos, the protocol is based on broadcast based approach. 
*/
machine LeaderElectionMachine : LeaderElectionInterface
receives eTimeOut, eCancelSuccess, eCancelFailure;
sends eNewLeader, eStartTimer, eCancelTimer;
{
	var servers : seq[MultiPaxosNodeInterface];
	var parentServer : MultiPaxosNodeInterface;
	var currentLeader : (rank:int, server : MultiPaxosNodeInterface);
	var myRank : int;
	var CommunicateLeaderTimeout : TimerPtr;
	var BroadCastTimeout : TimerPtr;
	
	start state Init {
		entry (payload: LEContructorType){
			servers = payload.servers;
			parentServer = payload.parentServer;
			myRank = payload.rank;
			currentLeader = (rank = myRank, server = parentServer as MultiPaxosNodeInterface);
			CommunicateLeaderTimeout = CreateTimer(this as ITimerClient);
			BroadCastTimeout = CreateTimer(this as ITimerClient);
			goto ProcessPings;
		}
	}
	
	fun BroadCast(ev : event, pd : any) {
		var iter: int;
		iter = 0;
		while(iter < sizeof(servers))
		{
			send servers[iter], ev, pd;
			iter = iter + 1;
		}
	}

	state ProcessPings {
		entry {
			BroadCast(ePing, (rank = myRank, server = parentServer));
			StartTimer(BroadCastTimeout, 100);
		}
		on ePing do (payload : (rank:int, server : MultiPaxosNodeInterface))
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
				send parentServer, eNewLeader, currentLeader;
				//reset
				currentLeader = (rank = myRank, server = parentServer);
				StartTimer(CommunicateLeaderTimeout, 100);
				CancelTimer(BroadCastTimeout);
			}
			goto ProcessPings;
		}
	}
}

machine LeaderElectionAbsMachine : LeaderElectionInterface
receives eTimeOut, eCancelSuccess, eCancelFailure;
sends eNewLeader, eStartTimer, eCancelTimer;
{
	var servers : seq[MultiPaxosNodeInterface];
	var parentServer : MultiPaxosNodeInterface;
	var currentLeader : (rank:int, server : MultiPaxosNodeInterface);
	var myRank : int;
	
	start state Init {
		entry (payload: LEContructorType) {
			servers = payload.servers;
			parentServer = payload.parentServer;
			myRank = payload.rank;
			currentLeader = (rank = myRank, server = parentServer);
			goto SendLeader;
		}
	}
	
	state SendLeader {
		entry {
			currentLeader = GetNewLeader();
			assert(currentLeader.rank <= myRank);
			send parentServer, eNewLeader, currentLeader;
		}
	}
	
	model fun GetNewLeader() : (rank:int, server : MultiPaxosNodeInterface) {
			return (rank = 1, server = servers[0]);
	}

}