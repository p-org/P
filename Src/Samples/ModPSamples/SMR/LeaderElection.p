/*
The leader election protocol for multi-paxos, the protocol is based on broadcast based approach. 
*/
machine LeaderElection {
	var servers : seq[machine];
	var parentServer : machine;
	var currentLeader : (rank:int, server : machine);
	var myRank : int;
	var CommunicateLeaderTimeout : TimerPtr;
	var BroadCastTimeout : TimerPtr;
	var iter : int;
	
	start state Init {
		entry (payload: (servers: seq[machine], parentServer:machine, rank : int)){
			servers = payload.servers;
			parentServer = payload.parentServer;
			myRank = payload.rank;
			currentLeader = (rank = myRank, server = this);
			CommunicateLeaderTimeout = CreateTimer(this);
			BroadCastTimeout = CreateTimer(this, 10);
			goto ProcessPings;
		}
	}
	
	fun BroadCast(ev : event, pd : any) {
		iter = 0;
		while(iter < sizeof(servers))
		{
			send servers[iter], ev, pd;
		}
	}
	state ProcessPings {
		entry {
				BroadCast(Ping, (rank = myRank, server = this));
				StartTimer(BroadCastTimeout, 100);
		}
		on Ping do (payload : (rank:int, server : machine))
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
				send parentServer, newLeader, currentLeader;
				currentLeader = (rank = myRank, server = this);
				StartTimer(CommunicateLeaderTimeout, 100);
				CancelTimer(BroadCastTimeout);
			}
			goto ProcessPings;
		}
	}
}
