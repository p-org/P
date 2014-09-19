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
	var CommunicateLeaderTimeout : mid;
	var BroadCastTimeout : mid;
	var iter : int;
	
	start state Init {
		entry {
			servers = ((servers: seq[id], parentServer:id, rank : int)) payload.servers;
			parentServer = ((servers: seq[id], parentServer:id, rank : int))payload.parentServer;
			myRank = ((servers: seq[id], parentServer:id, rank : int))payload.rank;
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
			if(((myId : mid))payload.myId == CommunicateLeaderTimeout)
			{
				assert(currentLeader.rank <= myRank);
				send(parentServer, newLeader, currentLeader);
				currentLeader = (rank = myRank, server = this);
				send(CommunicateLeaderTimeout, startTimer);
				send(BroadCastTimeout, cancelTimer);
			}
		};
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