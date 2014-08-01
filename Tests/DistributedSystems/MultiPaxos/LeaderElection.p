/*
The leader election protocol for multi-paxos, the protocol is based on broadcast based approach. 

*/
event Ping : (rank:int, server : id);
event newLeader : (rank:int, server : id);
event timeout : (myId : id)
machine LeaderElection {
	var servers : seq[id];
	var parentServer : id;
	var currentLeader : (rank:int, server : id);
	var myRank : int;
	var CommunicateLeaderTimeout : id;
	var BroadCastTimeout : id;
	var iter : int;
	
	start state Init {
		entry {
			servers = payload.servers;
			parentServer = payload.parentServer;
			myRank = payload.rank;
			currentLeader = (rank = myRank, server = this);
			CommunicateLeaderTimeout = new Timer((this, 100));
			BroadCastTimeout = new Timer((this, 10));
			raise(local);
		}
		on local goto ProcessPings;
	}
	
	action calculateLeader {
		if(payload.rank < myRank)
		{
			currentLeader = payload;
		}
	}
	fun BroadCast(ev : eid, pd : any) {
		iter = 0;
		while(iter < sizeof(servers))
		{
			send(servers[iter], ev, pd);
		}
	}
	state ProcessPings {
		entry {
				BroadCast(Ping, (rank = myRank, server = this));
				send(BroadCastTimeout, startTimer);
		}
		on Ping do calculateLeader;
		on timeout goto ProcessPings
		{
			if(payload.myId == CommunicateLeaderTimeout)
			{
				send(parentServer, newLeader, currentLeader);
				currentLeader = (rank = myRank, server = this);
				send(CommunicateLeaderTimeout, startTimer);
				send(BroadCastTimeout, cancelTimer);
				
			}
		};
	}
}

machine Timer {
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